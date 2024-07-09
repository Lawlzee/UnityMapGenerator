using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class BackdropParams
    {
        public Vector3 center;
        public ulong seed;
        public Material material;
        public Texture2D colorGradiant;
        public PropsDefinitionCollection propsCollection;    
    }

    public abstract class BackdropTerrainGenerator : ScriptableObject
    {
        public abstract GameObject Generate(BackdropParams args);
    }
}
