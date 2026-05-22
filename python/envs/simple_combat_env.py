"""
Simplified headless Python combat environment for fast RL training.
Replicates Unity's combat math without rendering. Runs at thousands of steps/sec.

Combat parameters match docs/combat_spec.md exactly.
"""

import math
import gymnasium as gym
import numpy as np
from gymnasium import spaces


# ── Combat constants (from combat_spec.md) ──────────────────────────────────
ARENA_SIZE = 10.0
ARENA_DIAGONAL = math.sqrt(2 * ARENA_SIZE ** 2)  # ~14.14

PLAYER_SPAWN = np.array([3.0, 5.0])
BOSS_SPAWN = np.array([7.0, 5.0])

STARTING_HP = 100.0
LIGHT_ATTACK_DMG = 5.0
HEAVY_ATTACK_DMG = 15.0
ATTACK_RANGE = 2.0
HEAVY_COOLDOWN_STEPS = 3
BLOCK_REDUCTION = 0.5

MOVE_SPEED = 1.0
DODGE_DISTANCE = 2.0
DODGE_IFRAMES = 1       # steps of invulnerability
STAGGER_DURATION = 2    # steps

MAX_STEPS = 500

# ── JUMP: action 8 (Jump). 4-step parabolic arc split into two phases:
#     0.0 ≤ jump_t < 1.5  → startup, still on ground, still hittable
#     1.5 ≤ jump_t < 4.0  → airborne, immune to ground attacks (jump-dodge)
#   Movement actions still work mid-jump but at half speed.
JUMP_STARTUP = 1.5            # steps before airborne i-frames begin
JUMP_TOTAL = 4.0              # total jump duration (0.8 s at 5 steps/sec)
JUMP_COOLDOWN_STEPS = 5
AERIAL_SPEED_MUL = 0.5        # horizontal speed multiplier while airborne
MAX_JUMP_HEIGHT = 0.82        # peak Y (units) — visual only; combat keys off jump_t


