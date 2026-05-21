"""
Mock Unity TCP Server for testing the Python TCP client without Unity running.
Sends fake game state, receives actions, and simulates basic state transitions.
"""

import socket
import json
import random
import math


def create_state(player_pos=None, boss_pos=None, player_hp=1.0, boss_hp=1.0,
                 floor_number=1, boss_id=0, done=False):
    """Build a state dict matching the protocol contract."""
    if player_pos is None:
        player_pos = [0.3, 0.5]
    if boss_pos is None:
        boss_pos = [0.7, 0.6]

    dx = player_pos[0] - boss_pos[0]
    dy = player_pos[1] - boss_pos[1]
    distance = min(1.0, math.sqrt(dx * dx + dy * dy) / math.sqrt(2))

    return {
        "player_hp": round(player_hp, 4),
        "boss_hp": round(boss_hp, 4),
        "player_pos": [round(p, 4) for p in player_pos],
        "boss_pos": [round(b, 4) for b in boss_pos],
        "distance": round(distance, 4),
        "player_last_actions": [random.randint(0, 7) for _ in range(3)],
        "player_state": random.randint(0, 4),
        "floor_number": floor_number,
        "boss_id": boss_id,
        "done": done
    }


def run_mock_server(host="localhost", port=9876, max_steps=100_000):
    """Run a mock Unity server that sends state and receives actions."""
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((host, port))
    server.listen(1)
    print(f"[Mock Server] Listening on {host}:{port}...")

    conn, addr = server.accept()
    print(f"[Mock Server] Python client connected from {addr}")

    buffer = ""
    player_hp = 1.0
    boss_hp = 1.0
    player_pos = [0.3, 0.5]
    boss_pos = [0.7, 0.6]
    step = 0

    try:
        while step < max_steps:
            # Send state
            done = player_hp <= 0 or boss_hp <= 0
            state = create_state(player_pos, boss_pos, player_hp, boss_hp, done=done)
            msg = json.dumps(state) + "\n"
            conn.sendall(msg.encode("utf-8"))

            if done:
                # Wait for reset command
                while "\n" not in buffer:
                    data = conn.recv(4096)
                    if not data:
                        print("[Mock Server] Client disconnected")
                        return
                    buffer += data.decode("utf-8")
                line, buffer = buffer.split("\n", 1)
                cmd = json.loads(line)
                if cmd.get("command") == "reset":
                    player_hp = 1.0
                    boss_hp = 1.0
                    player_pos = [0.3, 0.5]
                    boss_pos = [0.7, 0.6]
                    print(f"[Mock Server] Reset at step {step}")
                    step = 0
                continue

            # Receive action
            while "\n" not in buffer:
                data = conn.recv(4096)
                if not data:
                    print("[Mock Server] Client disconnected")
                    return
                buffer += data.decode("utf-8")

            line, buffer = buffer.split("\n", 1)
            action_data = json.loads(line)
            action = action_data.get("action", 0)

            # Simulate basic state changes
            move_speed = 0.05
            if action == 0:  # Move toward
                dx = player_pos[0] - boss_pos[0]
                dy = player_pos[1] - boss_pos[1]
                dist = max(0.01, math.sqrt(dx*dx + dy*dy))
                boss_pos[0] += (dx / dist) * move_speed
                boss_pos[1] += (dy / dist) * move_speed
            elif action == 1:  # Move away
                dx = player_pos[0] - boss_pos[0]
                dy = player_pos[1] - boss_pos[1]
                dist = max(0.01, math.sqrt(dx*dx + dy*dy))
                boss_pos[0] -= (dx / dist) * move_speed
                boss_pos[1] -= (dy / dist) * move_speed
            elif action == 2:  # Strafe left
                boss_pos[1] += move_speed
            elif action == 3:  # Strafe right
                boss_pos[1] -= move_speed
            elif action == 4:  # Light attack
                dx = player_pos[0] - boss_pos[0]
                dy = player_pos[1] - boss_pos[1]
                if math.sqrt(dx*dx + dy*dy) < 0.2:
                    player_hp -= 0.05
            elif action == 5:  # Heavy attack
                dx = player_pos[0] - boss_pos[0]
                dy = player_pos[1] - boss_pos[1]
                if math.sqrt(dx*dx + dy*dy) < 0.2:
                    player_hp -= 0.15

            # Clamp positions to arena
            boss_pos[0] = max(0.0, min(1.0, boss_pos[0]))
            boss_pos[1] = max(0.0, min(1.0, boss_pos[1]))

            # Simulate player fighting back randomly
            if random.random() < 0.15:
                dx = player_pos[0] - boss_pos[0]
                dy = player_pos[1] - boss_pos[1]
                if math.sqrt(dx*dx + dy*dy) < 0.2:
                    boss_hp -= 0.05

            # Simulate player moving randomly
            player_pos[0] += random.uniform(-0.02, 0.02)
            player_pos[1] += random.uniform(-0.02, 0.02)
            player_pos[0] = max(0.0, min(1.0, player_pos[0]))
            player_pos[1] = max(0.0, min(1.0, player_pos[1]))

            player_hp = max(0.0, player_hp)
            boss_hp = max(0.0, boss_hp)

            step += 1

    except (ConnectionError, BrokenPipeError):
        print("[Mock Server] Connection lost")
    finally:
        conn.close()
        server.close()
        print(f"[Mock Server] Shut down after {step} steps")


if __name__ == "__main__":
    run_mock_server()
