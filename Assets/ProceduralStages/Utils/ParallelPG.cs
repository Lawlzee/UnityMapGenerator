using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public static class ParallelPG
    {
        public static void For(int fromInclusive, int toExlusive, int bandsCount, Action<int, int, int> body)
        {
            int bandSize = Mathf.CeilToInt((toExlusive - fromInclusive) / (float)bandsCount);

            Parallel.For(0, bandsCount, bandIndex =>
            {
                int min = bandIndex * bandSize + fromInclusive;
                int max = Math.Min(toExlusive, min + bandSize);

                body(bandIndex, min, max);
            });
        }
    }
}
