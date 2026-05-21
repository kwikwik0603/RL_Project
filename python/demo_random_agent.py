"""
Random agent demo — controls the enemy in Unity with random actions.
Usage:
  1. Start Unity in Play mode (TCPServer listening on port 9876)
  2. Run: python -m python.demo_random_agent

Can also run against the mock server for testing:
  Terminal 1: python -m python.utils.mock_server
  Terminal 2: python -m python.demo_random_agent
"""

from python.envs.unity_tower_env import UnityTowerEnv

NUM_EPISODES = 5
MAX_STEPS_PER_EPISODE = 1000


def main():
    env = UnityTowerEnv(host="localhost", port=9876)

    try:
        for episode in range(1, NUM_EPISODES + 1):
            obs, info = env.reset()
            total_reward = 0.0
            step = 0

            print(f"\n{'='*50}")
            print(f"Episode {episode}")
            print(f"{'='*50}")

            for step in range(1, MAX_STEPS_PER_EPISODE + 1):
                action = env.action_space.sample()
                obs, reward, terminated, truncated, info = env.step(action)
                total_reward += reward

                if step % 50 == 0:
                    raw = info.get("raw_state", {})
                    print(
                        f"  Step {step:4d} | "
                        f"Action: {action} | "
                        f"Reward: {reward:+.2f} | "
                        f"Player HP: {raw.get('player_hp', '?'):.2f} | "
                        f"Boss HP: {raw.get('boss_hp', '?'):.2f}"
                    )

                if terminated or truncated:
                    break

            raw = info.get("raw_state", {})
            winner = "BOSS" if raw.get("player_hp", 1) <= 0 else "PLAYER" if raw.get("boss_hp", 1) <= 0 else "TIMEOUT"
            print(f"\n  Episode {episode} ended at step {step}")
            print(f"  Total reward: {total_reward:+.2f}")
            print(f"  Result: {winner}")

    except ConnectionRefusedError:
        print("ERROR: Could not connect to Unity TCP server on localhost:9876.")
        print("Make sure Unity is running with TCPServer active, or start the mock server:")
        print("  python -m python.utils.mock_server")
    except ConnectionError as e:
        print(f"\nServer disconnected: {e}")
        print("(Mock server hit its step cap, or Unity closed. This is expected if the mock server finished.)")
    except KeyboardInterrupt:
        print("\nInterrupted by user.")
    finally:
        env.close()
        print("\nDone.")


if __name__ == "__main__":
    main()
