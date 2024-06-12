using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Game.GPUSkinning.Editor
{
    public partial class GPUSkinningBakeWindow : OdinEditorWindow
    {
        [MenuItem("Tools/GPUSkinningBakeWindow")]
        static void OpenWindow()
        {
            var window = GetWindow<GPUSkinningBakeWindow>();
            window.titleContent = new GUIContent("GPUSkinningBakeWindow");
            window.Show();
        }

        public GameObject target;

        [Space(40)]
        [BoxGroup]
        public Transform rootBoneTransform = null;
        [BoxGroup, InlineEditor]
        public GPUSkinningAnimation animation = null;


        private GPUSkinningBaker m_Baker = new();


        [Button]
        void Init()
        {
            if (target == null) return;

            var skinRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinRenderer == null) return;

            string assetPath = null;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(target))
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            else
                assetPath = AssetDatabase.GetAssetPath(target);
            //在当前target位置生成GPUAnimation文件夹
            if (string.IsNullOrEmpty(assetPath))
                return;


            var savePath = CreateDirectory(assetPath);
            LoadAnimationData(savePath);

            m_Baker.animation = animation;
            m_Baker.skinnedMeshRenderer = skinRenderer;
            m_Baker.rootBoneTransform = rootBoneTransform;
            m_Baker.target = target;
            m_Baker.savePath = savePath;
            m_Baker.Init();
        }

        [Button]
        void Bake()
        {
            m_Baker.Bake();
        }

        string CreateDirectory(string assetPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(assetPath);
            DirectoryInfo directoryInfoAnimation = new DirectoryInfo($"{directoryInfo.Parent}/GPUAnimation");
            if (!directoryInfoAnimation.Exists)
            {
                directoryInfoAnimation.Create();
                AssetDatabase.Refresh();
            }

            return "Assets" + directoryInfoAnimation.FullName.Substring(Application.dataPath.Length) + "/";
        }

        void LoadAnimationData(string savePath)
        {
            string assetPath = $"{savePath}GPUSkinning_Animation_{target.name}.asset";
            var anim = AssetDatabase.LoadAssetAtPath<GPUSkinningAnimation>(assetPath);
            animation = anim == null ? ScriptableObject.CreateInstance<GPUSkinningAnimation>() : anim;
            if (anim == null)
            {
                animation.guid = System.Guid.NewGuid().ToString();
                AssetDatabase.CreateAsset(animation, assetPath);
            }
        }
    }
}
