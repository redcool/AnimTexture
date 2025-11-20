#if UNITY_EDITOR
namespace AnimTexture
{
    using PowerUtilities;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.AI;

    public class AnimTexturePlayerCreator 
    {
        [MenuItem(AnimTextureEditor.POWER_UTILS_MENU+"/CreatePlayer_FromSelected")]
        static void CreatePlayer()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            CreatePlayer(objs);
        }
        /// <summary>
        /// Create new go list, setup animTexture
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="isDestroySkinnedMeshRenderer"></param>
        /// <returns></returns>
        public static List<GameObject> CreatePlayer(GameObject[] objs)
        {
            var list = new List<GameObject>();
            foreach (var obj in objs)
            {
                if (! obj.GetComponentInChildren<SkinnedMeshRenderer>())
                    continue;

                var parentGo = new GameObject(obj.name);
                AddAgent(parentGo);

                var go = Object.Instantiate(obj);

                go.name = obj.name + "_Animator";
                go.transform.SetParent(parentGo.transform);
                SetupAnimTexture(go, obj.name);

                SetupAnimator(go);
                SetupMeshRenderer(go);

                go.DestroyComponents<Animation>(true, true);

                list.Add(go);
            }
            return list;
        }

        public static List<GameObject> CreatePlayerWithSimpleControl(GameObject[] objs)
        {
            var list = new List<GameObject>();
            foreach (var obj in objs)
            {
                if (!obj.GetComponentInChildren<SkinnedMeshRenderer>())
                    continue;

                var parentGo = new GameObject(obj.name);
                AddAgent(parentGo);

                var go = Object.Instantiate(obj);

                go.name = obj.name + "_Animator";
                go.transform.SetParent(parentGo.transform);
                SetupAnimTexture(go, obj.name);

                //SetupAnimator(go);
                GetOrAdd<SimpleAnimationControl>(go);
                SetupMeshRenderer(go);

                list.Add(go);
            }
            return list;
        }

        public static void SetupAnimTexture(GameObject go,string goName)
        {
            var anim = go.GetComponentInChildren<Animation>();
            if (anim)
                anim.enabled = false;

            var manifest = AssetDatabase.LoadAssetAtPath<AnimTextureManifest>(AnimTextureEditor.GetManifestPath(goName));
            var texAnim = GetOrAdd<TextureAnimation>(go);
            texAnim.manifest = manifest;

            GetOrAdd<TextureAnimationSetup>(go);
        }

        private static void SetupMeshRenderer(GameObject go)
        {
            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
            var mf = GetOrAdd<MeshFilter>(go);
            mf.sharedMesh = skin.sharedMesh;
            skin.enabled = false;

            var mr = GetOrAdd<MeshRenderer>(go);
            mr.sharedMaterials = skin.sharedMaterials;
        }

        private static void SetupAnimator(GameObject go)
        {
            var animator = GetOrAdd<Animator>(go);
            //animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/{AnimTextureEditor.ANIM_TEXTURE_PATH}/OtherRes/SimpleController.controller");

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