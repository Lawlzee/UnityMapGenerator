using UnityEngine;

namespace ProceduralStages
{
    public class DelayedInstantier : MonoBehaviour
    {
        public GameObject[] prefabs;
        private bool instantied;

        public void OnDisable()
        {
            instantied = false;
        }

        public void Update()
        {
            if (!instantied)
            {
                foreach (var prefab in prefabs)
                {
                    Instantiate(prefab);
                }

                instantied = true;
            }
        }
    }
}