using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace ProceduralStages
{
    public class MeshBackdropTerrain
    {
        public MeshResult meshResult;
        public Vector3 position;
        public float propsWeigth;
    }

    public abstract class MeshBackdropTerrainGenerator : BackdropTerrainGenerator
    {
        public GameObject backdropTerrainPrefab;
        public int maxPropKind;

        public Interval distance;

        public Vector3 minSize;
        public Vector3 maxSize;

        public float scalePerDistance;

        public override GameObject Generate(BackdropParams args)
        {
            var terrain = GenerateTerrain(args.center, args.seed);
            //backdropTerrains[i] = terrain;

            GameObject gameObject = Instantiate(backdropTerrainPrefab);
            gameObject.transform.position = terrain.position;

            gameObject.GetComponent<MeshFilter>().mesh = terrain.meshResult.mesh;
            gameObject.GetComponent<MeshRenderer>().material = args.material;

            Graphs graphs = MapGenerator.instance.nodeGraphCreator.CreateBackdropGraphs(terrain);

            MapGenerator.instance.propsPlacer.PlaceAll(
                MapGenerator.rng,
                terrain.position,
                graphs,
                args.propsCollection,
                MapGenerator.instance.meshColorer,
                args.colorGradiant,
                args.material,
                0,
                terrain.propsWeigth,
                new Bounds(),
                maxPropKind);

            return gameObject;
        }

        protected abstract MeshBackdropTerrain GenerateTerrain(
            Vector3 center,
            ulong seed);
    }
}
