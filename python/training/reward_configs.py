"""
Reward weight configurations for each shadow character (Nemesis system).

Each config is a dict of sub-reward weights. The SimpleCombatEnv uses these
to compute a weighted reward sum at each step. Different weights produce
different fighting personalities from the same PPO architecture.

Sub-rewards:
  damage_dealt    — boss hits the player (+)
  damage_taken    — boss takes damage (-)
  kill            — player HP reaches 0 (+)
  death           — boss HP reaches 0 (-)
  proximity       — controls preferred fighting distance (+ = close, - = far)
  punish_spam     — penalty for repeating the same action 3x (-)
  exploit_opening — reward for counter-hitting vulnerable player (+)
  challenge       — penalty if fight ends too quickly (-)
"""

# Balanced baseline — used for initial training and Sam (friend) fallback
BASE_CONFIG = {
    "damage_dealt": 1.0,
    "damage_taken": -0.5,
    "kill": 10.0,
    "death": -5.0,
    "proximity": 0.1,
    "punish_spam": -0.3,
    "exploit_opening": 0.5,
    "challenge": -2.0,
}

# ── Shadow Character Configs ────────────────────────────────────────────────

# Matron (Mother) — Patient, defensive, counter-attacker
# High: damage_taken (avoids hits), exploit_opening (punishes mistakes), challenge
# Low: damage_dealt (passive), proximity (prefers distance)
MATRON_CONFIG = {
    "damage_dealt": 0.5,
    "damage_taken": -1.0,      # 2x base — hates getting hit
    "kill": 10.0,
    "death": -5.0,
    "proximity": -0.03,        # slight preference for distance
    "punish_spam": -0.3,
    "exploit_opening": 1.25,   # 2.5x base — lives for counter-hits
    "challenge": -3.0,         # 1.5x base — wants long fights
}

# Peter (Father) — Aggressive, relentless, in-your-face
# High: damage_dealt (constant pressure), proximity (stays close), kill
# Low: damage_taken (doesn't care), challenge (fine with quick kills)
PETER_CONFIG = {
    "damage_dealt": 2.0,       # 2x — loves dealing damage
    "damage_taken": -0.15,     # 0.3x — barely cares about own HP
    "kill": 15.0,              # 1.5x — very kill-oriented
    "death": -5.0,
    "proximity": 0.2,          # 2x — wants to stay close
    "punish_spam": -0.3,
    "exploit_opening": 0.5,
    "challenge": -1.0,         # 0.5x — fine with quick fights
}

# Nicole (Girlfriend/Wife) — Hit-and-run, evasive, surgical punisher
# High: exploit_opening (precise), damage_taken (elusive), proximity (distance)
# Low: damage_dealt (selective), kill (patient)
NICOLE_CONFIG = {
    "damage_dealt": 0.5,       # 0.5x — doesn't chase damage
    "damage_taken": -1.0,      # 2x — very evasive
    "kill": 8.0,               # 0.8x — patient
    "death": -5.0,
    "proximity": -0.15,        # 1.5x favoring distance
    "punish_spam": -0.3,
    "exploit_opening": 1.25,   # 2.5x — surgical counter-attacks
    "challenge": -2.0,
}

# Sam — Son — Erratic, emotional, unpredictable
# High: damage_dealt (bursts of aggression)
# Low: damage_taken (reckless), exploit_opening (not strategic)
# Special: spam penalty reduced (allows repetitive bursts)
SAM_SON_CONFIG = {
    "damage_dealt": 1.5,
    "damage_taken": -0.15,     # 0.3x — reckless
    "kill": 10.0,
    "death": -5.0,
    "proximity": 0.1,
    "punish_spam": -0.03,      # ~0.1x — spam is barely penalized
    "exploit_opening": 0.15,   # 0.3x — not strategic
    "challenge": -2.0,
}

# Sam — Best Friend — Mirrors the player (behavioral cloning)
# Uses balanced weights as fallback before enough BC data is collected
SAM_FRIEND_CONFIG = {
    "damage_dealt": 1.0,
    "damage_taken": -0.5,
    "kill": 10.0,
    "death": -5.0,
    "proximity": 0.1,
    "punish_spam": -0.3,
    "exploit_opening": 0.5,
    "challenge": -2.0,
}

# Map boss_id to config for easy lookup
SHADOW_CONFIGS = {
    0: MATRON_CONFIG,
    1: PETER_CONFIG,
    2: NICOLE_CONFIG,
    3: SAM_SON_CONFIG,
    4: SAM_FRIEND_CONFIG,
}

SHADOW_NAMES = {
    0: "Matron (Mother)",
    1: "Peter (Father)",
    2: "Nicole (Girlfriend/Wife)",
    3: "Sam (Son)",
    4: "Sam (Best Friend)",
}
