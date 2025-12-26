# Earthbound Voxel Engine

**Current Version:** Indev 0.90 (Prototype)

## ⚠️ Project Status: On Hold
Development of this project is currently paused. The author is temporarily shifting focus to the development of a military vehicle simulation game. This repository serves as a snapshot of the engine's current state.

## 🤖 Development Method (AI Generated)
**This entire project (100% of the code) was written by Google Gemini 3.0 Pro.**

- **Role of Repository Owner:** Direction, Game Design, Ideation, Debugging supervision.
- **Role of AI:** Implementation of all C# logic, OpenGL rendering, Physics, and Math.

The code demonstrates what is possible to achieve in a short timeframe (approx. 5-7 days) using LLM-assisted programming, but it may contain non-standard solutions or simplifications typical of AI generation.

## 🐛 Known Issues & Limitations
This is an **Indev** build and is **not** a finished product. Please be aware of the following:

1.  **Bugs:** There are approximately 7 known major bugs (visual artifacts, collision edge-cases, physics jitters).
2.  **Simplifications:** Some systems (like the lighting engine and fluid dynamics) use simplified logic for performance and code brevity.
3.  **Unused Assets:** The `assets` folder contains texture and audio files that are not yet implemented or used in the code.
4.  **Optimization:** While the game uses Texture Arrays and Chunk Meshing, further optimization is required for large render distances.

## 🛠 Technical Overview
A voxel sandbox engine written from scratch in C# using OpenTK (OpenGL 4.0).

### Features Implemented
- **Rendering:** Custom Voxel Mesh generation, Texture Arrays, Face Culling.
- **World Generation:** Procedural terrain using FastNoiseLite (Biomes: Forest, Desert, Snow, Mountains).
- **Lighting:** Voxel-based sunlight propagation and block emission (Torches/Lava).
- **Physics:** AABB Collision detection, basic fluid dynamics (Water/Lava flow).
- **Inventory:** Drag & Drop system, Hotbar, Item stacking.
- **Save/Load:** Binary region file format with GZip compression + JSON player data.
- **Audio:** 3D Sound system using OpenAL (Dynamic footsteps based on block type).

## 📂 Repository Tools (Python Scripts)
You may find `.py` scripts in the root directory. These are **development tools** used to interact with the AI, not part of the game engine itself:

- **`a.py`**: A utility script used to scan the `assets` folder. It generated lists of file paths (e.g., .ogg files) to provide the AI with the correct directory structure.
- **`all.py`**: A script used to concatenate all `.cs` source files into a single `all.txt` file. This allowed the AI to read the entire codebase context in one go for debugging and updating.

## ⌨ Controls
- **W, A, S, D**: Movement
- **Space**: Jump / Swim Up
- **Ctrl**: Sprint
- **Mouse Left**: Break Block
- **Mouse Right**: Place Block / Interact
- **E**: Open/Close Inventory
- **G**: Drop Item
- **F3**: Debug Info
- **F4**: Unstuck (Emergency teleport to sky)
- **Esc**: Pause Menu

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
**Attribution Requirement:** If you use this code, you must credit the original author (**Rislan2283922**) as per the MIT license terms.
