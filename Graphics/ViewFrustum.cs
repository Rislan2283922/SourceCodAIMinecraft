using OpenTK.Mathematics;
using System;

namespace EarthBound.Graphics
{
    public class ViewFrustum
    {
        private float[] _clipMatrix = new float[16];
        private float[][] _frustum = new float[6][];

        public ViewFrustum()
        {
            for (int i = 0; i < 6; i++)
            {
                _frustum[i] = new float[4];
            }
        }

        public void Update(Matrix4 viewProjection)
        {
            // Load matrix into array
            _clipMatrix[0] = viewProjection.M11; _clipMatrix[1] = viewProjection.M12; _clipMatrix[2] = viewProjection.M13; _clipMatrix[3] = viewProjection.M14;
            _clipMatrix[4] = viewProjection.M21; _clipMatrix[5] = viewProjection.M22; _clipMatrix[6] = viewProjection.M23; _clipMatrix[7] = viewProjection.M24;
            _clipMatrix[8] = viewProjection.M31; _clipMatrix[9] = viewProjection.M32; _clipMatrix[10] = viewProjection.M33; _clipMatrix[11] = viewProjection.M34;
            _clipMatrix[12] = viewProjection.M41; _clipMatrix[13] = viewProjection.M42; _clipMatrix[14] = viewProjection.M43; _clipMatrix[15] = viewProjection.M44;

            // Extract the RIGHT plane
            _frustum[0][0] = _clipMatrix[3] - _clipMatrix[0];
            _frustum[0][1] = _clipMatrix[7] - _clipMatrix[4];
            _frustum[0][2] = _clipMatrix[11] - _clipMatrix[8];
            _frustum[0][3] = _clipMatrix[15] - _clipMatrix[12];
            NormalizePlane(0);

            // Extract the LEFT plane
            _frustum[1][0] = _clipMatrix[3] + _clipMatrix[0];
            _frustum[1][1] = _clipMatrix[7] + _clipMatrix[4];
            _frustum[1][2] = _clipMatrix[11] + _clipMatrix[8];
            _frustum[1][3] = _clipMatrix[15] + _clipMatrix[12];
            NormalizePlane(1);

            // Extract the BOTTOM plane
            _frustum[2][0] = _clipMatrix[3] + _clipMatrix[1];
            _frustum[2][1] = _clipMatrix[7] + _clipMatrix[5];
            _frustum[2][2] = _clipMatrix[11] + _clipMatrix[9];
            _frustum[2][3] = _clipMatrix[15] + _clipMatrix[13];
            NormalizePlane(2);

            // Extract the TOP plane
            _frustum[3][0] = _clipMatrix[3] - _clipMatrix[1];
            _frustum[3][1] = _clipMatrix[7] - _clipMatrix[5];
            _frustum[3][2] = _clipMatrix[11] - _clipMatrix[9];
            _frustum[3][3] = _clipMatrix[15] - _clipMatrix[13];
            NormalizePlane(3);

            // Extract the FAR plane
            _frustum[4][0] = _clipMatrix[3] - _clipMatrix[2];
            _frustum[4][1] = _clipMatrix[7] - _clipMatrix[6];
            _frustum[4][2] = _clipMatrix[11] - _clipMatrix[10];
            _frustum[4][3] = _clipMatrix[15] - _clipMatrix[14];
            NormalizePlane(4);

            // Extract the NEAR plane
            _frustum[5][0] = _clipMatrix[3] + _clipMatrix[2];
            _frustum[5][1] = _clipMatrix[7] + _clipMatrix[6];
            _frustum[5][2] = _clipMatrix[11] + _clipMatrix[10];
            _frustum[5][3] = _clipMatrix[15] + _clipMatrix[14];
            NormalizePlane(5);
        }

        private void NormalizePlane(int side)
        {
            float magnitude = MathF.Sqrt((_frustum[side][0] * _frustum[side][0]) + (_frustum[side][1] * _frustum[side][1]) + (_frustum[side][2] * _frustum[side][2]));
            _frustum[side][0] /= magnitude;
            _frustum[side][1] /= magnitude;
            _frustum[side][2] /= magnitude;
            _frustum[side][3] /= magnitude;
        }

        // Check if a box (Chunk) is within the frustum
        public bool IsBoxVisible(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            for (int i = 0; i < 6; i++)
            {
                if ((_frustum[i][0] * minX + _frustum[i][1] * minY + _frustum[i][2] * minZ + _frustum[i][3] <= 0) &&
                    (_frustum[i][0] * maxX + _frustum[i][1] * minY + _frustum[i][2] * minZ + _frustum[i][3] <= 0) &&
                    (_frustum[i][0] * minX + _frustum[i][1] * maxY + _frustum[i][2] * minZ + _frustum[i][3] <= 0) &&
                    (_frustum[i][0] * maxX + _frustum[i][1] * maxY + _frustum[i][2] * minZ + _frustum[i][3] <= 0) &&
                    (_frustum[i][0] * minX + _frustum[i][1] * minY + _frustum[i][2] * maxZ + _frustum[i][3] <= 0) &&
                    (_frustum[i][0] * maxX + _frustum[i][1] * minY + _frustum[i][2] * maxZ + _frustum[i][3] <= 0) &&
                    (_frustum[i][0] * minX + _frustum[i][1] * maxY + _frustum[i][2] * maxZ + _frustum[i][3] <= 0) &&
                    (_frustum[i][0] * maxX + _frustum[i][1] * maxY + _frustum[i][2] * maxZ + _frustum[i][3] <= 0))
                {
                    return false;
                }
            }
            return true;
        }
    }
}