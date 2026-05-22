# Jump Feature — Design & Implementation Notes
**Date added:** 2026-05-23
**Scope:** `SimpleCombatEnv` only (Unity-side and `UnityTowerEnv` not yet updated)

---

## Summary

A new action — **action 8: Jump** — was added to `SimpleCombatEnv`. Unlike the existing 8 actions (which are mutually exclusive: you can only do one per tick), Jump puts the entity into a multi-tick **airborne state** during which the existing movement actions (0–3) still apply at reduced speed. This is the project's first **stateful, multi-tick action**, and it doubles as a defensive option: if you commit to the jump early enough, ground attacks whiff against you.

Combat actions (light/heavy/block/dodge) are suppressed while airborne — you can't attack mid-air. Combat resumes the moment you land.

All 5 validation tests pass. Speed dropped from ~44 500 steps/sec to ~13 200 steps/sec (still well above the 10 000 threshold).

---

## Why these design decisions

### Why "Option C" — extend `Discrete(9)` instead of switching to `MultiDiscrete`

Three options were considered:

| Option | Action space | What it gives | What it costs |
|---|---|---|---|
| A — More discrete actions | `Discrete(13)` (add `Jump+Toward`, `Jump+Away`, etc.) | One-tick compound moves | Combinatorial blow-up if you later want jump-attack, jump-block, etc. |
| B — `MultiDiscrete([5, 2, 5])` | Three parallel heads: movement, vertical, combat | Cleanest separation; one-tick compounds | Larger refactor; protocol message changes; all 8 combat actions need re-routing |
| **C — `Discrete(9)`, jump as state** | One new action that *starts* a 4-step state | Minimal diff; compounds emerge over consecutive ticks | Compounds take 2 ticks instead of 1 (jump on tick T, then move on tick T+1) |

**Picked C** because the project is still in Phase 0 — nothing trained, no commitments locked in — and because the existing dodge/stagger logic already uses the "stateful timer" pattern. The agent learns the compound *naturally*: pick action 8 at tick T (initiates jump), pick action 0 at tick T+1 (now interpreted by the aerial handler → moves toward target at half speed while in the air). That is "jump-forward" emerging from composition, no new action ID required.

The cost (compound takes 2 ticks not 1) is acceptable because each tick is only 0.2 s of game time — the responsiveness penalty is negligible at human reaction-time scale, and entirely absorbable by the planner inside PPO.

### Why a **float-valued** `jump_t`

The user's spec called for a 1.5-step startup window:

> "if the action is done 1.5 or steps before an hit action from the boss then it is dodge, but if hits before then it's a hit."

Steps in this env are integers (one per `step()` call), so 1.5 isn't natively representable. Two options:

1. **Round to int** — pick either "airborne at tick T+1" or "airborne at tick T+2". Both feel arbitrary.
2. **Float timer that advances by 1.0 per tick** — `jump_t` crosses 1.5 cleanly on tick T+2, so the predicate `jump_t >= 1.5` is true at T+2 and false at T+1. Code stays in integer-tick land; the **boundary** is the only thing that's fractional.

Picked option 2. It costs nothing (one `float` instead of `int`) and makes the "1.5" in the spec literal rather than approximate. The walkthrough table:

| Jump initiated at | `jump_t` value when attack resolves | Airborne? (≥ 1.5) | Outcome |
|---|---|---|---|
| Same tick as attack | 1.0 | No | **HIT** — caught in startup |
| 1 tick before attack | 2.0 | Yes | **JUMP-DODGE** |
| 2 ticks before attack | 3.0 | Yes | **JUMP-DODGE** |
| 3 ticks before attack | 0.0 (landed) | No | **HIT** — already on ground again |
| Attack first, then jump | 0.0 | No | **HIT** — too late |

The "you can't dodge on reaction" property emerges naturally: at minimum the agent must commit one tick (200 ms) before impact, which is roughly human reaction-time. This forces real *planning* rather than reflex.

### Why two functions (a) and (b)

Requested explicitly: a function for ground movement and a function for airborne movement. The rationale is **separation of physics concerns**:

- **`_ground_action(entity, action)`** — function (a). X/Z plane only. Full movement speed. All eight original actions available (move, strafe, light, heavy, block, dodge).
- **`_aerial_action(entity, action)`** — function (b). X/Z at half speed (`AERIAL_SPEED_MUL = 0.5`) plus the parabolic Y arc derived from `jump_t`. Combat actions (4–7) become no-ops mid-air.

A thin dispatcher (`_execute_boss_action`, `_execute_player_action`) routes each tick to (a) or (b) based on `jump_t`:

```
if action == 8:        → _try_initiate_jump(entity)
elif jump_t > 0:       → _aerial_action(entity, action)
else:                  → _ground_action(entity, action)
```

