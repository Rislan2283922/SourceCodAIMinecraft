using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace EarthBound.World
{
    [Serializable]
    public class InventorySlot
    {
        public int Slot { get; set; }
        public string ItemID { get; set; }
        public int Count { get; set; }
    }

    [Serializable]
    public class PlayerData
    {
        public float Health { get; set; } = 20.0f;
        public int Hunger { get; set; } = 20;
        public float XP { get; set; } = 0;
        public int Level { get; set; } = 0;

        // --- NEW: Fire State ---
        public bool IsBurning { get; set; } = false;
        public float BurnTimer { get; set; } = 0.0f;
        // -----------------------

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }

        public List<InventorySlot> Inventory { get; set; } = new List<InventorySlot>();

        public void SetFromGame(Camera cam, InventorySystem inv, bool isBurning, float burnTimer)
        {
            X = cam.position.X;
            Y = cam.position.Y;
            Z = cam.position.Z;
            RotX = cam.rawRotation.X;
            RotY = cam.rawRotation.Y;

            this.IsBurning = isBurning;
            this.BurnTimer = burnTimer;

            Inventory.Clear();
            for (int i = 0; i < InventorySystem.TOTAL_SIZE; i++)
            {
                var s = inv.Slots[i];
                if (s != null)
                {
                    Inventory.Add(new InventorySlot { Slot = i, ItemID = s.Type.ToString(), Count = s.Count });
                }
            }
        }

        public void ApplyToGame(Camera cam, InventorySystem inv, out bool isBurning, out float burnTimer)
        {
            cam.position = new Vector3(X, Y, Z);
            cam.rawRotation = new Vector3(RotX, RotY, 0);
            cam.smoothRotation = cam.rawRotation;

            isBurning = this.IsBurning;
            burnTimer = this.BurnTimer;

            inv.Slots = new ItemStack[InventorySystem.TOTAL_SIZE];
            if (Inventory != null)
            {
                foreach (var slot in Inventory)
                {
                    if (slot.Slot >= 0 && slot.Slot < InventorySystem.TOTAL_SIZE)
                    {
                        if (Enum.TryParse(slot.ItemID, out BlockType type))
                        {
                            inv.Slots[slot.Slot] = new ItemStack(type, slot.Count);
                        }
                    }
                }
            }
        }
    }
}