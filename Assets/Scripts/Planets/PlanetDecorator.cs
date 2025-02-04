using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProcGen
{
    
    public class PlanetDecorator : MonoBehaviour
    {
        [Serializable]
        public struct DecorOptions
        {
            public Sprite[] spritePool;
            public bool animate;
            public bool move;
            public string objectName;
            public BackgroundLayer layer;
            public int count;
            public float minSpawnHeight;
            public float minAngleIncrement;
            public float maxAngleIncrement;
            public float minHeightOffset;
            public float maxHeightOffset;
            public Color spriteColor;
            public string sortingLayer;
            public int sortingOrder;
        }
        
        public enum BackgroundLayer { This, Foreground, Midground, BackgroundOne, BackgroundTwo }
        
        // TODO: Make these options into one array
        [SerializeField] private DecorOptions realTreeOptions;
        [SerializeField] private DecorOptions fgTreeOptions;
        [SerializeField] private DecorOptions mgBushOptions, mgBirdOptions;
        [SerializeField] private DecorOptions bgMountainOptions, bgIslandOptions;
        [SerializeField] private DecorOptions flowerOptions, grassOptions, rockOptions, bushOptions;
        [SerializeField] private Material bgTerrainMaterialFg, bgTerrainMaterialMg;

        private Transform[] _backgroundLayerParents;
        private List<KeyValuePair<GameObject, DecorOptions>> _updatingDecorObjects = new();

        public void SpawnTrees(PlanetGenerator planetGen)
        {
            if (_backgroundLayerParents == null)
            {
                InitParentObjects();
            }
            
            SpawnDecor(planetGen, realTreeOptions);
        }

        public void CreateBackgroundDecorations(PlanetGenerator planetGen)
        {
            if (_backgroundLayerParents == null)
            {
                InitParentObjects();
            }
            
            SpawnDecor(planetGen, fgTreeOptions);
            SpawnDecor(planetGen, mgBushOptions);
            SpawnDecor(planetGen, mgBirdOptions);
            SpawnDecor(planetGen, bgMountainOptions);
            SpawnDecor(planetGen, bgIslandOptions);
            SpawnDecor(planetGen, flowerOptions);
            SpawnDecor(planetGen, grassOptions);
            SpawnDecor(planetGen, rockOptions);
            SpawnDecor(planetGen, bushOptions);
        }

        public void CreateBackgroundTerrain(MeshFilter[] meshFilters)
        {
            // TODO: Make the background terrain generation smarter
            
            /* This doesn't work rn. It tries to make a single mesh out of the whole terrain,
             * which is stupid because we only really need a bit of the surface terrain. The background
             * will probably change to some cave wall thing very soon anyway.
             *
             * I wonder if you could just take the terrain camera view and duplicate it
             * but with a flat color or something...
             */
            
            if (_backgroundLayerParents == null)
            {
                InitParentObjects();
            }

            const int meshBundleSize = 64; // how many tiles are combined and optimized
            var combines = new CombineInstance[meshFilters.Length];
            
            // Create meshes so we can combine multiple meshes into one, optimize them and go on.
            // This way we can reduce the amount of vertices as we're combining all the tiles.
            // If we try to combine all the tiles directly into 1, it'll have like over 300k vertices,
            // and Unity starts screaming.
            // var midMeshes = new Mesh[Mathf.CeilToInt(meshFilters.Length / (float)meshBundleSize)];
            //
            // for (var i = 0; i < meshFilters.Length;)
            // {
            //     for (var j = 0; j < meshBundleSize && i < meshFilters.Length; j++, i++)
            //     {
            //         combines[j].mesh = meshFilters[i].mesh;
            //         combines[j].transform = meshFilters[i].transform.localToWorldMatrix;
            //     }
            //     
            //     var index = Mathf.FloorToInt(i / (float)meshBundleSize - 1);
            //     var tMesh = new Mesh();
            //     tMesh.CombineMeshes(combines, true);
            //     
            //     // optimize
            //     var simplifier = new UnityMeshSimplifier.MeshSimplifier();
            //     simplifier.Initialize(tMesh);
            //     simplifier.SimplifyMesh(0.25f);
            //     midMeshes[index] = simplifier.ToMesh();
            // }
            //
            // combines = new CombineInstance[midMeshes.Length];
            //
            for (var i = 0; i < combines.Length; i++)
            {
                combines[i].mesh = meshFilters[i].mesh;
                combines[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }
            
            var mesh = new Mesh();
            mesh.CombineMeshes(combines, true);
            
            // mesh.Optimize();
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(mesh);
            
            meshSimplifier.SimplifyMesh(.35f);

            var bgTerrainFg = new GameObject("bgTerrain");
            var bgTerrainMg = new GameObject("bgTerrain2");
            bgTerrainFg.transform.parent = _backgroundLayerParents![1];
            bgTerrainMg.transform.parent = _backgroundLayerParents![2];
            
            var pos = bgTerrainFg.transform.position;
            pos.z = 1f;
            bgTerrainFg.transform.position = pos;
            pos.z = 1.5f;
            bgTerrainMg.transform.position = pos;

            var meshRendererFg = bgTerrainFg.AddComponent<MeshRenderer>();
            var meshRendererMg = bgTerrainMg.AddComponent<MeshRenderer>();
            meshRendererFg.material = bgTerrainMaterialFg;
            meshRendererMg.material = bgTerrainMaterialMg;
            
            var meshFilterFg = bgTerrainFg.AddComponent<MeshFilter>();
            var meshFilterMg = bgTerrainMg.AddComponent<MeshFilter>();
            meshFilterFg.sharedMesh = meshSimplifier.ToMesh();
            meshFilterMg.sharedMesh = meshFilterFg.sharedMesh;
            print($"Mesh vertices: { meshFilterFg.mesh.vertices.Length }");
        }

        public (Transform[], List<KeyValuePair<GameObject, DecorOptions>>) GetDecorData()
        {
            return (_backgroundLayerParents, _updatingDecorObjects);
        }
        
        private void SpawnDecor(PlanetGenerator planetGen, DecorOptions options)
        {
            // start at angle 0 and increment by random amounts
            var angle = 0f;
            for (var i = 0; i < options.count; i++)
            {
                // Get surface height
                var point = (Vector3)planetGen.GetRelativeSurfacePoint(angle);
                point += transform.position;
                
                var dirToPlanet = (transform.position - point).normalized;
                
                // raycast below each decor object to find the surface point
                var hit = Physics2D.Raycast(point, dirToPlanet);

                if (Vector3.Distance(planetGen.transform.position, hit.point) < options.minSpawnHeight) return;

                // spawn decor objects
                var decor = new GameObject(options.objectName);
                decor.transform.SetParent(_backgroundLayerParents[(int)options.layer]);
                
                var sr = decor.AddComponent<SpriteRenderer>();
                sr.sprite = options.spritePool[Random.Range(0, options.spritePool.Length)];
                sr.color = options.spriteColor;

                if (options.sortingLayer != "")
                {
                    sr.sortingLayerName = options.sortingLayer;
                    sr.sortingOrder = options.sortingOrder;
                }
                
                // This assumes that the sprites have their pivot set to bottom center.
                // Normally this would set the center of the sprite to the surface level, burying ths sprite.
                decor.transform.position = (Vector3)hit.point - dirToPlanet * Random.Range(options.minHeightOffset, options.maxHeightOffset);
                decor.transform.LookAt(decor.transform.position + Vector3.forward, -dirToPlanet);

                var pos = decor.transform.localPosition;
                pos.z = 0;
                decor.transform.localPosition = pos;

                if (options.animate || options.move)
                {
                    var entry = new KeyValuePair<GameObject, DecorOptions>(decor, options);
                    _updatingDecorObjects.Add(entry);
                }
                
                angle += Random.Range(options.minAngleIncrement, options.maxAngleIncrement);
            }
        }
        
        private void InitParentObjects()
        {
            var parallaxParent = new GameObject("Parallax Background").transform;
            parallaxParent.parent = transform;
            parallaxParent.localPosition = Vector3.zero;

            var arrLength = Enum.GetValues(typeof(BackgroundLayer)).Length;
            _backgroundLayerParents = new Transform[arrLength];
            
            for (var i = 0; i < arrLength; i++)
            {
                var parentTr = new GameObject(Enum.GetName(typeof(BackgroundLayer), i)).transform;
                parentTr.parent = parallaxParent;
                parentTr.localPosition = i > 1 ? Vector3.forward * 2 : Vector3.zero;
                _backgroundLayerParents[i] = parentTr;
            }
        }
    }
}
