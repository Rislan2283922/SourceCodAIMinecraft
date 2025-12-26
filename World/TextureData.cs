using System.Collections.Generic;
using System; // Для Enum
using EarthBound.Graphics;

namespace EarthBound.World
{
    public static class TextureData
    {
        private static Dictionary<BlockType, Dictionary<Faces, string>> _blockPaths
        = new Dictionary<BlockType, Dictionary<Faces, string>>();

        public static Dictionary<BlockType, Dictionary<Faces, int>> BlockLayerIndices
            = new Dictionary<BlockType, Dictionary<Faces, int>>();

        public static List<int> BreakLayerIndices = new List<int>();

        static TextureData()
        {
            // --- NATURAL BLOCKS ---
            AssignAll(BlockType.DIRT, "blocks/nature/dirt.png");
            AssignAll(BlockType.STONE, "blocks/stone/stone.png");
            AssignAll(BlockType.COBBLESTONE, "blocks/stone/cobblestone.png");
            AssignAll(BlockType.MOSSY_COBBLE, "blocks/stone/mossy_stone.png");
            AssignAll(BlockType.SAND, "blocks/stone/sand.png");
            AssignAll(BlockType.GRAVEL, "blocks/stone/gravel.png");
            AssignAll(BlockType.BEDROCK, "blocks/stone/stone.png");
            AssignAll(BlockType.OBSIDIAN, "blocks/stone/stone.png");
            AssignAll(BlockType.SPONGE, "blocks/nature/sponge.png");
            AssignAll(BlockType.SANDSTONE, "blocks/stone/sand.png");

            // --- SNOW & ICE ---
            AssignAll(BlockType.SNOW, "blocks/nature/snow.png");
            AssignAll(BlockType.SNOW_LAYER, "blocks/nature/snow.png");
            AssignAll(BlockType.ICE, "blocks/nature/ice.png");

            // --- PLANTS ---
            AssignAll(BlockType.LEAVES, "blocks/nature/leaves.png");
            AssignAll(BlockType.DEAD_BUSH, "blocks/nature/dead_bush.png");
            AssignAll(BlockType.FLOWER_RED, "blocks/plants/flower_1.png");
            AssignAll(BlockType.FLOWER_YELLOW, "blocks/plants/flower_2.png");
            AssignAll(BlockType.MUSHROOM_RED, "blocks/plants/mushroom_red.png");
            AssignAll(BlockType.MUSHROOM_BROWN, "blocks/plants/mushroom_brown.png");
            AssignAll(BlockType.GLASS, "blocks/utility/glass_old_style.png");

            // --- MANUFACTURED BLOCKS ---
            AssignAll(BlockType.PLANKS, "blocks/wood/planks.png");
            AssignAll(BlockType.BRICKS, "blocks/stone/bricks.png");

            // --- ORES ---
            AssignAll(BlockType.GOLD_ORE, "blocks/stone/ore_gold.png");
            AssignAll(BlockType.IRON_ORE, "blocks/stone/ore_iron.png");
            AssignAll(BlockType.COAL_ORE, "blocks/stone/ore_coal.png");
            AssignAll(BlockType.DIAMOND_ORE, "blocks/stone/ore_diamond.png");

            // --- COMPLEX BLOCKS ---

            // Grass: Side needs tinting now
            AssignSideTopBot(BlockType.GRASS, "blocks/nature/grass_side.png", "blocks/nature/grass_top.png", "blocks/nature/dirt.png");

            // Log
            AssignSideTopBot(BlockType.LOG, "blocks/wood/log_side.png", "blocks/wood/log_top.png", "blocks/wood/log_top.png");
            AssignAll((BlockType)50, "blocks/stone/cobblestone.png");
            AssignAll((BlockType)51, "blocks/wood/planks.png");

            // --- CHRISTMAS TREE (Разные текстуры для низа и верха) ---
            _blockPaths[BlockType.CHRISTMAS_TREE] = new Dictionary<Faces, string>();
            // По умолчанию все грани - низ
            foreach (Faces f in Enum.GetValues(typeof(Faces)))
                _blockPaths[BlockType.CHRISTMAS_TREE][f] = "blocks/nature/spruce_bottom.png";
            // Верхнюю грань (которую мы используем как идентификатор для верхушки) ставим на spruce_top
            _blockPaths[BlockType.CHRISTMAS_TREE][Faces.TOP] = "blocks/nature/spruce_top.png";
            // --------------------------------------------------------

            // TNT
            AssignSideTopBot(BlockType.TNT, "blocks/utility/tnt_side.png", "blocks/utility/tnt_top.png", "blocks/utility/tnt_top.png");

            // Workbench
            AssignSideTopBot(BlockType.WORKBENCH, "blocks/utility/workbench_side.png", "blocks/utility/workbench_top.png", "blocks/utility/workbench_bottom.png");

            // Furnace
            AssignSideTopBot(BlockType.FURNACE, "blocks/utility/furnace_side.png", "blocks/utility/furnace_top.png", "blocks/utility/furnace_top.png");
            _blockPaths[BlockType.FURNACE][Faces.FRONT] = "blocks/utility/furnace_front.png";

            // Bookshelf
            AssignAll(BlockType.BOOKSHELF, "blocks/utility/bookshelf.png");
            _blockPaths[BlockType.BOOKSHELF][Faces.TOP] = "blocks/wood/planks.png";
            _blockPaths[BlockType.BOOKSHELF][Faces.BOTTOM] = "blocks/wood/planks.png";

            // --- FLUIDS & FIRE ---
            AssignAll(BlockType.WATER, "blocks/liquids/water.png");
            AssignAll(BlockType.LAVA, "blocks/liquids/lava.png");
            AssignAll(BlockType.FIRE, "blocks/fire/fire_0.png");

            // --- ITEMS ---
            AssignAll(BlockType.STICK, "items/materials/stick.png");
            AssignAll(BlockType.LIGHTER, "items/misc/lighter_closed.png");
            AssignAll(BlockType.LIGHTER_ON, "items/misc/lighter_open.png");
            AssignAll(BlockType.WOOD_SWORD, "items/tools/sword_wood.png");
            AssignAll(BlockType.WOOD_PICKAXE, "items/tools/pickaxe_wood.png");
            AssignAll(BlockType.WOOD_AXE, "items/tools/axe_wood.png");
            AssignAll(BlockType.WOOD_SHOVEL, "items/tools/shovel_wood.png");
        }

