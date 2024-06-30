using RoR2.Navigation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class Graphs
    {
        public NodeGraph ground;
        public NodeGraph air;
        public PropsNode[] floorProps;
        public PropsNode[] ceilingProps;

        public Dictionary<Vector3, PropsNode> nodeInfoByPosition;
        public Dictionary<Vector3, int> groundNodeIndexByPosition;

        public void OccupySpace(Vector3 position, bool solid)
        {
            if (groundNodeIndexByPosition.TryGetValue(position, out int index))
            {
                ref var node = ref ground.nodes[index];

                node.flags = NodeFlags.NoCharacterSpawn | NodeFlags.NoShrineSpawn | NodeFlags.NoChestSpawn | (node.flags & NodeFlags.NoCeiling);

                if (solid)
                {
                    node.flags |= NodeFlags.NoCharacterSpawn;
                    node.forbiddenHulls = HullMask.Human | HullMask.Golem | HullMask.BeetleQueen;
                }
            }
        }
    }
}
