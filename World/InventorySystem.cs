using System;
using System.Collections.Generic;

namespace EarthBound.World
{
    [Serializable]
    public class ItemStack
    {
        public BlockType Type;
        public int Count;
        public const int MAX_STACK = 128;

        public ItemStack(BlockType type, int count)
        {
            Type = type;
            Count = count;
        }

        public ItemStack() { }
    }

    public class InventorySystem
    {
        public ItemStack[] Slots;
        public const int HOTBAR_SIZE = 9;
        public const int TOTAL_SIZE = 9 + (8 * 6);

        // --- НОВОЕ: Предмет, который мы "держим" мышкой ---
        public ItemStack DragStack;

        public InventorySystem()
        {
            Slots = new ItemStack[TOTAL_SIZE];
        }

        public bool AddItem(BlockType type, int count = 1)
        {
            for (int i = 0; i < TOTAL_SIZE; i++)
            {
                if (Slots[i] != null && Slots[i].Type == type && Slots[i].Count < ItemStack.MAX_STACK)
                {
                    int space = ItemStack.MAX_STACK - Slots[i].Count;
                    int toAdd = Math.Min(space, count);
                    Slots[i].Count += toAdd;
                    count -= toAdd;
                    if (count <= 0) return true;
                }
            }

            for (int i = 0; i < TOTAL_SIZE; i++)
            {
                if (Slots[i] == null)
                {
                    Slots[i] = new ItemStack(type, count);
                    return true;
                }
            }

            return false;
        }

        public bool ConsumeItem(int slotIndex, int count = 1)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SIZE || Slots[slotIndex] == null) return false;

            if (Slots[slotIndex].Count >= count)
            {
                Slots[slotIndex].Count -= count;
                if (Slots[slotIndex].Count <= 0) Slots[slotIndex] = null;
                return true;
            }
            return false;
        }

        public ItemStack GetStack(int index)
        {
            if (index < 0 || index >= TOTAL_SIZE) return null;
            return Slots[index];
        }

        public void SetStack(int index, ItemStack stack)
        {
            if (index >= 0 && index < TOTAL_SIZE) Slots[index] = stack;
        }
    }
}