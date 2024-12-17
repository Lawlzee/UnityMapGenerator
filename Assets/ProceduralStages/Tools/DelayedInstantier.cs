using UnityEngine;

namespace ProceduralStages
{
    public class DelayedInstantier : MonoBehaviour
    {
        public GameObject[] prefabs;

        public void Update()
        {
            foreach (var prefab in prefabs)
            {
                Instantiate(prefab);
            }

            Destroy(gameObject);
        }
    }
}