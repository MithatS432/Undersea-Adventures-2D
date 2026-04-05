## 🌊 Undersea Adventures – 2D High-Performance Mobile Match-3 Engine
Undersea Adventures is a mobile-focused puzzle game built on fluid grid mechanics, dynamic combo chains, and an advanced power-up system.  
The project aims to deliver complex matching algorithms and reactive in-game events through an optimized architecture.

## 🎮 Game Mechanics & Logic
The game dynamically generates special objects with unique abilities based on match patterns:

### 💎 Match Types & Special Objects
| Match Pattern        | Created Object   | Functionality                                |
|----------------------|-----------------|----------------------------------------------|
| 3 Tiles (Any)        | Standard Clear  | Removes matched tiles                        |
| 4 Tiles (Horizontal) | 💣 Horizontal Bomb | Clears the entire row                        |
| 4 Tiles (Vertical)   | 🧨 Vertical Bomb   | Clears the entire column                     |
| T or L Shape         | 💥 Large Bomb      | Triggers a 3x3 radial explosion              |
| 5 Tiles (Line)       | 🌈 Color Bomb      | Clears all tiles of a specific color         |

## 🕹️ Gameplay Flow
- **Input:** Smooth touch/mouse drag-and-drop system for tile swapping  
- **Chain Reactions:** Gravity-based tile collapse triggering sequential combos  
- **Feedback System:** Dynamic visual feedback scaling intensity based on combo length  

## 🛠️ Technical Features

### Core Systems
- **Grid Management:** NxN coordinate system handling tile states, spawning, and gravity-based displacement  
- **Match Detection Engine:** High-performance recursive algorithm for horizontal, vertical, and cluster-based (T/L) patterns  
- **Sequence Controller:** Robust state-manager handling timing between destruction, falling animations, and new match evaluations  

### Mobile Optimization
- **Sprite Atlas Integration:** Optimized rendering by grouping textures, reducing draw calls  
- **Memory Management:** Efficient object lifecycle handling to ensure stable FPS on mid-to-low end devices  

## 📋 Technical Stack
- **Engine:** Unity 2D  
- **Language:** C# (OOP, Event-driven architecture)  
- **Target Platforms:** Android  
