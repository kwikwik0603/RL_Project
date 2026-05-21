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


class SimpleCombatEnv(gym.Env):
    """Fast headless combat env matching Unity's combat math."""

    metadata = {"render_modes": ["text"]}

    def __init__(self, reward_config=None, render_mode=None):
        super().__init__()

        # Same obs/action space as UnityTowerEnv — policies are interchangeable
        self.observation_space = spaces.Box(
            low=0.0, high=1.0, shape=(13,), dtype=np.float32
        )
        self.action_space = spaces.Discrete(8)

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

        # ── Boss action (if not staggered) ──────────────────────────────
        boss_attacked = False
        boss_blocking = False
        boss_is_heavy = False

        if self.boss_stagger_timer > 0:
            self.boss_state = 4  # staggered, can't act
        else:
            self.boss_state = 0
            self._execute_boss_action(action)
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
            self.player_state = 0
            self._execute_player_action(player_action)
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
            if self.player_dodge_iframes <= 0:
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
            if self.boss_dodge_iframes <= 0:
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
        direction = self.player_pos - self.boss_pos
        dist = max(0.01, np.linalg.norm(direction))
        unit = direction / dist

        if action == 0:  # Move toward
            self.boss_pos += unit * MOVE_SPEED
        elif action == 1:  # Move away
            self.boss_pos -= unit * MOVE_SPEED
        elif action == 2:  # Strafe left
            perp = np.array([-unit[1], unit[0]])
            self.boss_pos += perp * MOVE_SPEED
        elif action == 3:  # Strafe right
            perp = np.array([unit[1], -unit[0]])
            self.boss_pos += perp * MOVE_SPEED
        elif action == 6:  # Block (no movement)
            self.boss_state = 2
        elif action == 7:  # Dodge
            perp = np.array([-unit[1], unit[0]])
            self.boss_pos += perp * DODGE_DISTANCE
            self.boss_dodge_iframes = DODGE_IFRAMES
            self.boss_state = 3

        if action in (4, 5):
            self.boss_state = 1  # attacking

        # Clamp to arena
        self.boss_pos = np.clip(self.boss_pos, 0.0, ARENA_SIZE)

    def _execute_player_action(self, action: int):
        direction = self.boss_pos - self.player_pos
        dist = max(0.01, np.linalg.norm(direction))
        unit = direction / dist

        if action == 0:  # Move toward
            self.player_pos += unit * MOVE_SPEED
        elif action == 1:  # Move away
            self.player_pos -= unit * MOVE_SPEED
        elif action == 2:  # Strafe left
            perp = np.array([-unit[1], unit[0]])
            self.player_pos += perp * MOVE_SPEED
        elif action == 3:  # Strafe right
            perp = np.array([unit[1], -unit[0]])
            self.player_pos += perp * MOVE_SPEED
        elif action == 6:  # Block
            self.player_state = 2
        elif action == 7:  # Dodge
            perp = np.array([-unit[1], unit[0]])
            self.player_pos += perp * DODGE_DISTANCE
            self.player_dodge_iframes = DODGE_IFRAMES
            self.player_state = 3

        if action in (4, 5):
            self.player_state = 1  # attacking

        self.player_pos = np.clip(self.player_pos, 0.0, ARENA_SIZE)

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
            self.player_last_actions[0] / 7.0,
            self.player_last_actions[1] / 7.0,
            self.player_last_actions[2] / 7.0,
            self.player_state / 4.0,
            self.floor_number / 60.0,
            self.boss_id / 4.0,
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
