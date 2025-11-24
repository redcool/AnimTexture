#if UNITY_EDITOR
namespace AnimTexture
{
    using PowerUtilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.AI;
    using Object = UnityEngine.Object;

    public class AnimTexturePlayerCreator 
    {
        [MenuItem(AnimTextureEditor.POWER_UTILS_MENU+"/CreatePlayer_FromSelected")]
        static void CreatePlayer()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            CreatePlayerWithAnimatorControl(objs);
        }
        /// <summary>
        /// Create animTex player
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="onSetupInst">{instance go, original obj}</param>
        /// <returns></returns>
        public static List<GameObject> CreatePlayer(GameObject[] objs,Action<GameObject,GameObject> onSetupInst)
        {
            var list = new List<GameObject>();
            foreach (var obj in objs)
            {
                if (!obj.GetComponentInChildren<SkinnedMeshRenderer>())
                    continue;
                // outerGO
                var playerGO = new GameObject(obj.name + "_AnimTex");
                AddAgent(playerGO);

                // outerGO child
                var instGO = Object.Instantiate(obj);
                instGO.name = obj.name;
                instGO.transform.SetParent(playerGO.transform);
                SetupAnimTexture(instGO, obj.name);

                
                SetupMeshRenderer(instGO);

                instGO.DestroyComponents<Animation>(true, true);

                list.Add(playerGO);

                onSetupInst?.Invoke(instGO, obj);
            }
            return list;
        }

        public static List<GameObject> CreatePlayerWithAnimatorControl(GameObject[] objs)
        {
            return CreatePlayer(objs, (instGO, obj) => {
                SetupAnimator(instGO, obj.GetComponent<Animator>()?.runtimeAnimatorController);
            });
        }

        public static List<GameObject> CreatePlayerWithSimpleControl(GameObject[] objs)
        {
            return CreatePlayer(objs, (instGO, obj) => {
                GetOrAdd<SimpleAnimationControl>(instGO);
            });
        }

        static void SetupAnimTexture(GameObject go,string goName)
        {
            var anim = go.GetComponentInChildren<Animation>();
            if (anim)
                anim.enabled = false;

            var manifest = AssetDatabase.LoadAssetAtPath<AnimTextureManifest>(AnimTextureEditor.GetManifestPath(goName));
            var texAnim = GetOrAdd<TextureAnimation>(go);
            texAnim.manifest = manifest;

            GetOrAdd<TextureAnimationSetup>(go);
        }

        static void SetupMeshRenderer(GameObject go)
        {
            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
            var mf = GetOrAdd<MeshFilter>(go);
            mf.sharedMesh = skin.sharedMesh;
            skin.enabled = false;

            var mr = GetOrAdd<MeshRenderer>(go);
            mr.sharedMaterials = skin.sharedMaterials;
        }

        static void SetupAnimator(GameObject go,RuntimeAnimatorController originalController)
        {
            var animator = GetOrAdd<Animator>(go);
            animator.runtimeAnimatorController = originalController;

            GetOrAdd<AnimatorControl>(go);
        }

        static void AddAgent(GameObject go)
        {
            GetOrAdd<NavMeshAgent>(go);
            GetOrAdd<AgentPlayer>(go);
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            if (!go)
                return default(T);
            var comp = go.GetComponent<T>();
            if (!comp)
                comp = go.AddComponent<T>();
            return comp;
        }
    }
}
#endif