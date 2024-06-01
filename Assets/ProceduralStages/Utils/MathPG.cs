using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public static class MathPG
    {
        public static float CropFloat(float value)
        {
            uint bytes = FloatToInt.ToUint(value);
            uint choped = bytes & 0xFFFFFFF8;
            return FloatToInt.ToFloat(choped);
        }

        [StructLayout(LayoutKind.Explicit)]
        struct FloatToInt
        {
            [FieldOffset(0)] private float f;
            [FieldOffset(0)] private uint i;

            public static uint ToUint(float value)
            {
                return new FloatToInt { f = value }.i;
            }

            public static float ToFloat(uint value)
            {
                return new FloatToInt { i = value }.f;
            }
        }
    }
}
