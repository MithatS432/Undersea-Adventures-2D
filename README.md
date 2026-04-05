# 🌊 Undersea Adventures 2D
> **Mobile Match-3 Puzzle Game** with reactive power-ups and dynamic combo-driven gameplay.

![Banner](https://via.placeholder.com/800x300?text=Undersea+Adventures+Gameplay+Preview)

## 📖 Overview
Undersea Adventures is a high-performance, mobile-optimized match-3 experience. Built with a focus on **satisfying feedback loops** and **optimized rendering**, it features a robust grid system capable of handling complex chain reactions.

### Key Highlights
* **Dynamic Grid:** Gravity-based collapse and tile refilling.
* **Optimized Performance:** Uses Sprite Atlases to minimize draw calls—crucial for mobile thermal overhead.
* **Reactive VFX:** Feedback intensity scales with combo length.

---

## 🎮 Game Mechanics

### 💣 Power-up System
| Power-up | Pattern | Effect |
| :--- | :--- | :--- |
| **Small Bomb** | 4 Horizontal | Clears entire row |
| **Large Bomb** | 4 Vertical | Clears entire column |
| **Color Bomb** | T or L Shape | Clears all tiles of a specific type |

### 🎯 Match Logic
* **Match 3:** Standard point gain.
* **Match 4:** Generates specialized bombs based on swipe direction.
* **5+ / L-T Shape:** Triggers enhanced board-clear events.

---

## 🛠️ Technical Architecture

### Core Systems
1. **Grid Manager:** Handles the NxN coordinate system and tile spawning.
2. **Match Engine:** Recursive algorithm for detecting horizontal, vertical, and L-shaped clusters.
3. **Sequence Controller:** Manages the "Wait-for-Collapse" state machine to ensure combos trigger in the correct order.