This is the same pattern the env already uses for stagger ("if staggered, skip action execution"); the jump just adds one more branch.

### Why the parabolic Y arc

Unity needs to know how high the model is at each tick so it can lerp the visual character upward. The shape `y = MAX_JUMP_HEIGHT * 4 * t * (1 - t)` (with `t` normalized 0..1 across the jump) gives `y = 0` at both ends and peak at the midpoint — the canonical jump shape with one line of code, no gravity integration needed. The env doesn't actually use Y for combat (it uses `jump_t` directly), so this is purely the value the network — and eventually Unity — reads from `boss_y` / `player_y`.

### Why combat actions are suppressed mid-air

Two reasons:

1. **Design commitment.** Jump is a defensive option; if it also let you attack, it would dominate ground options (free mobility + free damage + i-frames against most things). Locking out combat keeps the option *interesting* — you trade offense for defense.
2. **Simplicity.** Adding jump-attack would need its own damage value, hitbox math, and reward shaping (jump_dodge in [reward_configs.py](RL_Project/python/training/reward_configs.py)). Phase 0 doesn't need that complexity.

If/when jump-attack becomes desirable, the change is small: drop the `if self.boss_jump_t == 0:` gate around `action == 4` in `step()` and pick a damage value.

### Why stagger freezes the jump

Without this, a stunlocked entity in the middle of a jump would "complete" the jump in stagger — the arc would visually finish while they're frozen in place. That looks broken. The fix is one conditional: only advance `jump_t` when `stagger_timer <= 0`. Mid-jump heavy hits now correctly pin the character in mid-air for the stagger duration before the arc resumes — which is also more interesting tactically (a successful heavy interrupts a jumping enemy mid-leap).

---

## All changes made

### `RL_Project/python/envs/simple_combat_env.py`

Every addition tagged with `# JUMP:` for greppability.

1. **Constants block** between `MAX_STEPS` and the class:
   - `JUMP_STARTUP = 1.5` — steps of startup before airborne i-frames begin.
   - `JUMP_TOTAL = 4.0` — total jump duration (0.8 s).
   - `JUMP_COOLDOWN_STEPS = 5` — gap between consecutive jumps.
   - `AERIAL_SPEED_MUL = 0.5` — half horizontal speed mid-air.
   - `MAX_JUMP_HEIGHT = 0.82` — peak Y, units; for Unity to render the arc.

2. **Spaces** in `__init__`:
   - `observation_space` shape `(13,)` → `(15,)` (added `boss_jump_t`, `player_jump_t`).
   - `action_space` `Discrete(8)` → `Discrete(9)` (added action 8 = Jump).

3. **State variables** in `__init__` and `reset()`:
   - `boss_jump_t: float`, `boss_jump_cooldown: int`, `boss_y: float`.
   - Player counterparts.

4. **`step()` timer block** — added cooldown decrements and float-timer advancement. Stagger freezes the jump.

5. **`step()` action gating** — boss/player state set to `5` (Airborne) when `jump_t > 0`. Combat actions only register if `jump_t == 0`.

6. **Damage resolution** — both attack branches now gate on `not self._is_airborne(target)` in addition to dodge i-frames. This is the entire jump-dodge mechanic.

7. **`_execute_boss_action` / `_execute_player_action`** — refactored from monolithic 27-line functions into 6-line dispatchers that pick (a) or (b).

8. **`_ground_action(entity, action)`** — function (a). New. Generalizes the old per-entity logic by taking `entity` as a string (`"boss"` or `"player"`) and using `getattr`/`setattr`. Handles X/Z movement, block, dodge, and attack-state setup at full speed.

9. **`_aerial_action(entity, action)`** — function (b). New. X/Z at `AERIAL_SPEED_MUL × MOVE_SPEED`, plus the parabolic Y arc.

10. **`_try_initiate_jump(entity)`** — guards action 8: only fires if grounded and off cooldown.

11. **`_is_airborne(entity)`** — the predicate `jump_t >= JUMP_STARTUP`. One line, used in two places.

12. **`_get_obs`** — appended `boss_jump_t / JUMP_TOTAL` and `player_jump_t / JUMP_TOTAL`. Also bumped two normalization divisors: action-history `/7.0 → /8.0` (9 actions now) and player-state `/4.0 → /5.0` (state 5 added).

### `RL_Project/python/tests/test_simple_env.py`

- `test_observation_bounds`: shape assertion updated from `(13,)` to `(15,)` (one line + a matching print message).
- No new tests added. **Worth adding** (see below): an explicit test that proves the 1.5-step jump-dodge boundary.

---

## Phase model — exactly when each thing happens

