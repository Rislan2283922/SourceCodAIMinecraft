using System;
using System.Linq;
using System.Reflection;

namespace EarthBound.World.Blocks
{
    public static class BlocksManager
    {
        public static Block[] Blocks = new Block[256];
        public static bool Initialized = false;

        public static void Initialize()
        {
            if (Initialized) return;

            // 1. Заполняем массив дефолтным воздухом во избежание null reference
            for (int i = 0; i < Blocks.Length; i++) Blocks[i] = new AirBlock();

            // 2. Ищем все классы, наследуемые от Block, в текущей сборке
            var blockTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Block)) && !t.IsAbstract);

            foreach (Type type in blockTypes)
            {
                // Ищем константу public const int Index
                FieldInfo indexField = type.GetField("Index", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (indexField != null)
                {
                    int id = (int)indexField.GetValue(null);

                    if (id >= 0 && id < Blocks.Length)
                    {
                        // Создаем экземпляр
                        Block block = (Block)Activator.CreateInstance(type);
                        Blocks[id] = block;
                        block.Initialize();
                    }
                }
            }

            Initialized = true;
        }

        public static Block GetBlock(BlockType type) => Blocks[(int)type];
        public static Block GetBlock(int id)
        {
            if (id < 0 || id >= Blocks.Length) return Blocks[0];
            return Blocks[id];
        }
    }
}