"""
Gymnasium wrapper for the Unity Tower Within game.
Connects to Unity's TCP server and exposes the standard Gym interface
so Stable-Baselines3 can train/evaluate against the live game.
"""

import gymnasium as gym
import numpy as np
from gymnasium import spaces

from python.utils.tcp_client import TCPClient


class UnityTowerEnv(gym.Env):
    """Gymnasium environment that wraps Unity via TCP."""

    metadata = {"render_modes": []}

    def __init__(self, host: str = "localhost", port: int = 9876):
        super().__init__()

        self.host = host
        self.port = port
        self.client = TCPClient(host, port)

        # 13-dim observation: player_hp(1) + boss_hp(1) + player_pos(2) +
        # boss_pos(2) + distance(1) + player_last_actions(3) +
        # player_state(1) + floor_number(1) + boss_id(1)
        self.observation_space = spaces.Box(
            low=0.0, high=1.0, shape=(13,), dtype=np.float32
        )

        # 8 discrete actions: 0-3 movement, 4-5 attacks, 6 block, 7 dodge
        self.action_space = spaces.Discrete(8)

        self._last_boss_hp = 1.0
        self._last_player_hp = 1.0

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        if not self.client.is_connected():
            self.client.connect()
        else:
            self.client.send_reset()

        state = self.client.receive_state()
        obs = self._state_to_obs(state)

        self._last_boss_hp = state["boss_hp"]
        self._last_player_hp = state["player_hp"]

        return obs, {"raw_state": state}

    def step(self, action):
        self.client.send_action(int(action))
        state = self.client.receive_state()

        obs = self._state_to_obs(state)
        reward = self._compute_reward(state)
        terminated = state["done"]
        truncated = False

        self._last_boss_hp = state["boss_hp"]
        self._last_player_hp = state["player_hp"]

        return obs, reward, terminated, truncated, {"raw_state": state}

    def close(self):
        self.client.close()

    def _state_to_obs(self, state: dict) -> np.ndarray:
        """Flatten state dict into a (13,) numpy array, all values in [0, 1]."""
        obs = np.array([
            state["player_hp"],
            state["boss_hp"],
            state["player_pos"][0],
            state["player_pos"][1],
            state["boss_pos"][0],
            state["boss_pos"][1],
            state["distance"],
            state["player_last_actions"][0] / 7.0,
            state["player_last_actions"][1] / 7.0,
            state["player_last_actions"][2] / 7.0,
            state["player_state"] / 4.0,
            state["floor_number"] / 60.0,
            state["boss_id"] / 4.0,
        ], dtype=np.float32)
        return np.clip(obs, 0.0, 1.0)

    def _compute_reward(self, state: dict) -> float:
        """Placeholder reward: +1 damage dealt, -1 damage taken, ±bonus on kill/death."""
        reward = 0.0

        # Reward for dealing damage to the player
        player_hp_delta = self._last_player_hp - state["player_hp"]
        if player_hp_delta > 0:
            reward += player_hp_delta * 10.0  # scale up since HP is normalized

        # Penalty for taking damage
        boss_hp_delta = self._last_boss_hp - state["boss_hp"]
        if boss_hp_delta > 0:
            reward -= boss_hp_delta * 5.0

        # Kill bonus / death penalty
        if state["done"]:
            if state["player_hp"] <= 0:
                reward += 10.0  # boss wins
            elif state["boss_hp"] <= 0:
                reward -= 5.0   # boss loses

        return reward
