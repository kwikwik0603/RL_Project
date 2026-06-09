# Unity ↔ Python Bridge Setup (New S_Boss System)

This connects the Python RL client to your teammate's new `S_Boss` combat system
via `S_BossTCPBridge.cs`. The bridge is a thin adapter — it only feeds Python's
chosen action into the existing `S_Boss.currentAction` field and streams game
state back. It does **not** modify any existing script.

## Setup in OutdoorsScene

1. **Create the manager object:** GameObject → Create Empty → rename `NetworkManager`.
2. **Add the bridge:** select `NetworkManager` → Add Component → `S_BossTCPBridge`.
3. **Wire references:**
   - **Boss** ← drag the `Boss` object from the Hierarchy (the one with `S_Boss`).
   - **Player** ← drag the `Player` object.
4. Leave the rest at defaults (Port 9876, Decision Interval 0.2, Arena Size 50).
   - If the boss/player positions are large, increase **Arena Size** so normalized
     positions stay sensible. Doesn't affect movement — only the state values.
5. **Save the scene** (Ctrl+S).

## Run

1. Press **Play** in Unity → Console shows `[BossTCPBridge] Listening on localhost:9876...`
2. Terminal: `python -m python.demo_random_agent`
3. Console shows `Python client connected.` and the **boss moves** (toward / away /
   orbit) under Python control, fully animated.

## Action mapping (protocol → S_Boss)

| Python action | Mapped to S_Boss.currentAction | Status |
|---------------|-------------------------------|--------|
| 0 Move Toward | 1 (toward) | ✅ |
| 1 Move Away   | 2 (away)   | ✅ |
| 2 Strafe Left | 3 (orbit)  | ✅ (direction not yet controllable) |
| 3 Strafe Right| 3 (orbit)  | ✅ (direction not yet controllable) |
| 4 Light Attack| 0 (idle) + log | ⏳ needs S_Boss support |
| 5 Heavy Attack| 0 (idle) + log | ⏳ needs S_Boss support |
| 6 Block       | 0 (idle) + log | ⏳ needs S_Boss support |
| 7 Dodge       | 0 (idle) + log | ⏳ needs S_Boss support |

## Known stubs (for the team)

- **Player HP** is sent as a placeholder `1.0` — player damage isn't wired yet
  (`S_Health` is a stub; `S_PlayerHealth.currentHealth` is private). When player
  health is exposed, update `SendState()` in `S_BossTCPBridge.cs` to read it.
- **Attacks / block / dodge** (actions 4–7) need to be added to `S_Boss`'s switch.
  Once they exist, extend `ApplyMappedAction()` to route to them.
- **Strafe direction**: both strafes map to orbit. To control left/right, expose an
  orbit-direction setter on `S_Boss`.

None of these block the Phase 0 goal (boss movement controlled by Python).
