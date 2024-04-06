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
        TunnelCaves
    }

    public static class TerrainTypeExtensions
    {
        public static string GetName(this TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.OpenCaves:
                    return "Open caves";
                case TerrainType.TunnelCaves:
                    return "Tunnel caves";
                case TerrainType.Islands:
                    return "Islands";
                default:
                    return "?";
            }
        }
    }
}
