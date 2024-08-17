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
        Moon,
        PotRolling
    }

    public static class TerrainTypeExtensions
    {
        public static bool IsNormalStage(this TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.OpenCaves:
                case TerrainType.TunnelCaves:
                case TerrainType.Islands:
                case TerrainType.Mines:
                case TerrainType.Basalt:
                case TerrainType.Towers:
                case TerrainType.Temple:
                    return true;
            }

            return false;
        }

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
                    return "Lunar Fields";
                case TerrainType.Basalt:
                    return "Basalt Isle";
                case TerrainType.Towers:
                    return "Block maze";
                case TerrainType.Temple:
                    return "Temple";
                case TerrainType.PotRolling:
                    return "The line";
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
                    return "Celestial Odyssey";
                case TerrainType.Basalt:
                    return "Volcanic Pillars";
                case TerrainType.Towers:
                    return "Enigmatic Towers";
                case TerrainType.Temple:
                    return "Ancient Sanctuary";
                case TerrainType.PotRolling:
                    return "Sisyphus' Trial";
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
                    return "You dream of exploring moonlit landscapes, where gravity bends and stars seem within reach.";
                case TerrainType.Basalt:
                    return "You dream of towering basalt columns and the roar of distant volcanoes.";
                case TerrainType.Towers:
                    return "You dream of diamonds.";
                case TerrainType.Temple:
                    return "You dream of mystical energies flowing through ancient ruins.";
                case TerrainType.PotRolling:
                    return "You dream of an upside down world.";
                default:
                    return "?";
            }
        }
    }
}
