using RoRGauntlet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public static class PublicGauntletCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("RiskOfResources.PublicRoRGauntlet");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ulong GetSeed()
        {
            return UInt64.Parse(LoadoutHandler.GetLoadout(RoRGauntlet.Main.CurrentLoadout.Value).stageSeed);
        }
    }
}