        private static void AssignAll(BlockType type, string path)
        {
            _blockPaths[type] = new Dictionary<Faces, string>();
            foreach (Faces f in Enum.GetValues(typeof(Faces)))
                _blockPaths[type][f] = path;
        }

        private static void AssignSideTopBot(BlockType type, string side, string top, string bot)
        {
            _blockPaths[type] = new Dictionary<Faces, string>
            {
                { Faces.LEFT, side }, { Faces.RIGHT, side }, { Faces.FRONT, side }, { Faces.BACK, side },
                { Faces.TOP, top }, { Faces.BOTTOM, bot }
            };
        }

        public static string GetPath(BlockType type, Faces face)
        {
            if (_blockPaths.ContainsKey(type) && _blockPaths[type].ContainsKey(face))
                return _blockPaths[type][face];
            return "blocks/stone/stone.png";
        }

        public static void InitLayers(TextureArray array)
        {
            foreach (var kvp in _blockPaths)
            {
                BlockType type = kvp.Key;
                BlockLayerIndices[type] = new Dictionary<Faces, int>();

                foreach (var faceKvp in kvp.Value)
                {
                    Faces face = faceKvp.Key;
                    string path = faceKvp.Value;
                    int layer = array.GetOrLoadLayer(path);
                    BlockLayerIndices[type][face] = layer;
                }
            }

            BreakLayerIndices.Clear();
            for (int i = 1; i <= 10; i++)
            {
                string path = $"ui/block_break/break_{i}.png";
                int layer = array.GetOrLoadLayer(path);
                BreakLayerIndices.Add(layer);
            }
            array.GenerateMipmaps();
        }
    }
}