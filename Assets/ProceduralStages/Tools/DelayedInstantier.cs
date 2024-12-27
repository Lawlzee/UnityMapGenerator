using UnityEngine;

namespace ProceduralStages
{
    public class DelayedInstantier : MonoBehaviour
    {
        public GameObject[] prefabs;
        public int delay = 1;

        public void Update()
        {
            if (delay > 0)
            {
                delay--;
                return;
            }

            foreach (var prefab in prefabs)
            {
                Instantiate(prefab);
            }

            Destroy(gameObject);
        }
    }
}