using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public enum BenchDirection
    {
        ClockWise,
        AntiClockWise
    }

    [Serializable]
    public struct BenchesHeightCurve
    {
        public ThreadSafeCurve curve;
        public BenchDirection direction;
    }
}
