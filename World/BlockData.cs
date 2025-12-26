using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World
{
    public enum BlockType
    {
        AIR = 0,
        DIRT = 1, GRASS = 2, STONE = 3, COBBLESTONE = 4,
        LOG = 5, LEAVES = 6, PLANKS = 7,
        BEDROCK = 8, GRAVEL = 9, SAND = 10, SANDSTONE = 11,
        WATER = 12, LAVA = 13, ICE = 14, SNOW = 15, SNOW_LAYER = 16,
        GOLD_ORE = 17, IRON_ORE = 18, COAL_ORE = 19, DIAMOND_ORE = 20,
        GLASS = 21, SPONGE = 22, MOSSY_COBBLE = 23, OBSIDIAN = 24, BRICKS = 25,
        TNT = 26, BOOKSHELF = 27, WORKBENCH = 28, FURNACE = 29,
        DEAD_BUSH = 30, FLOWER_RED = 31, FLOWER_YELLOW = 32, MUSHROOM_RED = 33, MUSHROOM_BROWN = 34,
        STICK = 35, WOOD_SWORD = 36, WOOD_PICKAXE = 37, WOOD_SHOVEL = 38, WOOD_AXE = 39,
        FIRE = 40, LIGHTER = 41, LIGHTER_ON = 42,

        // --- НОВЫЕ БЛОКИ (ID из генератора) ---
        STONE_STAIRS = 50,
        PLANKS_STAIRS = 51,
        CHRISTMAS_TREE = 52

    }

    public enum Faces { FRONT, BACK, LEFT, RIGHT, TOP, BOTTOM }

    public struct FaceDataRaw
    {
        public static readonly Dictionary<Faces, List<Vector3>> rawVertexData = new Dictionary<Faces, List<Vector3>>
        {
            {Faces.FRONT, new List<Vector3>() { new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f) }},
            {Faces.BACK, new List<Vector3>() { new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f) }},
            {Faces.LEFT, new List<Vector3>() { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f) }},
            {Faces.RIGHT, new List<Vector3>() { new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f) }},
            {Faces.TOP, new List<Vector3>() { new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f) }},
            {Faces.BOTTOM, new List<Vector3>() { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) }},
        };
    }

    public struct AABB
    {
        public Vector3 Min;
        public Vector3 Max;
        public AABB(Vector3 min, Vector3 max) { Min = min; Max = max; }
    }
}