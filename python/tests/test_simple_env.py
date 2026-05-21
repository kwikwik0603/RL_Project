"""
Validation tests for SimpleCombatEnv.
Run: python -m python.tests.test_simple_env
"""

import time
import numpy as np

from python.envs.simple_combat_env import (
    SimpleCombatEnv, STARTING_HP, LIGHT_ATTACK_DMG, HEAVY_ATTACK_DMG,
    BLOCK_REDUCTION, ATTACK_RANGE, ARENA_SIZE
)


def test_gym_compliance():
    """Run SB3's env_checker to verify Gymnasium compliance."""
    print("Test 1: Gymnasium compliance check...")
    from stable_baselines3.common.env_checker import check_env
    env = SimpleCombatEnv()
    check_env(env, warn=True)
    env.close()
    print("  PASSED — env is Gym-compliant\n")


def test_random_episodes():
    """Run 100 random episodes and report statistics."""
    print("Test 2: Random episode statistics (100 episodes)...")
    env = SimpleCombatEnv()

    episode_lengths = []
    episode_rewards = []
    boss_wins = 0
    player_wins = 0
    timeouts = 0

    for _ in range(100):
        obs, _ = env.reset()
        total_reward = 0.0
        steps = 0

        while True:
            action = env.action_space.sample()
            obs, reward, terminated, truncated, info = env.step(action)
            total_reward += reward
            steps += 1
            if terminated or truncated:
                break

        episode_lengths.append(steps)
        episode_rewards.append(total_reward)

        if info.get("boss_won"):
            boss_wins += 1
        elif info.get("player_won"):
            player_wins += 1
        else:
            timeouts += 1

    env.close()

    print(f"  Avg episode length: {np.mean(episode_lengths):.1f} steps")
    print(f"  Avg reward:         {np.mean(episode_rewards):.2f}")
    print(f"  Boss win rate:      {boss_wins}%")
    print(f"  Player win rate:    {player_wins}%")
    print(f"  Timeouts:           {timeouts}%")
    print("  PASSED\n")


def test_benchmark_speed():
    """Benchmark: time 100,000 random steps. Target: 10,000+ steps/sec."""
    print("Test 3: Speed benchmark (100,000 steps)...")
    env = SimpleCombatEnv()
    env.reset()

    num_steps = 100_000
    start = time.perf_counter()

    for _ in range(num_steps):
        action = env.action_space.sample()
        _, _, terminated, truncated, _ = env.step(action)
        if terminated or truncated:
            env.reset()

    elapsed = time.perf_counter() - start
    steps_per_sec = num_steps / elapsed

    env.close()

    print(f"  {num_steps:,} steps in {elapsed:.2f}s")
    print(f"  Speed: {steps_per_sec:,.0f} steps/sec")
    status = "PASSED" if steps_per_sec >= 10_000 else "WARNING — below 10k target"
    print(f"  {status}\n")


def test_combat_accuracy():
    """Verify specific combat scenarios match expected outcomes."""
    print("Test 4: Combat accuracy checks...")
    env = SimpleCombatEnv()

    # Scenario 1: Light attack in range should deal 5 damage
    env.reset()
    env.player_pos = np.array([5.0, 5.0])
    env.boss_pos = np.array([5.5, 5.0])  # distance = 0.5, within range
    env.player_dodge_iframes = 0
    env.player_stagger_timer = 0
    prev_hp = env.player_hp

    # Force player to idle (not blocking/dodging) by overriding scripted action
    env._scripted_player_action = lambda: 0  # player just moves, doesn't block
    env.step(4)  # boss light attack

    dmg = prev_hp - env.player_hp
    assert dmg == LIGHT_ATTACK_DMG, f"  FAIL: Light attack did {dmg} dmg, expected {LIGHT_ATTACK_DMG}"
    print(f"  Scenario 1 (light attack in range):  {dmg} dmg — OK")

    # Scenario 2: Heavy attack in range should deal 15 damage
    env.reset()
    env.player_pos = np.array([5.0, 5.0])
    env.boss_pos = np.array([5.5, 5.0])
    env._scripted_player_action = lambda: 0
    prev_hp = env.player_hp

    env.step(5)  # boss heavy attack

    dmg = prev_hp - env.player_hp
    assert dmg == HEAVY_ATTACK_DMG, f"  FAIL: Heavy attack did {dmg} dmg, expected {HEAVY_ATTACK_DMG}"
    print(f"  Scenario 2 (heavy attack in range):  {dmg} dmg — OK")

    # Scenario 3: Attack out of range should deal 0 damage
    env.reset()
    env.player_pos = np.array([1.0, 1.0])
    env.boss_pos = np.array([8.0, 8.0])  # way out of range
    env._scripted_player_action = lambda: 0
    prev_hp = env.player_hp

    env.step(4)

    dmg = prev_hp - env.player_hp
    assert dmg == 0, f"  FAIL: Out-of-range attack did {dmg} dmg, expected 0"
    print(f"  Scenario 3 (attack out of range):    {dmg} dmg — OK")

    # Scenario 4: Blocked light attack should deal 2.5 damage
    env.reset()
    env.player_pos = np.array([5.0, 5.0])
    env.boss_pos = np.array([5.5, 5.0])
    env._scripted_player_action = lambda: 6  # player blocks
    prev_hp = env.player_hp

    env.step(4)

    dmg = prev_hp - env.player_hp
    expected = LIGHT_ATTACK_DMG * BLOCK_REDUCTION
    assert dmg == expected, f"  FAIL: Blocked attack did {dmg} dmg, expected {expected}"
    print(f"  Scenario 4 (blocked light attack):   {dmg} dmg — OK")

    # Scenario 5: Attack during dodge i-frames should deal 0 damage
    env.reset()
    env.player_pos = np.array([5.0, 5.0])
    env.boss_pos = np.array([5.5, 5.0])
    env.player_dodge_iframes = 2  # player is in i-frames
    env._scripted_player_action = lambda: 0
    prev_hp = env.player_hp

    env.step(4)

    dmg = prev_hp - env.player_hp
    assert dmg == 0, f"  FAIL: Attack during i-frames did {dmg} dmg, expected 0"
    print(f"  Scenario 5 (attack during i-frames): {dmg} dmg — OK")

    env.close()
    print("  All combat accuracy checks PASSED\n")


def test_observation_bounds():
    """Verify all observations stay within [0, 1]."""
    print("Test 5: Observation bounds check (1000 steps)...")
    env = SimpleCombatEnv()
    env.reset()

    for _ in range(1000):
        action = env.action_space.sample()
        obs, _, terminated, truncated, _ = env.step(action)
        assert obs.min() >= 0.0, f"  FAIL: obs has value below 0: {obs.min()}"
        assert obs.max() <= 1.0, f"  FAIL: obs has value above 1: {obs.max()}"
        assert obs.shape == (13,), f"  FAIL: obs shape is {obs.shape}, expected (13,)"
        if terminated or truncated:
            env.reset()

    env.close()
    print("  All observations within [0, 1], shape (13,) — PASSED\n")


if __name__ == "__main__":
    print("=" * 60)
    print("SimpleCombatEnv Validation Suite")
    print("=" * 60 + "\n")

    test_gym_compliance()
    test_random_episodes()
    test_benchmark_speed()
    test_combat_accuracy()
    test_observation_bounds()

    print("=" * 60)
    print("ALL TESTS PASSED")
    print("=" * 60)
