using Rewired.ComponentControls.Effects;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    public class MoonExitOrbSpawner : MonoBehaviour
    {
        public Vector3 rotatorLocalRotatiton;
        public float rotatorSlowRoationSpeed;

        public Vector3 orbLocalPosition;
        public Vector3 orbLocalRotatiton;
        public Vector3 orbLocalScale;

        public float timeMax;
        public Transform explicitDestination;

        public void Awake()
        {
            string path = "RoR2/Base/moon/MoonExitArenaOrb.prefab";
            GameObject prefab = Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();

            GameObject rotator = new GameObject("Rotator");
            rotator.transform.parent = transform;
            rotator.transform.localPosition = new Vector3(0, 0, 0);
            rotator.transform.localEulerAngles = rotatorLocalRotatiton;

            RotateAroundAxis rotateAroundAxis = rotator.AddComponent<RotateAroundAxis>();
            rotateAroundAxis.speed = RotateAroundAxis.Speed.Slow;
            rotateAroundAxis.slowRotationSpeed = rotatorSlowRoationSpeed;
            rotateAroundAxis.fastRotationSpeed = 20;
            rotateAroundAxis.rotateAroundAxis = RotateAroundAxis.RotationAxis.Z;
            rotateAroundAxis.relativeTo = Space.Self;
            rotateAroundAxis.reverse = false;

            GameObject orb = Instantiate(prefab, rotator.transform);
            orb.transform.localPosition = orbLocalPosition;
            orb.transform.localEulerAngles = orbLocalRotatiton;
            orb.transform.localScale = orbLocalScale;

            orb.GetComponent<ObjectScaleCurve>().timeMax = timeMax;
            orb.GetComponent<MapZone>().explicitDestination = explicitDestination.transform;
        }
    }
}
