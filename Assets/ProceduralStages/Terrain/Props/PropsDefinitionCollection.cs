using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "propsCollection", menuName = "ProceduralStages/PropsCollection", order = 0)]
    public class PropsDefinitionCollection : ScriptableObject
    {
        public PropsDefinitionCategory[] categories;
    }

    [Serializable]
    public struct PropsDefinitionCategory
    {
        public string name;
        public PropsDefinition[] props;
        //todo: weight?
    }
}
