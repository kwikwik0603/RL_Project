# Combat Math Specification — The Tower Within
**Version:** 1.0
**Agreed by:** Vishal Ravi Muthaiah, Satvik N Kaushik
**Date:** 2026-04-16

This document defines the exact combat parameters used in both Unity and the simplified Python environment. Any changes must be synced between both implementations.

## Arena
| Parameter       | Value         |
|-----------------|---------------|
| Arena size      | 10 x 10 units |
| Arena bounds    | (0, 0) to (10, 10) |
| Spawn player    | (3.0, 5.0)    |
| Spawn boss      | (7.0, 5.0)    |

## Timing
| Parameter           | Value       |
|---------------------|-------------|
| Decision step       | 0.2 seconds |
| Steps per second    | 5           |
| Max steps per fight | 500 (100 seconds) |

## Movement
| Parameter       | Value          |
|-----------------|----------------|
| Move speed      | 1.0 units/step |
| Dodge distance  | 2.0 units      |
| Strafe speed    | 1.0 units/step |

## Combat
| Parameter              | Value   |
|------------------------|---------|
| Starting HP (player)   | 100     |
| Starting HP (boss)     | 100     |
| Light attack damage    | 5       |
| Heavy attack damage    | 15      |
| Attack range           | 2.0 units |
| Heavy attack cooldown  | 3 steps |
| Block damage reduction | 50%     |

## Dodge
| Parameter              | Value             |
|------------------------|-------------------|
| Dodge distance         | 2.0 units         |
| Dodge i-frames         | 1 step (0.2 sec)  |
| Dodge direction        | Perpendicular to facing |

## Stagger
| Parameter              | Value   |
|------------------------|---------|
| Heavy hit staggers     | Yes     |
| Stagger duration       | 2 steps |
| Stagger = vulnerable   | Yes (cannot block/dodge) |

## Player States
| ID | State      | Description                        |
|----|------------|------------------------------------|
| 0  | Idle       | No action in progress              |
| 1  | Attacking  | Currently in attack animation      |
| 2  | Blocking   | Actively blocking (damage reduced) |
| 3  | Dodging    | In dodge i-frames                  |
| 4  | Staggered  | Hit by heavy attack, vulnerable    |

## Normalization (for observation space)
- HP: divide by 100 → [0, 1]
- Position: divide by 10 → [0, 1]
- Distance: divide by arena diagonal (√200 ≈ 14.14) → [0, 1]
- Action IDs: divide by 7 → [0, 1]
- Player state: divide by 4 → [0, 1]
- Floor number: divide by 60 → [0, 1]
- Boss ID: divide by 4 → [0, 1]
