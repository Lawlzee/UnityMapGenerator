using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "BirdsharkBackdrop", menuName = "ProceduralStages/BirdsharkBackdrop", order = 2)]
    public class BirdsharkBackdrop : BackdropTerrainGenerator
    {
        public Interval distance;
        public Interval height;

        public GameObject spinnerPrefab;
        public Interval spinnerSpeed;

        public GameObject birdPrefab;
        public IntervalInt birdCount;

        public override GameObject Generate(BackdropParams args)
        {
            //GameObject prefabObject = spinnerPrefab ?? Addressables.LoadAssetAsync<GameObject>(assetKey).WaitForCompletion();
            //
            //GameObject gameObject = Instantiate(prefabObject);
            //gameObject.transform.position += args.center;
            //
            //return gameObject;
            return null;
        }
    }
}
