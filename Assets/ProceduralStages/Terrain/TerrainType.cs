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
        Mines,
        Basalt,
        Towers,
        Temple,
        Moon
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
                case TerrainType.Moon:
                    return "Moon";
                case TerrainType.Basalt:
                    return "Basalt Isle";
                case TerrainType.Towers:
                    return "Block maze";
                case TerrainType.Temple:
                    return "Temple";
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
                case TerrainType.Moon:
                    return "TODO";
                case TerrainType.Basalt:
                    return "Volcanic Pillars";
                case TerrainType.Towers:
                    return "Enigmatic Towers";
                case TerrainType.Temple:
                    return "Ancient Sanctuary";
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
                case TerrainType.Moon:
                    return "TODO";
                case TerrainType.Basalt:
                    return "You dream of towering basalt columns and the roar of distant volcanoes.";
                case TerrainType.Towers:
                    return "You dream of diamonds.";
                case TerrainType.Temple:
                    return "You dream of mystical energies flowing through ancient ruins.";
                default:
                    return "?";
            }
        }
    }
}
