using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using UnityEngine;

/// <summary>
/// Phase 0 TCP bridge between Python (RL client) and the new S_Boss combat system.
///
/// This script is a THIN BRIDGE. It does NOT contain combat or movement logic.
/// It only:
///   1. Hosts a TCP server (Unity = server, Python = client) on port 9876.
///   2. Sends protocol-compliant game state JSON every decision step (~0.2s).
///   3. Receives an action integer from Python and writes it to S_Boss.currentAction.
///
/// It sets the EXISTING public field S_Boss.currentAction, so it does not modify
/// S_Boss.cs or any other teammate script. Remove this component (or the
/// NetworkManager object) to fully disable it — nothing else depends on it.
///
/// Protocol matches /shared/protocol.json. The 8-action Python protocol is mapped
/// down to the 4 movement actions S_Boss currently implements; attacks/block/dodge
/// are logged until S_Boss implements them.
/// </summary>
public class S_BossTCPBridge : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private int port = 9876;
    [SerializeField] private float decisionInterval = 0.2f;

    [Header("Scene References")]
    [SerializeField] private S_Boss boss;       // the boss whose currentAction we drive
    [SerializeField] private Transform player;   // player transform (for state)

    [Header("State Normalization")]
    [Tooltip("Boss max health, used to normalize HP. Should match BossData.maxHealth.")]
    [SerializeField] private float bossMaxHealth = 100f;
    [Tooltip("Log raw/normalized distance + action each second for debugging.")]
    [SerializeField] private bool debugLog = true;

    // Training coordinate frame — MUST match SimpleCombatEnv (combat_spec.md).
    // 1 Unity world unit == 1 training unit (both use attackRange = 2).
    private const float TRAIN_ARENA = 10f;                 // ARENA_SIZE in training
    private const float TRAIN_DIAGONAL = 14.142f;          // 10 * sqrt(2)
    private const float ATTACK_RANGE = 2f;                 // must match Attackdata.attackRange
    private const int RETREAT_WINDOW = 6;                  // must match SimpleCombatEnv.RETREAT_WINDOW
    private int debugStep = 0;

    // Footsies: when the policy commits a strike in range, the boss enters a
    // recovery/retreat phase. We feed this back as the boss_recovering obs so the
    // policy knows to back off — producing advance->strike->retreat->re-engage.
    private int recoverTimer = 0;
    private float lastWorldDist = 999f;

    // --- Networking state ---
    private TcpListener listener;
    private TcpClient client;
    private NetworkStream stream;
    private Thread acceptThread;
    private volatile bool clientConnected = false;
    private volatile bool startLoop = false;
    private bool running = true;
    private byte[] readBuffer = new byte[4096];
    private string lineBuffer = "";

    // --- Captured spawn positions (restored on reset) ---
    private Vector3 bossSpawn;
    private Vector3 playerSpawn;

    void Start()
    {
        if (boss == null || player == null)
        {
            Debug.LogError("[BossTCPBridge] Boss or Player reference not set. Disabling.");
            enabled = false;
            return;
        }

        bossSpawn = boss.transform.position;
        playerSpawn = player.position;

        listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        Debug.Log($"[BossTCPBridge] Listening on localhost:{port}...");

        acceptThread = new Thread(AcceptClient) { IsBackground = true };
        acceptThread.Start();
    }

    private void AcceptClient()
    {
        try
        {
            client = listener.AcceptTcpClient();
            stream = client.GetStream();
            clientConnected = true;
            startLoop = true;
            Debug.Log("[BossTCPBridge] Python client connected.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BossTCPBridge] Accept failed: {e.Message}");
        }
    }

    void Update()
    {
        // Coroutines must be started on the main thread.
        if (startLoop)
        {
            startLoop = false;
            StartCoroutine(DecisionLoop());
        }
    }

    private IEnumerator DecisionLoop()
    {
        while (running && clientConnected)
        {
            if (!SendState()) break;

            string line = ReadLine();
            if (line == null) { OnDisconnect(); break; }

            if (line.Contains("\"command\"") && line.Contains("reset"))
                ResetFight();
            else
                ApplyMappedAction(ParseAction(line));

            yield return new WaitForSeconds(decisionInterval);
        }
    }

    // ----- State serialization (Unity -> Python) -----

    private bool SendState()
    {
        float bossHpNorm = Mathf.Clamp01(boss.currentHealth / bossMaxHealth);
        // Player HP is not yet wired in the combat system, so we send 1.0 as a
        // placeholder. Replace once player health is exposed (see setup doc).
        float playerHpNorm = 1.0f;

        // Reconstruct the observation in the TRAINING frame so it matches what the
        // policy saw during training, independent of the scene's world coordinates.
        // Boss is treated as centered; player sits at its real relative offset.
        Vector3 flatPlayer = new Vector3(player.position.x, 0, player.position.z);
        Vector3 flatBoss = new Vector3(boss.transform.position.x, 0, boss.transform.position.z);
        float worldDist = Vector3.Distance(flatPlayer, flatBoss);

        float relX = (player.position.x - boss.transform.position.x) / TRAIN_ARENA;
        float relZ = (player.position.z - boss.transform.position.z) / TRAIN_ARENA;
        Vector2 bPos = new Vector2(0.5f, 0.5f);
        Vector2 pPos = new Vector2(Mathf.Clamp01(0.5f + relX), Mathf.Clamp01(0.5f + relZ));

        float dist = Mathf.Clamp01(worldDist / TRAIN_DIAGONAL);
        lastWorldDist = worldDist;
        float bossRecovering = recoverTimer / (float)RETREAT_WINDOW;

        bool done = boss.currentHealth <= 0f;

        if (debugLog && (++debugStep % 5 == 0))
            Debug.Log($"[BossTCPBridge] worldDist={worldDist:0.0} distNorm={dist:0.00} " +
                      $"-> boss.currentAction={boss.currentAction}");

        var sb = new StringBuilder();
        sb.Append("{");
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"player_hp\": {0:0.####}, ", playerHpNorm);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"boss_hp\": {0:0.####}, ", bossHpNorm);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"player_pos\": [{0:0.####}, {1:0.####}], ", pPos.x, pPos.y);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"boss_pos\": [{0:0.####}, {1:0.####}], ", bPos.x, bPos.y);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"distance\": {0:0.####}, ", dist);
        sb.Append("\"player_last_actions\": [0, 0, 0], "); // not tracked yet
        sb.Append("\"player_state\": 0, ");                // not tracked yet
        sb.Append("\"floor_number\": 1, ");
        sb.Append("\"boss_id\": 0, ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"boss_recovering\": {0:0.####}, ", bossRecovering);
        sb.AppendFormat("\"done\": {0}", done ? "true" : "false");
        sb.Append("}\n");

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
            stream.Write(data, 0, data.Length);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BossTCPBridge] Send failed: {e.Message}");
            OnDisconnect();
            return false;
        }
    }

    // ----- Reading (Python -> Unity) -----

    private string ReadLine()
    {
        try
        {
            while (!lineBuffer.Contains("\n"))
            {
                int count = stream.Read(readBuffer, 0, readBuffer.Length);
                if (count <= 0) return null;
                lineBuffer += Encoding.UTF8.GetString(readBuffer, 0, count);
            }

            int idx = lineBuffer.IndexOf("\n");
            string line = lineBuffer.Substring(0, idx);
            lineBuffer = lineBuffer.Substring(idx + 1);
            return line;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BossTCPBridge] Read failed: {e.Message}");
            return null;
        }
    }

    [Serializable] private class ActionMessage { public int action = -1; }

    private int ParseAction(string json)
    {
        try { return JsonUtility.FromJson<ActionMessage>(json).action; }
        catch { return -1; }
    }

    // ----- Map protocol action (0-8) -> S_Boss.currentAction (0-3) -----
    //
    // Protocol: 0=toward 1=away 2=strafeL 3=strafeR 4=light 5=heavy 6=block 7=dodge 8=idle
    // S_Boss:   0=idle   1=toward 2=away   3=orbit
    private void ApplyMappedAction(int protocolAction)
    {
        // Footsies recovery timer: counts down each decision. Reaching strike
        // range engages the strike+retreat phase (matches SimpleCombatEnv). This
        // drives the visible advance->retreat oscillation using move toward/away.
        if (recoverTimer > 0) recoverTimer--;
        if (recoverTimer == 0 && lastWorldDist <= ATTACK_RANGE)
            recoverTimer = RETREAT_WINDOW;

        switch (protocolAction)
        {
            case 0: boss.currentAction = 1; break;  // toward
            case 1: boss.currentAction = 2; break;  // away
            case 2: boss.currentAction = 3; break;  // strafe left  -> orbit
            case 3: boss.currentAction = 3; break;  // strafe right -> orbit
            case 8: boss.currentAction = 0; break;  // idle (explicit, deliberate)
            case 4:
            case 5:
            case 6:
            case 7:
                boss.currentAction = 0; // idle while unimplemented
                Debug.Log($"[BossTCPBridge] Action {protocolAction} (attack/block/dodge) " +
                          "not yet implemented in S_Boss — boss idles. Add it to S_Boss to enable.");
                break;
            default:
                boss.currentAction = 0;
                break;
        }
    }

    // ----- Reset -----

    private void ResetFight()
    {
        boss.transform.position = bossSpawn;
        player.position = playerSpawn;
        boss.currentAction = 0;
        recoverTimer = 0;
        Debug.Log("[BossTCPBridge] Fight reset (positions restored).");
    }

    // ----- Helpers -----

    private void OnDisconnect()
    {
        clientConnected = false;
        boss.currentAction = 0; // freeze the boss safely
        Debug.LogWarning("[BossTCPBridge] Python disconnected. Boss idles.");
    }

    void OnApplicationQuit() { Shutdown(); }
    void OnDestroy() { Shutdown(); }

    private void Shutdown()
    {
        running = false;
        clientConnected = false;
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }
        try { listener?.Stop(); } catch { }
        Debug.Log("[BossTCPBridge] Shut down.");
    }
}
