"""
PPO training pipeline for The Tower Within boss AI.

Usage:
  # Smoke test (Phase 0)
  python -m python.training.train_ppo

  # Train specific shadow character
  python -m python.training.train_ppo --boss_id 1 --timesteps 100000

  # Train all shadows
  python -m python.training.train_ppo --all --timesteps 50000
"""

import argparse
import os

from stable_baselines3 import PPO
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.evaluation import evaluate_policy

from python.envs.simple_combat_env import SimpleCombatEnv
from python.training.reward_configs import SHADOW_CONFIGS, SHADOW_NAMES


MODELS_DIR = os.path.join(os.path.dirname(__file__), "..", "models")
LOGS_DIR = os.path.join(os.path.dirname(__file__), "..", "logs")


def train_boss(boss_id: int, timesteps: int = 10_000, seed: int = 42):
    """Train a PPO agent for a specific shadow character."""
    config = SHADOW_CONFIGS[boss_id]
    name = SHADOW_NAMES[boss_id]

    print(f"\n{'='*60}")
    print(f"Training: {name} (boss_id={boss_id})")
    print(f"Timesteps: {timesteps:,}")
    print(f"Reward config: {config}")
    print(f"{'='*60}\n")

    env = SimpleCombatEnv(reward_config=config)
    env = Monitor(env)

    model = PPO(
        "MlpPolicy",
        env,
        verbose=1,
        seed=seed,
        device="cpu",
        tensorboard_log=LOGS_DIR,
        learning_rate=3e-4,
        n_steps=2048,
        batch_size=64,
        n_epochs=10,
        gamma=0.99,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=0.01,
    )

    model.learn(
        total_timesteps=timesteps,
        tb_log_name=f"boss_{boss_id}_{name.split('(')[0].strip().lower()}",
    )

    # Save
    os.makedirs(MODELS_DIR, exist_ok=True)
    model_path = os.path.join(MODELS_DIR, f"ppo_boss_{boss_id}")
    model.save(model_path)
    print(f"\nModel saved to {model_path}")

    # Evaluate
    mean_reward, std_reward = evaluate_policy(model, env, n_eval_episodes=10)
    print(f"Evaluation — Mean reward: {mean_reward:.2f} +/- {std_reward:.2f}")

    env.close()
    return model


def main():
    parser = argparse.ArgumentParser(description="Train PPO boss agents")
    parser.add_argument("--boss_id", type=int, default=0, help="Shadow character ID (0-4)")
    parser.add_argument("--timesteps", type=int, default=10_000, help="Training timesteps")
    parser.add_argument("--all", action="store_true", help="Train all 5 shadow characters")
    parser.add_argument("--seed", type=int, default=42, help="Random seed")
    args = parser.parse_args()

    os.makedirs(LOGS_DIR, exist_ok=True)

    if args.all:
        for boss_id in range(5):
            train_boss(boss_id, args.timesteps, args.seed)
    else:
        train_boss(args.boss_id, args.timesteps, args.seed)

    print("\nTraining complete.")
    print(f"View TensorBoard: tensorboard --logdir={os.path.abspath(LOGS_DIR)}")


if __name__ == "__main__":
    main()
