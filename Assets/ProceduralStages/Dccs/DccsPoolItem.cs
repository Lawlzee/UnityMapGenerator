﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    public enum DccsPoolItemType
    {
        Monsters,
        Interactables
    }

    public enum StageType
    {
        Regular,
        Moon,
        Simulacrum,
        PotRolling,
        Other
    }

    [Serializable]
    public class DccsPoolItem
    {
        public string Asset;
        public int StageIndex;
        public DccsPoolItemType Type;
        public StageType StageType;
        public bool DLC1;
        public bool DLC2;

        //todo: put in scene
        public static readonly List<DccsPoolItem> All = new List<DccsPoolItem>()
        {
            new DccsPoolItem
            {
                Asset = "RoR2/Base/arena/dpArenaInteractables.asset",
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Other,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/arena/dpArenaMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/artifactworld/dpArtifactWorldInteractables.asset",
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Other,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/artifactworld/dpArtifactWorldMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/blackbeach/dpBlackBeachInteractables.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/blackbeach/dpBlackBeachMonsters.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/dampcave/dpDampCaveInteractables.asset",
                StageIndex = 4,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/dampcave/dpDampCaveMonsters.asset",
                StageIndex = 4,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/foggyswamp/dpFoggySwampInteractables.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/foggyswamp/dpFoggySwampMonsters.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/frozenwall/dpFrozenWallInteractables.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/frozenwall/dpFrozenWallMonsters.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/goldshores/dpGoldshoresInteractables.asset",
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Other,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/goldshores/dpGoldshoresMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/golemplains/dpGolemplainsInteractables.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/golemplains/dpGolemplainsMonsters.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/goolake/dpGooLakeInteractables.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/goolake/dpGooLakeMonsters.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/moon/dpMoonInteractables.asset",
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Moon,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/moon/dpMoonMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Moon,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/rootjungle/dpRootJungleInteractables.asset",
                StageIndex = 4,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/rootjungle/dpRootJungleMonsters.asset",
                StageIndex = 4,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/shipgraveyard/dpShipgraveyardInteractables.asset",
                StageIndex = 4,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/shipgraveyard/dpShipgraveyardMonsters.asset",
                StageIndex = 4,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/skymeadow/dpSkyMeadowInteractables.asset",
                StageIndex = 5,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/skymeadow/dpSkyMeadowMonsters.asset",
                StageIndex = 5,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/wispgraveyard/dpWispGraveyardInteractables.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/Base/wispgraveyard/dpWispGraveyardMonsters.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dpInfiniteTowerInteractables.asset",
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/ancientloft/dpAncientLoftInteractables.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/ancientloft/dpAncientLoftMonsters.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/itancientloft/dpITAncientLoftMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/itdampcave/dpITDampCaveMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/itfrozenwall/dpITFrozenWallMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/itgolemplains/dpITGolemplainsMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/itgoolake/dpITGooLakeMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/itmoon/dpITMoonMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/itskymeadow/dpITSkyMeadowMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Simulacrum,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/snowyforest/dpSnowyForestInteractables.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/snowyforest/dpSnowyForestMonsters.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/sulfurpools/dpSulfurPoolsInteractables.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/sulfurpools/dpSulfurPoolsMonsters.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/voidstage/dpVoidStageInteractables.asset",
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Other,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC1/voidstage/dpVoidStageMonsters.asset",
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/artifactworld01/dpArtifactWorld01Interactables.asset",
                StageIndex = 0,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Other,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/artifactworld01/dpArtifactWorld01Monsters.asset",
                StageIndex = 0,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/artifactworld02/dpArtifactWorld02Monsters.asset",
                StageIndex = 0,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/artifactworld03/dpArtifactWorld03Interactables.asset",
                StageIndex = 0,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Other,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/artifactworld03/dpArtifactWorld03Monsters.asset",
                StageIndex = 0,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/habitat/dpHabitatInteractables.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/habitat/dpHabitatMonsters.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/habitatfall/dpHabitatfallInteractables.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/habitatfall/dpHabitatfallMonsters.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/helminthroost/dpHelminthRoostInteractables.asset",
                StageIndex = 5,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/helminthroost/dpHelminthRoostMonsters.asset",
                StageIndex = 5,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/lakes/dpLakesInteractables.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/lakes/dpLakesMonsters.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/lakesnight/dpLakesnightInteractables.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/lakesnight/dpLakesnightMonsters.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/lemuriantemple/dpLemurianTempleInteractables.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/lemuriantemple/dpLemurianTempleMonsters.asset",
                StageIndex = 2,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/meridian/dpMeridianInteractables.asset",
                StageIndex = 0,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Other,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/meridian/dpMeridianMonsters.asset",
                StageIndex = 0,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Other,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/dpSulfurPoolsMonstersDLC2.asset",
                StageIndex = 3,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = true,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/village/dpVillageInteractables.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Interactables,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/village/dpVillageMonsters.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            },
            new DccsPoolItem
            {
                Asset = "RoR2/DLC2/villagenight/dpVillageNightMonsters.asset",
                StageIndex = 1,
                Type = DccsPoolItemType.Monsters,
                StageType = StageType.Regular,
                DLC1 = false,
                DLC2 = true
            }
        };
    }
}
