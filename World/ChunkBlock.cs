using OpenTK.Mathematics;

namespace EarthBound.World
{
    // Represents the raw DATA of a block in the chunk.
    // Contains no logic, only state.
    public struct ChunkBlock    
    {
        public BlockType Type;
        public byte Data; // Metadata (Flow level, rotation, etc.)
        public byte Light; // Packed light (Sun | Block)

        public ChunkBlock(BlockType type, byte data = 0)
        {
            this.Type = type;
            this.Data = data;
            this.Light = 0;
        }

        public void SetSunLight(int val)
        {
            int blockL = Light & 0x0F;
            Light = (byte)((val << 4) | blockL);
        }

        public int GetSunLight()
        {
            return (Light >> 4) & 0x0F;
        }

        public void SetBlockLight(int val)
        {
            int sunL = (Light >> 4) & 0x0F;
            Light = (byte)((sunL << 4) | val);
        }

        public int GetBlockLight()
        {
            return Light & 0x0F;
        }
    }
}