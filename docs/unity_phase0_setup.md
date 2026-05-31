# Unity Phase 0 Setup — Running the Real Integration Test (No Mock)

This guide gets the Python ↔ Unity TCP round trip working with the **real** Unity
game instead of the mock server. You build a minimal arena, attach `TCPServer.cs`,
press Play, and run the Python demo — the enemy moves in Unity, controlled by Python.

The C# server is already written: `Assets/Scripts/Networking/TCPServer.cs`.
It includes the combat skeleton (HP, attacks, blocking, dodging, reset) matching
`docs/combat_spec.md` and `shared/protocol.json`.

---

## Step 1 — Install Unity 6000.3.12f1

The project was created in Unity **6000.3.12f1** (Unity 6). Use the exact version
to avoid upgrade prompts.

1. Download **Unity Hub**: https://unity.com/download
2. Install Unity Hub, sign in with a free Unity account.
3. In Unity Hub → **Installs** → **Install Editor** → **Archive** tab →
   "download archive" link → find **6000.3.12f1**.
   (Direct: https://unity.com/releases/editor/archive — pick Unity 6, version 6000.3.12.)
4. During install, include the **Windows Build Support** module (default is fine).
5. Wait for it to finish (a few GB).

---

## Step 2 — Open the Project

1. Unity Hub → **Projects** → **Add** → **Add project from disk**.
2. Select the repo folder: `D:\TowerWithin_Gaming_Project\RL_Project`
3. Click the project to open it. First open takes a few minutes (it imports assets
   and recompiles scripts — including the new `TCPServer.cs`).
4. Confirm there are **no compile errors** in the Console (bottom panel).
   `TCPServer.cs` uses the Input System package, which the project already has.

---

## Step 3 — Build the Prototype Arena Scene

You can either use the existing `OutdoorsScene` or make a clean test scene.
A clean scene is simpler — do that:

1. **File → New Scene** → pick "Basic (Built-in)" or empty → **Save As**
   `Assets/Scenes/PrototypeArena.unity`.

2. **Floor:**
   - **GameObject → 3D Object → Plane**. Rename to `Floor`.
   - Set its Transform Position to `(5, 0, 5)` so the 10×10 plane covers `(0,0)`→`(10,10)`.
     (A default Unity plane is 10×10 units, so position 5,0,5 centers it on that range.)

3. **Player:**
   - **GameObject → 3D Object → Capsule**. Rename to `Player`.
   - Position `(3, 1, 5)`.
   - Give it a blue material (optional): right-click in Project →
     **Create → Material**, set Albedo blue, drag onto the capsule.

4. **Enemy:**
   - **GameObject → 3D Object → Capsule**. Rename to `Enemy`.
   - Position `(7, 1, 5)`.
   - Give it a red material (optional).
   - This one has **no movement script** — Python controls it.

5. **Camera:** position the Main Camera so you can see the arena from above, e.g.
   Position `(5, 12, -2)`, Rotation `(70, 0, 0)`. Adjust until both capsules are visible.

> For Phase 0 you do **not** need the existing player controller, Rigidbody, or
> input wiring. You can move the Player capsule by hand in the Scene view to test
> combat, or just leave it still. The point is to see the **enemy** move via Python.

---

## Step 4 — Add the Network Manager

1. **GameObject → Create Empty**. Rename to `NetworkManager`. Position `(0,0,0)`.
2. With it selected, in the Inspector click **Add Component** → search **TCPServer** → add it.
3. In the TCPServer component, drag the scene objects into the slots:
   - **Player** field ← drag the `Player` capsule from the Hierarchy.
   - **Enemy** field ← drag the `Enemy` capsule from the Hierarchy.
4. Leave the other values at their defaults — they already match `combat_spec.md`
   (port 9876, arena size 10, light 5 / heavy 15 dmg, range 2, etc.).
5. **Save the scene** (Ctrl+S).

---

## Step 5 — Run the Integration Test (No Mock)

Order matters: **start Unity first** (it's the server), then run Python (the client).

1. In Unity, press the **Play** button (top center).
   - Console should print: `[TCPServer] Listening on localhost:9876...`
   - Unity will appear to "wait" — that's normal, it's waiting for Python.

2. In a terminal at the repo root, run the demo:
   ```bash
   python -m python.demo_random_agent
   ```

3. **What you should see:**
   - Unity Console: `[TCPServer] Python client connected.`
   - The **red Enemy capsule starts moving** around the arena (random actions).
   - Console logs combat events: `[Combat] Boss light hit player for 5...`
   - The Python terminal prints state/reward each step, episodes end, reset fires.

4. **Test combat both ways:**
   - Move the Player capsule near the Enemy in the Scene view (drag it) and press
     **Space** in the Game view → Console logs `Player hit boss`.
   - When Player or Boss HP hits 0, the episode ends and Python sends a reset →
     Console logs `[TCPServer] Fight reset.`

5. **Record a 30-second screen capture** of the enemy moving — that's the Phase 0
   demo deliverable.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Python: `ConnectionRefusedError` | Unity isn't in Play mode yet, or not on port 9876. Press Play first. |
| Unity freezes on Play | It's blocking waiting for Python — run the demo. If still frozen after, check firewall didn't block the port. |
| `Keyboard.current is null` | Make sure the Input System package is active (it is in this project). Space attack is optional anyway. |
| Enemy moves but no damage | Move the Player capsule within 2 units of the Enemy — attacks only land in range. |
| Compile errors on `using UnityEngine.InputSystem` | Window → Package Manager → confirm "Input System" is installed. |
| Want to stop | Stop Play mode in Unity, then Ctrl+C the Python terminal. |

---

## What This Proves

When the red capsule moves under Python's control with **no mock server running**,
Phase 0's integration checkpoint is met: Unity and Python talk over TCP end-to-end,
state flows out, actions flow in, combat resolves, and reset works. The same
`UnityTowerEnv` that drives this random demo will later load a trained PPO model
with zero code changes.
