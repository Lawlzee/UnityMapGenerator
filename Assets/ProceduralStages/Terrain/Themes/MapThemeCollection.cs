using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "MapThemeCollection", menuName = "ProceduralStages/MapThemeCollection", order = 1)]
    public class MapThemeCollection : ScriptableObject
    {
        public MapTheme[] themes;
    }
}