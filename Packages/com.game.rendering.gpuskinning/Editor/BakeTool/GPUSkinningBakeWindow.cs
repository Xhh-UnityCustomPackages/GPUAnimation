using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;

namespace GameWish.Game.Editor
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

        [OnValueChanged("OnTargetChanged")]
        public GameObject target;


        [BoxGroup]
        public Transform rootBoneTransform = null;
        [BoxGroup, InlineEditor]
        public GPUSkinningAnimation animation = null;


        private GPUSkinningBaker m_Baker = new();
        [ShowInInspector]
        private string m_SavePath;


        private static readonly Dictionary<string, System.Type> _cacheMaterialTypes = new();
        private static readonly Dictionary<string, IMaterialProvider> _cacheMaterialInstance = new();
        public static readonly List<System.Type> _cacheMaterialTypesList = new();

        [OnInspectorInit]
        void Init()
        {
            //得到全部的材质类型
            InitMaterialProvider();
        }

        #region Material
        void InitMaterialProvider()
        {
            _cacheMaterialTypes.Clear();
            _cacheMaterialInstance.Clear();

            var customTypes = GetAssignableTypes(typeof(IMaterialProvider));
            for (int i = 0; i < customTypes.Count; i++)
            {
                System.Type type = customTypes[i];
                if (_cacheMaterialTypes.ContainsKey(type.Name) == false)
                {
                    _cacheMaterialTypesList.Add(type);
                    _cacheMaterialTypes.Add(type.Name, type);
                }
            }

            var count = _cacheMaterialTypesList.Count;
            for (int i = 0; i < count; i++)
            {
                var type = _cacheMaterialTypesList[i];

                var filterRule = GetFilterRuleInstance(type.Name);
            }
        }

        public static List<System.Type> GetAssignableTypes(System.Type parentType)
        {
            TypeCache.TypeCollection collection = TypeCache.GetTypesDerivedFrom(parentType);
            return collection.ToList();
        }

        public static IMaterialProvider GetFilterRuleInstance(string ruleName)
        {
            if (_cacheMaterialInstance.TryGetValue(ruleName, out IMaterialProvider instance))
                return instance;

            // 如果不存在创建类的实例
            if (_cacheMaterialTypes.TryGetValue(ruleName, out System.Type type))
            {
                instance = (IMaterialProvider)System.Activator.CreateInstance(type);
                _cacheMaterialInstance.Add(ruleName, instance);
                return instance;
            }
            else
            {
                throw new System.Exception($"{nameof(IMaterialProvider)}类型无效：{ruleName}");
            }
        }
        #endregion

        void OnTargetChanged()
        {
            if (target == null)
            {
                rootBoneTransform = null;
                return;
            }

            if (rootBoneTransform != null && !rootBoneTransform.IsChildOf(target.transform))
            {
                rootBoneTransform = target.transform.GetChild(0);
            }
            else if (rootBoneTransform == null)
            {
                rootBoneTransform = target.transform.GetChild(0);
            }
        }


        [Button]
        void Bake()
        {
            if (target == null) return;

            var skinRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinRenderer == null) return;
            var animator = target.GetComponent<Animator>();
            if (animator == null) return;

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
            m_SavePath = savePath;

            CreateMaterial();

            m_Baker.animation = animation;
            m_Baker.skinnedMeshRenderer = skinRenderer;
            m_Baker.rootBoneTransform = rootBoneTransform;
            m_Baker.target = target;
            m_Baker.savePath = savePath;
            m_Baker.Init();
            m_Baker.Bake();
        }

        [ValueDropdown("GetMaterialProvider")]
        public IMaterialProvider materialProvider;

        public IEnumerable<IMaterialProvider> GetMaterialProvider()
        {
            return _cacheMaterialInstance.Values;
        }


        void CreateMaterial()
        {
            if (materialProvider == null) return;
            if (animation.material != null) return;

            var material = materialProvider.GetMaterial();
            var savePath = $"{m_SavePath}GPUSkinning_farmer.mat";
            Debug.LogError($"{savePath}");
            AssetDatabase.CreateAsset(material, savePath);
            AssetDatabase.Refresh();

            animation.material = AssetDatabase.LoadAssetAtPath<Material>(savePath);
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
