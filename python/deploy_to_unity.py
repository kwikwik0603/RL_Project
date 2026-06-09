"""
Deploy a TRAINED PPO policy to Unity — the boss acts with intent, not randomly.

This is the "real" counterpart to demo_random_agent.py. Instead of sampling
random actions, it loads a trained policy and calls model.predict() each step.
The policy trained in SimpleCombatEnv deploys here unchanged because both envs
share the same 13-dim observation and 8-action space.

Usage:
  1. Press Play in Unity (S_BossTCPBridge listening on 9876).
  2. Run:
       python -m python.deploy_to_unity --boss_id 1
     where boss_id selects which trained model to load (models/ppo_boss_<id>.zip).

  --deterministic (default True) makes the boss pick its best action each step,
  which looks far more purposeful than stochastic sampling.
"""

import argparse
import os

from stable_baselines3 import PPO

from python.envs.unity_tower_env import UnityTowerEnv
from python.training.reward_configs import SHADOW_NAMES

MODELS_DIR = os.path.join(os.path.dirname(__file__), "models")

NUM_EPISODES = 5
MAX_STEPS_PER_EPISODE = 1000


def main():
    parser = argparse.ArgumentParser(description="Deploy a trained boss policy to Unity")
    parser.add_argument("--boss_id", type=int, default=1,
                        help="Which trained shadow to deploy (0-4). Loads models/ppo_boss_<id>.zip")
    parser.add_argument("--host", default="localhost")
    parser.add_argument("--port", type=int, default=9876)
    parser.add_argument("--stochastic", action="store_true",
                        help="Sample from the policy instead of taking the best action")
    args = parser.parse_args()

    model_path = os.path.join(MODELS_DIR, f"ppo_boss_{args.boss_id}")
    if not os.path.exists(model_path + ".zip"):
        print(f"ERROR: No trained model at {model_path}.zip")
        print(f"Train one first:  python -m python.training.train_ppo --boss_id {args.boss_id} --timesteps 200000")
        return

    name = SHADOW_NAMES.get(args.boss_id, f"boss_{args.boss_id}")
    print(f"Loading policy for {name} from {model_path}.zip")
    model = PPO.load(model_path, device="cpu")

    env = UnityTowerEnv(host=args.host, port=args.port)
    deterministic = not args.stochastic

    try:
        for episode in range(1, NUM_EPISODES + 1):
            obs, info = env.reset()
            total_reward = 0.0
            step = 0

            print(f"\n{'='*50}\nEpisode {episode} — {name}\n{'='*50}")

            for step in range(1, MAX_STEPS_PER_EPISODE + 1):
                action, _ = model.predict(obs, deterministic=deterministic)
                obs, reward, terminated, truncated, info = env.step(action)
                total_reward += reward

                if step % 50 == 0:
                    raw = info.get("raw_state", {})
                    print(f"  Step {step:4d} | Action: {int(action)} | "
                          f"Reward: {reward:+.2f} | "
                          f"Player HP: {raw.get('player_hp', '?'):.2f} | "
                          f"Boss HP: {raw.get('boss_hp', '?'):.2f}")

                if terminated or truncated:
                    break

            print(f"  Episode {episode} ended at step {step} | Total reward: {total_reward:+.2f}")

    except ConnectionRefusedError:
        print("ERROR: Could not connect to Unity on "
              f"{args.host}:{args.port}. Press Play in Unity first.")
    except ConnectionError as e:
        print(f"\nServer disconnected: {e}")
    except KeyboardInterrupt:
        print("\nInterrupted by user.")
    finally:
        env.close()
        print("\nDone.")


if __name__ == "__main__":
    main()