```
ground          startup        airborne          ground
   │               │              │                │
   ▼ jump_t = 0    ▼ 0 < t < 1.5  ▼ 1.5 ≤ t < 4.0  ▼ jump_t = 0
   │               │              │                │
   │ all 8 actions │ stuck mid-   │ X/Z at half    │ all 8 actions
   │   available   │   transition │   speed; Y > 0 │   available
   │               │              │                │
   │ hittable      │ HITTABLE     │ IMMUNE to      │ hittable
   │   normally    │  (vulnerable │  ground attacks│
   │               │   startup)   │  ("jump-dodge")│
   │               │              │                │
   │ combat works  │ combat = no-op (jump_t > 0)   │ combat works
```

---

## Validation

All 5 tests in [test_simple_env.py](../python/tests/test_simple_env.py) pass after the change:

| # | Test | Result |
|---|---|---|
| 1 | Gymnasium compliance | PASSED |
| 2 | 100 random episodes | PASSED — avg 45.8 steps, player wins 94% (scripted player is strong vs. random boss policy) |
| 3 | Speed benchmark (100 000 steps) | PASSED — ~13 200 steps/sec |
| 4 | Combat accuracy (5 hand-computed scenarios) | PASSED — light=5, heavy=15, OOR=0, blocked=2.5, i-frames=0 |
| 5 | Observation bounds (1 000 random steps, `(15,)` shape) | PASSED |

### Speed regression — root cause and fix

Speed dropped from ~44 500 → ~13 200 steps/sec (about 3.4× slower). Cause: the generic `_ground_action(entity, action)` and `_aerial_action(entity, action)` use `getattr`/`setattr` to handle both boss and player via one method, and `getattr` is significantly slower than direct attribute access in a tight inner loop.

**This is acceptable for now** because (a) 13 k/sec is still ~30× faster than Unity training, (b) the test threshold of 10 k/sec still passes, (c) the validation suite for Phase 0 is not gated on raw throughput.

**If we need the old speed back later**, the fix is straightforward: specialize the helpers (`_ground_action_boss`, `_ground_action_player`, `_aerial_action_boss`, `_aerial_action_player`) with direct attribute access. Costs ~50 lines of duplicated code; recovers most of the lost throughput.

---

## What was NOT updated (and needs to be later)

The following files still describe the pre-jump world. They are not broken in isolation — `SimpleCombatEnv` runs standalone — but the moment Unity comes online, all three must be brought in sync:

- **[combat_spec.md](combat_spec.md)** — Player States table is missing state 5 (Airborne). Normalization rules section is missing `jump_t`. No "Jump" parameter table exists.
- **[shared/protocol.json](../shared/protocol.json)** — Action message defines only 8 actions; needs `8: "Jump"`. State message has no `player_y`, `boss_y`, or `jump_t` fields. `player_state` range is still `[0, 4]`.
- **[python/envs/unity_tower_env.py](../python/envs/unity_tower_env.py)** — `observation_space` is still `(13,)`. `_state_to_obs` doesn't read jump fields. The `/ 7.0` and `/ 4.0` normalization divisors are stale. Action space is still `Discrete(8)`.
- **[python/training/reward_configs.py](../python/training/reward_configs.py)** — No reward terms for jump behavior. Likely worth adding:
  - `jump_spam` (negative) — to prevent bunny-hopping.
  - `jump_dodge` (positive) — to reward successful airborne escapes.
  - Per-shadow tuning so Peter is more spammy and Matron is more grounded.

A separate pass should also harden the scripted player at [simple_combat_env.py:277](../python/envs/simple_combat_env.py#L277) to use jumps — otherwise the boss learns "jump is a free dodge" overfitted to a never-jumping opponent.

---

## Open design questions

- **Should jump have any cost beyond cooldown?** Right now it's free if off cooldown. Real action games often spend stamina; if/when stamina is added, jump should drain it.
- **Should mid-air collision push entities apart?** Currently two entities can occupy the same X/Z while one is airborne. Probably fine — both are points, not boxes.
- **Should heavy attacks track upward to hit airborne targets?** Currently no — all attacks miss airborne. A future "anti-air" heavy could be its own action ID, or a flag on the existing heavy.
- **Should the scripted player be allowed to jump?** Yes — see above. Otherwise the boss overfits.

---

## Quick reference — the jump-dodge predicate

```python
def _is_airborne(self, entity: str) -> bool:
    return getattr(self, f"{entity}_jump_t") >= JUMP_STARTUP
```

That single function is the entire jump-dodge mechanic. Read together with the damage-resolution gates in `step()`, it implements the full timing rule: jump 1.5+ ticks before impact → escape; jump later → eat the hit.
