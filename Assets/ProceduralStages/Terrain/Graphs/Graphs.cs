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

        public PropsNode? FindNodeApproximate(Xoroshiro128Plus rng, Vector3 position, float maxDistance)
        {
            List<PropsNode> validPositions = new List<PropsNode>();

            for (int i = 0; i < floorProps.Length; i++)
            {
                PropsNode node = floorProps[i];
                
                if ((node.position - position).sqrMagnitude > maxDistance * maxDistance)
                {
                    continue;
                }

                if (!groundNodeIndexByPosition.TryGetValue(node.position, out int index))
                {
                    continue;
                }

                var graphNode = ground.nodes[index];
                if (graphNode.forbiddenHulls == (HullMask.Human | HullMask.Golem | HullMask.BeetleQueen))
                {
                    continue;
                }

                validPositions.Add(node);
            }

            Log.Debug("validPositions.Length: " + validPositions.Count);

            if (validPositions.Count == 0)
            {
                return null;
            }

            return rng.NextElementUniform(validPositions);
        }
    }
}
