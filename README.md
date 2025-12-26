# Earthbound Voxel Engine

A custom-built voxel sandbox game engine written in C# using OpenTK and OpenGL 4.0. Developed from scratch, focusing on performance, procedural generation, and engine architecture.

**Current Version:** Indev 0.90

## 🎮 Features Implemented

### Core Engine
- **Custom OpenGL Rendering:** Written using OpenTK 4.0 with manual memory management (VAO/VBO/IBO).
- **Texture Arrays:** Optimized rendering using Texture2DArray to reduce draw calls and texture binding overhead.
- **Chunk System:** Infinite world generation with 16x128x16 chunks, utilizing mesh optimization (face culling).
- **Save/Load System:** 
  - Binary region file format (similar to Anvil) with GZip compression.
  - JSON-based player data persistence (inventory, health, position).
  - Dynamic world icon capture upon exit.

### World Generation
- **Procedural Terrain:** Powered by `FastNoiseLite` with multiple noise layers:
  - Continental noise (Ocean/Land separation).
  - Peaks noise (Mountain generation).
  - Temperature & Humidity maps.
- **Biomes:** Plains, Forests, Deserts, Snowy Plains, Snowy Forests, Mountains, Frozen Oceans.
- **Structures:** Procedural trees and vegetation.

### Gameplay Mechanics
- **Physics:** AABB (Axis-Aligned Bounding Box) collision detection and Raycasting.
- **Lighting Engine:** Voxel-based sunlight propagation and block light emission (smooth transitions, day/night cycle).
- **Fluids:** Cellular automata-based water and lava flow logic.
- **Inventory System:** Drag & drop support, hotbar, stacking logic.
- **Survival Elements:** Health system, fall damage, fire spreading/burning mechanics.

### Audio
- **3D Sound Engine:** Built on OpenAL Soft.
- **Dynamic Footsteps:** Sounds change based on the material under the player (Grass, Stone, Wood, Water, etc.).
- **Environment:** Ambient sound effects.

## 🛠 Tech Stack
- **Language:** C# (.NET 8.0)
- **Graphics:** OpenTK (OpenGL wrapper)
- **Image Processing:** StbImageSharp
- **Audio:** OpenAL.NETCore, NVorbis (OGG support)

## ⌨ Controls
- **W, A, S, D** - Movement
- **Space** - Jump / Swim Up
- **Ctrl** - Sprint
- **Mouse Left** - Break Block
- **Mouse Right** - Place Block / Interact
- **E** - Open/Close Inventory
- **Scroll / 1-9** - Select Hotbar Slot
- **G** - Drop Item
- **F3** - Debug Info (FPS, TPS, Coordinates)
- **F4** - Unstuck (Teleport to sky + Reset fall damage)
- **Esc** - Pause Menu

## 💿 Installation & Build

1. Clone the repository.
2. Open the solution in **Visual Studio 2022** or **JetBrains Rider**.
3. Ensure `.NET 8.0 SDK` is installed.
4. Build and Run.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
You are free to use this code, but you must include the original copyright notice and attribution.
