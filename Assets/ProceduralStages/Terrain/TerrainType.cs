using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public enum TerrainType
    {
        Random,
        OpenCaves,
        Islands,
        TunnelCaves,
        Mines
    }

    public static class TerrainTypeExtensions
    {
        public static string GetName(this TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.OpenCaves:
                    return "Open Caves";
                case TerrainType.TunnelCaves:
                    return "Tunnel Caves";
                case TerrainType.Islands:
                    return "Islands";
                case TerrainType.Mines:
                    return "Twisted Canyons";
                default:
                    return "?";
            }
        }
    }
}
