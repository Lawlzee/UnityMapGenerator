using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public enum Theme
    {
        Random,
        LegacyRandom,
        Desert,
        Snow,
        Void,
        Plains,
        Mushroom
    }

    public static class ThemeExtensions
    {
        public static string GetName(this Theme theme)
        {
            switch (theme)
            {
                case Theme.LegacyRandom:
                    return "Legacy Random";
                case Theme.Desert:
                    return "Desert";
                case Theme.Snow:
                    return "Snow";
                case Theme.Void:
                    return "Void";
                case Theme.Plains:
                    return "Plains";
                case Theme.Mushroom:
                    return "Mushroom";
                default:
                    return "?";
            }
        }
    }
}
