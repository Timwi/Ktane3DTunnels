using System;

namespace Assets
{
    enum Direction
    {
        ForwardXUpY = 0x211121,
        ForwardXUpNY = 0x211101,
        ForwardXUpZ = 0x211112,
        ForwardXUpNZ = 0x211110,
        ForwardNXUpY = 0x011121,
        ForwardNXUpNY = 0x011101,
        ForwardNXUpZ = 0x011112,
        ForwardNXUpNZ = 0x011110,

        ForwardYUpX = 0x121211,
        ForwardYUpNX = 0x121011,
        ForwardYUpZ = 0x121112,
        ForwardYUpNZ = 0x121110,
        ForwardNYUpX = 0x101211,
        ForwardNYUpNX = 0x101011,
        ForwardNYUpZ = 0x101112,
        ForwardNYUpNZ = 0x101110,

        ForwardZUpX = 0x112211,
        ForwardZUpNX = 0x112011,
        ForwardZUpY = 0x112121,
        ForwardZUpNY = 0x112101,
        ForwardNZUpX = 0x110211,
        ForwardNZUpNX = 0x110011,
        ForwardNZUpY = 0x110121,
        ForwardNZUpNY = 0x110101
    }

    static class DirectionUtils
    {
        public static bool IsWallForward(this Direction dir, int position)
        {
            int x, y, z;
            GetXYZ(position, out x, out y, out z);
            x += GetForwardX(dir);
            y += GetForwardY(dir);
            z += GetForwardZ(dir);
            return (x < 0 || x > 2 || y < 0 || y > 2 || z < 0 || z > 2);
        }

        private static int GetForwardX(this Direction dir) { return ((int)dir >> 20) - 1; }
        private static int GetForwardY(this Direction dir) { return (((int)dir >> 16) & 0xf) - 1; }
        private static int GetForwardZ(this Direction dir) { return (((int)dir >> 12) & 0xf) - 1; }
        private static int GetUpX(this Direction dir) { return (((int)dir >> 8) & 0xf) - 1; }
        private static int GetUpY(this Direction dir) { return (((int)dir >> 4) & 0xf) - 1; }
        private static int GetUpZ(this Direction dir) { return ((int)dir & 0xf) - 1; }
        private static int GetLeftX(this Direction dir) { return GetForwardY(dir) * GetUpZ(dir) - GetForwardZ(dir) * GetUpY(dir); }
        private static int GetLeftY(this Direction dir) { return GetForwardZ(dir) * GetUpX(dir) - GetForwardX(dir) * GetUpZ(dir); }
        private static int GetLeftZ(this Direction dir) { return GetForwardX(dir) * GetUpY(dir) - GetForwardY(dir) * GetUpX(dir); }

        public static Direction TurnLeftRight(this Direction dir, bool right)
        {
            var r = Rotate(GetUpX(dir), GetUpY(dir), GetUpZ(dir), GetForwardX(dir), GetForwardY(dir), GetForwardZ(dir), right ? 1 : -1);
            return (Direction)(((int)dir & 0xfff) | ((r[0] + 1) << 20) | ((r[1] + 1) << 16) | ((r[2] + 1) << 12));
        }
        public static Direction TurnUpDown(this Direction dir, bool up)
        {
            var r1 = Rotate(GetLeftX(dir), GetLeftY(dir), GetLeftZ(dir), GetForwardX(dir), GetForwardY(dir), GetForwardZ(dir), up ? 1 : -1);
            var r2 = Rotate(GetLeftX(dir), GetLeftY(dir), GetLeftZ(dir), GetUpX(dir), GetUpY(dir), GetUpZ(dir), up ? 1 : -1);
            return (Direction)(((r1[0] + 1) << 20) | ((r1[1] + 1) << 16) | ((r1[2] + 1) << 12) | ((r2[0] + 1) << 8) | ((r2[1] + 1) << 4) | (r2[2] + 1));
        }

        private static int[] Rotate(int axisX, int axisY, int axisZ, int x, int y, int z, int sin)
        {
            return new int[] {
                x * (axisX * axisX) +
                y * (axisX * axisY - axisZ * sin) +
                z * (axisX * axisZ + axisY * sin),

                x * (axisY * axisX + axisZ * sin) +
                y * (axisY * axisY) +
                z * (axisY * axisZ - axisX * sin),

                x * (axisZ * axisX - axisY * sin) +
                y * (axisZ * axisY + axisX * sin) +
                z * (axisZ * axisZ)
            };
        }

        public static int MoveForward(this Direction dir, int position)
        {
            int x, y, z;
            GetXYZ(position, out x, out y, out z);
            return MkPos(x + GetForwardX(dir), y + GetForwardY(dir), z + GetForwardZ(dir));
        }

        private static int MkPos(int x, int y, int z)
        {
            return x + 3 * (y + 3 * z);
        }

        private static void GetXYZ(int position, out int x, out int y, out int z)
        {
            x = position % 3;
            y = (position / 3) % 3;
            z = position / 9;
        }
    }
}