class SimpleCombatEnv(gym.Env):
    """Fast headless combat env matching Unity's combat math."""

    metadata = {"render_modes": ["text"]}

    def __init__(self, reward_config=None, render_mode=None):
        super().__init__()

        # Same obs/action space as UnityTowerEnv — policies are interchangeable.
        # JUMP: shape grew 13 → 15 (added boss_jump_t, player_jump_t),
        # JUMP: actions grew 8 → 9 (added action 8 = Jump).
        self.observation_space = spaces.Box(
            low=0.0, high=1.0, shape=(15,), dtype=np.float32
        )
        self.action_space = spaces.Discrete(9)

        self.reward_config = reward_config
        self.render_mode = render_mode

        # State variables (initialized in reset)
        self.player_pos = None
        self.boss_pos = None
        self.player_hp = 0.0
        self.boss_hp = 0.0
        self.step_count = 0

        self.player_last_actions = [0, 0, 0]
        self.player_state = 0  # 0=idle
        self.boss_state = 0

        self.boss_heavy_cooldown = 0
        self.boss_dodge_iframes = 0
        self.player_dodge_iframes = 0
        self.boss_stagger_timer = 0
        self.player_stagger_timer = 0

        self.boss_last_actions = [0, 0, 0]

        # JUMP: vertical state. jump_t is a FLOAT so the 1.5-step boundary
        # JUMP: lands cleanly on a tick where the entity is still grounded
        # JUMP: (t=1.0 < 1.5 = hittable; next tick t=2.0 ≥ 1.5 = airborne).
        self.boss_jump_t = 0.0
        self.boss_jump_cooldown = 0
        self.boss_y = 0.0
        self.player_jump_t = 0.0
        self.player_jump_cooldown = 0
        self.player_y = 0.0

        self.floor_number = 1
        self.boss_id = 0

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        self.player_pos = PLAYER_SPAWN.copy()
        self.boss_pos = BOSS_SPAWN.copy()
        self.player_hp = STARTING_HP
        self.boss_hp = STARTING_HP
        self.step_count = 0

        self.player_last_actions = [0, 0, 0]
        self.player_state = 0
        self.boss_state = 0

        self.boss_heavy_cooldown = 0
        self.boss_dodge_iframes = 0
        self.player_dodge_iframes = 0
        self.boss_stagger_timer = 0
        self.player_stagger_timer = 0

        self.boss_last_actions = [0, 0, 0]

        # JUMP: reset vertical state
        self.boss_jump_t = 0.0
        self.boss_jump_cooldown = 0
        self.boss_y = 0.0
        self.player_jump_t = 0.0
        self.player_jump_cooldown = 0
        self.player_y = 0.0

        if options:
            self.floor_number = options.get("floor_number", 1)
            self.boss_id = options.get("boss_id", 0)

        return self._get_obs(), {}

    def step(self, action):
        action = int(action)
        self.step_count += 1

        prev_player_hp = self.player_hp
        prev_boss_hp = self.boss_hp

        # ── Decrement timers ────────────────────────────────────────────
        if self.boss_heavy_cooldown > 0:
            self.boss_heavy_cooldown -= 1
        if self.boss_dodge_iframes > 0:
            self.boss_dodge_iframes -= 1
        if self.player_dodge_iframes > 0:
            self.player_dodge_iframes -= 1
        if self.boss_stagger_timer > 0:
            self.boss_stagger_timer -= 1
        if self.player_stagger_timer > 0:
            self.player_stagger_timer -= 1
        # JUMP: cooldowns and float-timer advancement. Stagger freezes the
        # JUMP: jump so a stunlocked entity doesn't "finish" a jump in mid-air.
        if self.boss_jump_cooldown > 0:
            self.boss_jump_cooldown -= 1
        if self.player_jump_cooldown > 0:
            self.player_jump_cooldown -= 1
        if self.boss_jump_t > 0 and self.boss_stagger_timer <= 0:
            self.boss_jump_t += 1.0
            if self.boss_jump_t >= JUMP_TOTAL:
                self.boss_jump_t = 0.0
                self.boss_y = 0.0
        if self.player_jump_t > 0 and self.player_stagger_timer <= 0:
            self.player_jump_t += 1.0
            if self.player_jump_t >= JUMP_TOTAL:
                self.player_jump_t = 0.0
                self.player_y = 0.0

        # ── Boss action (if not staggered) ──────────────────────────────
        boss_attacked = False
        boss_blocking = False
        boss_is_heavy = False

        if self.boss_stagger_timer > 0:
            self.boss_state = 4  # staggered, can't act
        else:
            # JUMP: state 5 = Airborne. _execute_boss_action dispatches to
            # JUMP: ground (a) or aerial (b) based on jump_t.
            self.boss_state = 5 if self.boss_jump_t > 0 else 0
            self._execute_boss_action(action)
            # JUMP: combat actions only register when fully grounded
            if self.boss_jump_t == 0:
                if action == 4:
                    boss_attacked = True
                elif action == 5 and self.boss_heavy_cooldown <= 0:
                    boss_attacked = True
                    boss_is_heavy = True
                elif action == 6:
                    boss_blocking = True

        # ── Scripted player action ──────────────────────────────────────
        player_action = self._scripted_player_action()
        player_attacked = False
        player_blocking = False
        player_is_heavy = False

        if self.player_stagger_timer > 0:
            self.player_state = 4
        else:
            self.player_state = 5 if self.player_jump_t > 0 else 0
            self._execute_player_action(player_action)
            if self.player_jump_t == 0:  # JUMP: grounded-only combat
                if player_action == 4:
                    player_attacked = True
                elif player_action == 5:
                    player_attacked = True
                    player_is_heavy = True
                elif player_action == 6:
                    player_blocking = True

        # ── Resolve combat ──────────────────────────────────────────────
        dist = self._distance()

        # Boss hits player
        if boss_attacked and dist <= ATTACK_RANGE:
            # JUMP: airborne target (jump_t ≥ 1.5) is immune to ground attacks;
            # JUMP: a target still in jump startup (jump_t < 1.5) is hittable.
            if self.player_dodge_iframes <= 0 and not self._is_airborne("player"):
                dmg = HEAVY_ATTACK_DMG if boss_is_heavy else LIGHT_ATTACK_DMG
                if player_blocking:
                    dmg *= BLOCK_REDUCTION
                self.player_hp -= dmg
                if boss_is_heavy:
                    self.player_stagger_timer = STAGGER_DURATION
                    self.player_state = 4
            if boss_is_heavy:
                self.boss_heavy_cooldown = HEAVY_COOLDOWN_STEPS

        # Player hits boss
        if player_attacked and dist <= ATTACK_RANGE:
            if self.boss_dodge_iframes <= 0 and not self._is_airborne("boss"):  # JUMP
                dmg = HEAVY_ATTACK_DMG if player_is_heavy else LIGHT_ATTACK_DMG
                if boss_blocking:
                    dmg *= BLOCK_REDUCTION
                self.boss_hp -= dmg
                if player_is_heavy:
                    self.boss_stagger_timer = STAGGER_DURATION
                    self.boss_state = 4

        # ── Clamp ───────────────────────────────────────────────────────
        self.player_hp = max(0.0, self.player_hp)
        self.boss_hp = max(0.0, self.boss_hp)

        # ── Update action history ───────────────────────────────────────
        self.player_last_actions.pop(0)
        self.player_last_actions.append(player_action)
        self.boss_last_actions.pop(0)
        self.boss_last_actions.append(action)

        # ── Check termination ───────────────────────────────────────────
        terminated = self.player_hp <= 0 or self.boss_hp <= 0
        truncated = self.step_count >= MAX_STEPS

        # ── Compute reward ──────────────────────────────────────────────
        reward = self._compute_reward(
            prev_player_hp, prev_boss_hp, action, terminated
        )

        obs = self._get_obs()
        info = {
            "player_hp": self.player_hp,
            "boss_hp": self.boss_hp,
            "step": self.step_count,
            "boss_won": self.player_hp <= 0,
            "player_won": self.boss_hp <= 0,
        }

        if self.render_mode == "text":
            self.render()

        return obs, reward, terminated, truncated, info

    # ── Movement & action execution ─────────────────────────────────────

    def _execute_boss_action(self, action: int):
        # JUMP: dispatcher → jump-init / aerial (b) / ground (a)
        if action == 8:
            self._try_initiate_jump("boss")
            return
        if self.boss_jump_t > 0:
            self._aerial_action("boss", action)
        else:
            self._ground_action("boss", action)

    def _execute_player_action(self, action: int):
        # JUMP: same dispatcher pattern as boss
        if action == 8:
            self._try_initiate_jump("player")
            return
        if self.player_jump_t > 0:
            self._aerial_action("player", action)
        else:
            self._ground_action("player", action)

    # ── (a) Ground action: X/Z plane only, full speed, all combat available ─
    def _ground_action(self, entity: str, action: int):
        pos = getattr(self, f"{entity}_pos")
        target = self.player_pos if entity == "boss" else self.boss_pos
        direction = target - pos
        dist = max(0.01, np.linalg.norm(direction))
        unit = direction / dist

        if action == 0:    # Move toward
            pos += unit * MOVE_SPEED
        elif action == 1:  # Move away
            pos -= unit * MOVE_SPEED
        elif action == 2:  # Strafe left
            pos += np.array([-unit[1], unit[0]]) * MOVE_SPEED
        elif action == 3:  # Strafe right
            pos += np.array([unit[1], -unit[0]]) * MOVE_SPEED
        elif action == 6:  # Block
            setattr(self, f"{entity}_state", 2)
        elif action == 7:  # Dodge
            pos += np.array([-unit[1], unit[0]]) * DODGE_DISTANCE
            setattr(self, f"{entity}_dodge_iframes", DODGE_IFRAMES)
            setattr(self, f"{entity}_state", 3)

        if action in (4, 5):
            setattr(self, f"{entity}_state", 1)  # attacking

        np.clip(pos, 0.0, ARENA_SIZE, out=pos)

    # ── (b) Aerial action: X/Z at half speed + Y from jump arc; combat = no-op
    def _aerial_action(self, entity: str, action: int):
        pos = getattr(self, f"{entity}_pos")
        target = self.player_pos if entity == "boss" else self.boss_pos
        direction = target - pos
        dist = max(0.01, np.linalg.norm(direction))
        unit = direction / dist
        speed = MOVE_SPEED * AERIAL_SPEED_MUL

        if action == 0:
            pos += unit * speed
        elif action == 1:
            pos -= unit * speed
        elif action == 2:
            pos += np.array([-unit[1], unit[0]]) * speed
        elif action == 3:
            pos += np.array([unit[1], -unit[0]]) * speed
        # Actions 4–7 ignored mid-air. Action 8 already handled by dispatcher.

        # JUMP: parabolic Y arc from current jump_t — 0 at t=0 and t=TOTAL,
        # JUMP: peak at midpoint. Visual only; combat checks jump_t directly.
        t = getattr(self, f"{entity}_jump_t")
        norm_t = t / JUMP_TOTAL
        setattr(self, f"{entity}_y", max(0.0, MAX_JUMP_HEIGHT * 4 * norm_t * (1 - norm_t)))

        np.clip(pos, 0.0, ARENA_SIZE, out=pos)

    def _try_initiate_jump(self, entity: str):
        # JUMP: action 8 only fires if entity is grounded and off cooldown.
        if (getattr(self, f"{entity}_jump_t") == 0
                and getattr(self, f"{entity}_jump_cooldown") <= 0):
            setattr(self, f"{entity}_jump_t", 1.0)
            setattr(self, f"{entity}_jump_cooldown", JUMP_COOLDOWN_STEPS)

    def _is_airborne(self, entity: str) -> bool:
        # JUMP: True past the 1.5-step startup → immune to ground attacks.
        return getattr(self, f"{entity}_jump_t") >= JUMP_STARTUP

    def _scripted_player_action(self) -> int:
        """Simple scripted opponent: approach, attack when in range, block randomly."""
        dist = self._distance()
        rng = self.np_random

        if dist > ATTACK_RANGE:
            return 0  # move toward boss
        else:
            roll = rng.random()
            if roll < 0.5:
                return 4  # light attack
            elif roll < 0.65:
                return 5  # heavy attack
            elif roll < 0.85:
                return 6  # block
            else:
                return 7  # dodge

    # ── Observations ────────────────────────────────────────────────────

    def _get_obs(self) -> np.ndarray:
        dist_norm = self._distance() / ARENA_DIAGONAL
        obs = np.array([
            self.player_hp / STARTING_HP,
            self.boss_hp / STARTING_HP,
            self.player_pos[0] / ARENA_SIZE,
            self.player_pos[1] / ARENA_SIZE,
            self.boss_pos[0] / ARENA_SIZE,
            self.boss_pos[1] / ARENA_SIZE,
            dist_norm,
            self.player_last_actions[0] / 8.0,   # JUMP: was /7.0 → 9 actions now
            self.player_last_actions[1] / 8.0,
            self.player_last_actions[2] / 8.0,
            self.player_state / 5.0,             # JUMP: was /4.0 → state 5 added
            self.floor_number / 60.0,
            self.boss_id / 4.0,
            self.boss_jump_t / JUMP_TOTAL,       # JUMP: 0..1 boss jump progress
            self.player_jump_t / JUMP_TOTAL,     # JUMP: 0..1 player jump progress
        ], dtype=np.float32)
        return np.clip(obs, 0.0, 1.0)

    def _distance(self) -> float:
        return float(np.linalg.norm(self.player_pos - self.boss_pos))

    # ── Reward ──────────────────────────────────────────────────────────

    def _compute_reward(self, prev_player_hp, prev_boss_hp, action, terminated) -> float:
        if self.reward_config is None:
            return self._default_reward(prev_player_hp, prev_boss_hp, terminated)

        cfg = self.reward_config
        reward = 0.0

        # r_damage_dealt: damage the boss dealt to the player this step
        dmg_dealt = prev_player_hp - self.player_hp
        reward += cfg.get("damage_dealt", 1.0) * (dmg_dealt / STARTING_HP)

        # r_damage_taken: damage the boss took this step
        dmg_taken = prev_boss_hp - self.boss_hp
        reward += cfg.get("damage_taken", -0.5) * (dmg_taken / STARTING_HP)

        # r_kill / r_death
        if terminated:
            if self.player_hp <= 0:
                reward += cfg.get("kill", 10.0)
            if self.boss_hp <= 0:
                reward += cfg.get("death", -5.0)

        # r_proximity: reward/penalty based on distance
        dist_norm = self._distance() / ARENA_DIAGONAL
        reward += cfg.get("proximity", 0.1) * (0.5 - dist_norm)

        # r_punish_spam: penalize repeating same action 3x in a row
        if (len(self.boss_last_actions) >= 3 and
                self.boss_last_actions[-1] == self.boss_last_actions[-2] == self.boss_last_actions[-3]):
            reward += cfg.get("punish_spam", -0.3)

        # r_exploit_opening: reward attacking a staggered/attacking player
        if action in (4, 5) and self.player_state in (1, 4):
            if self._distance() <= ATTACK_RANGE:
                reward += cfg.get("exploit_opening", 0.5)

        # r_challenge: penalize instant kills (fight too short)
        if terminated and self.step_count < 50:  # 10 seconds at 5 steps/sec
            reward += cfg.get("challenge", -2.0)

        return reward

    def _default_reward(self, prev_player_hp, prev_boss_hp, terminated) -> float:
        reward = 0.0
        dmg_dealt = prev_player_hp - self.player_hp
        if dmg_dealt > 0:
            reward += dmg_dealt / STARTING_HP * 10.0
        dmg_taken = prev_boss_hp - self.boss_hp
        if dmg_taken > 0:
            reward -= dmg_taken / STARTING_HP * 5.0
        if terminated:
            if self.player_hp <= 0:
                reward += 10.0
            elif self.boss_hp <= 0:
                reward -= 5.0
        return reward

    # ── Render (text mode for debugging) ────────────────────────────────

    def render(self):
        grid_size = 20
        grid = [["." for _ in range(grid_size)] for _ in range(grid_size)]

        px = int(self.player_pos[0] / ARENA_SIZE * (grid_size - 1))
        py = int(self.player_pos[1] / ARENA_SIZE * (grid_size - 1))
        bx = int(self.boss_pos[0] / ARENA_SIZE * (grid_size - 1))
        by = int(self.boss_pos[1] / ARENA_SIZE * (grid_size - 1))

        px, py = max(0, min(grid_size-1, px)), max(0, min(grid_size-1, py))
        bx, by = max(0, min(grid_size-1, bx)), max(0, min(grid_size-1, by))

        grid[py][px] = "P"
        grid[by][bx] = "B"

        print(f"\n Step {self.step_count} | Player HP: {self.player_hp:.0f} | Boss HP: {self.boss_hp:.0f}")
        for row in grid:
            print(" ".join(row))
