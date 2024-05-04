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
                    return "Echoing Cavern";
                case TerrainType.TunnelCaves:
                    return "Tunnel Cave";
                case TerrainType.Islands:
                    return "Lonely Island";
                case TerrainType.Mines:
                    return "Twisted Canyon";
                default:
                    return "?";
            }
        }

        public static string GetSubTitle(this TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.OpenCaves:
                    return "Echoes of the Depths";
                case TerrainType.TunnelCaves:
                    return "Twisting Underworld";
                case TerrainType.Islands:
                    return "Lost Horizons";
                case TerrainType.Mines:
                    return "Spiral Abyss";
                default:
                    return "?";
            }
        }

        public static string GetDreamMessage(this TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.OpenCaves:
                    return "You dream of echoing chambers.";
                case TerrainType.TunnelCaves:
                    return "You dream of winding tunnels.";
                case TerrainType.Islands:
                    return "You dream of isolated paradises.";
                case TerrainType.Mines:
                    return "You dream of towering canyons veiled in swirling mist.";
                default:
                    return "?";
            }
        }
    }
}
