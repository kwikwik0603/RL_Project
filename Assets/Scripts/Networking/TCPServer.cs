using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Phase 0 TCP server bridging Unity (server) and Python (client).
/// Sends game state as newline-delimited JSON every decision step (~0.2s),
/// receives an action integer, and applies it to the enemy.
///
/// Protocol matches /shared/protocol.json and /docs/combat_spec.md.
/// Attach to an empty GameObject named "NetworkManager".
/// </summary>
public class TCPServer : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private int port = 9876;
    [SerializeField] private float decisionInterval = 0.2f; // seconds per step

    [Header("Scene References")]
    [SerializeField] private Transform player;   // player capsule
    [SerializeField] private Transform enemy;    // boss capsule (controlled by Python)

    [Header("Arena")]
    [SerializeField] private float arenaSize = 10f; // 10x10, origin at (0,0) in X-Z

    [Header("Combat (must match docs/combat_spec.md)")]
    [SerializeField] private float startingHP = 100f;
    [SerializeField] private float lightAttackDmg = 5f;
    [SerializeField] private float heavyAttackDmg = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int heavyCooldownSteps = 3;
    [SerializeField] private float blockReduction = 0.5f;
    [SerializeField] private float moveSpeed = 1f;       // units per step
    [SerializeField] private float dodgeDistance = 2f;
    [SerializeField] private int dodgeIFrames = 1;       // steps of invulnerability
    [SerializeField] private float playerMeleeDmg = 10f; // player Space attack

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

    // --- Combat state ---
    private float playerHP;
    private float bossHP;
    private int bossHeavyCooldown = 0;
    private int bossDodgeIFrames = 0;
    private int[] playerLastActions = new int[3];
    private int playerState = 0; // 0=idle, 1=attacking
    private bool done = false;

    void Start()
    {
        playerHP = startingHP;
        bossHP = startingHP;

        listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        Debug.Log($"[TCPServer] Listening on localhost:{port}...");

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
            Debug.Log("[TCPServer] Python client connected.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TCPServer] Accept failed: {e.Message}");
        }
    }

    void Update()
    {
        // Coroutines must start on the main thread.
        if (startLoop)
        {
            startLoop = false;
            StartCoroutine(DecisionLoop());
        }

        // Player melee attack (Space) using the new Input System.
        if (clientConnected && !done && Keyboard.current != null
            && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            playerState = 1;
            if (Distance() <= attackRange)
            {
                bossHP = Mathf.Max(0f, bossHP - playerMeleeDmg);
                Debug.Log($"[Combat] Player hit boss for {playerMeleeDmg}. Boss HP: {bossHP}");
            }
        }
    }

    private IEnumerator DecisionLoop()
    {
        while (running && clientConnected)
        {
            // 1. Send current state to Python.
            if (!SendState()) break;

            // 2. Read the response (action or reset command). Blocks briefly.
            string line = ReadLine();
            if (line == null) { OnDisconnect(); break; }

            // 3. Apply it.
            if (line.Contains("\"command\"") && line.Contains("reset"))
            {
                ResetFight();
            }
            else
            {
                int action = ParseAction(line);
                if (action >= 0) ApplyAction(action);
            }

            // Reset per-step player flag after it's been reported.
            playerState = 0;

            // Tick cooldowns.
            if (bossHeavyCooldown > 0) bossHeavyCooldown--;
            if (bossDodgeIFrames > 0) bossDodgeIFrames--;

            yield return new WaitForSeconds(decisionInterval);
        }
    }

    // ----- State serialization (Unity -> Python) -----

    private bool SendState()
    {
        done = playerHP <= 0f || bossHP <= 0f;

        Vector2 pPos = NormPos(player.position);
        Vector2 bPos = NormPos(enemy.position);
        float dist = Mathf.Clamp01(Distance() / (arenaSize * Mathf.Sqrt(2f)));

        var sb = new StringBuilder();
        sb.Append("{");
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"player_hp\": {0:0.####}, ", playerHP / startingHP);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"boss_hp\": {0:0.####}, ", bossHP / startingHP);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"player_pos\": [{0:0.####}, {1:0.####}], ", pPos.x, pPos.y);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"boss_pos\": [{0:0.####}, {1:0.####}], ", bPos.x, bPos.y);
        sb.AppendFormat(CultureInfo.InvariantCulture, "\"distance\": {0:0.####}, ", dist);
        sb.AppendFormat("\"player_last_actions\": [{0}, {1}, {2}], ",
            playerLastActions[0], playerLastActions[1], playerLastActions[2]);
        sb.AppendFormat("\"player_state\": {0}, ", playerState);
        sb.Append("\"floor_number\": 1, ");   // hardcoded for Phase 0
        sb.Append("\"boss_id\": 0, ");         // hardcoded for Phase 0
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
            Debug.LogWarning($"[TCPServer] Send failed: {e.Message}");
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
                if (count <= 0) return null; // disconnected
                lineBuffer += Encoding.UTF8.GetString(readBuffer, 0, count);
            }

            int idx = lineBuffer.IndexOf("\n");
            string line = lineBuffer.Substring(0, idx);
            lineBuffer = lineBuffer.Substring(idx + 1);
            return line;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TCPServer] Read failed: {e.Message}");
            return null;
        }
    }

    [Serializable] private class ActionMessage { public int action = -1; }

    private int ParseAction(string json)
    {
        try
        {
            ActionMessage msg = JsonUtility.FromJson<ActionMessage>(json);
            return msg.action;
        }
        catch
        {
            return -1;
        }
    }

    // ----- Apply boss action -----

    private void ApplyAction(int action)
    {
        Vector3 toPlayer = player.position - enemy.position;
        toPlayer.y = 0f;
        Vector3 dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.forward;
        Vector3 perp = new Vector3(-dir.z, 0f, dir.x); // 90 degrees left

        switch (action)
        {
            case 0: enemy.position += dir * moveSpeed; break;            // move toward
            case 1: enemy.position -= dir * moveSpeed; break;            // move away
            case 2: enemy.position += perp * moveSpeed; break;           // strafe left
            case 3: enemy.position -= perp * moveSpeed; break;           // strafe right
            case 4: BossAttack(lightAttackDmg, false); break;            // light attack
            case 5: BossAttack(heavyAttackDmg, true); break;             // heavy attack
            case 6: Debug.Log("[Combat] Boss blocks."); break;           // block (player dmg halved on their swing)
            case 7:                                                       // dodge
                enemy.position += perp * dodgeDistance;
                bossDodgeIFrames = dodgeIFrames;
                Debug.Log("[Combat] Boss dodges.");
                break;
        }

        ClampToArena(enemy);

        // Face the player so movement reads clearly.
        if (toPlayer.sqrMagnitude > 0.0001f)
            enemy.rotation = Quaternion.LookRotation(dir);
    }

    private void BossAttack(float dmg, bool heavy)
    {
        if (heavy && bossHeavyCooldown > 0)
        {
            Debug.Log("[Combat] Heavy attack on cooldown.");
            return;
        }

        if (Distance() <= attackRange)
        {
            playerHP = Mathf.Max(0f, playerHP - dmg);
            Debug.Log($"[Combat] Boss {(heavy ? "HEAVY" : "light")} hit player for {dmg}. Player HP: {playerHP}");
        }

        if (heavy) bossHeavyCooldown = heavyCooldownSteps;
    }

    // ----- Reset -----

    private void ResetFight()
    {
        playerHP = startingHP;
        bossHP = startingHP;
        bossHeavyCooldown = 0;
        bossDodgeIFrames = 0;
        playerLastActions = new int[3];
        playerState = 0;
        done = false;

        // Reset positions to opposite corners-ish (matches combat_spec spawns).
        player.position = new Vector3(3f, player.position.y, 5f);
        enemy.position = new Vector3(7f, enemy.position.y, 5f);

        Debug.Log("[TCPServer] Fight reset.");
    }

    // ----- Helpers -----

    private float Distance()
    {
        Vector3 a = player.position; a.y = 0f;
        Vector3 b = enemy.position;  b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector2 NormPos(Vector3 worldPos)
    {
        return new Vector2(
            Mathf.Clamp01(worldPos.x / arenaSize),
            Mathf.Clamp01(worldPos.z / arenaSize)
        );
    }

    private void ClampToArena(Transform t)
    {
        Vector3 p = t.position;
        p.x = Mathf.Clamp(p.x, 0f, arenaSize);
        p.z = Mathf.Clamp(p.z, 0f, arenaSize);
        t.position = p;
    }

    private void OnDisconnect()
    {
        clientConnected = false;
        Debug.LogWarning("[TCPServer] Python disconnected. Enemy frozen.");
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
        Debug.Log("[TCPServer] Shut down.");
    }
}
