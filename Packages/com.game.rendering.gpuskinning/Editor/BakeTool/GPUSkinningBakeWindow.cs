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
        [BoxGroup, InlineEditor, OnValueChanged("RefeshSavePath")]
        public GPUSkinningAnimation animation = null;


        private GPUSkinningBaker m_Baker = new GPUSkinningBaker();
        [ShowInInspector, ReadOnly]
        private string m_SavePath;


        private static readonly Dictionary<string, System.Type> _cacheMaterialTypes = new Dictionary<string, System.Type>();
        private static readonly Dictionary<string, IMaterialProvider> _cacheMaterialInstance = new Dictionary<string, IMaterialProvider>();
        public static readonly List<System.Type> _cacheMaterialTypesList = new List<System.Type>();

        [OnInspectorInit]
        void Init()
        {
            //得到全部的材质类型
            InitMaterialProvider();
            materialProviderName = null;
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

                var filterRule = GetMaterialProviderInstance(type.Name);
            }
        }

        public static List<System.Type> GetAssignableTypes(System.Type parentType)
        {
            TypeCache.TypeCollection collection = TypeCache.GetTypesDerivedFrom(parentType);
            return collection.ToList();
        }

        public static IMaterialProvider GetMaterialProviderInstance(string ruleName)
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

            animation = null;

            //尝试找到对应的GPU SO
            var guids = AssetDatabase.FindAssets("t:GPUSkinningAnimation");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Contains(GetAnimationName(target.name)))
                {
                    animation = AssetDatabase.LoadAssetAtPath<GPUSkinningAnimation>(assetPath);
                    RefeshSavePath();
                    break;
                }
            }
        }

        void RefeshSavePath()
        {
            if (animation == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(animation);
            assetPath = Path.GetDirectoryName(assetPath);
            assetPath += Path.DirectorySeparatorChar;
            m_SavePath = assetPath;
        }


        [Button]
        void Bake()
        {
            if (target == null) return;

            var animator = target.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                EditorUtility.DisplayDialog("提示", "请先添加Animator组件并设置Controller", "确定");
                return;
            }

            var skinRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinRenderer == null) return;

            LoadOrCreateAnimation();
            CreateMaterial();

            m_Baker.animation = animation;
            m_Baker.skinnedMeshRenderer = skinRenderer;
            m_Baker.rootBoneTransform = rootBoneTransform;
            m_Baker.target = target;
            m_Baker.savePath = m_SavePath;
            m_Baker.Init();
            m_Baker.Bake();
        }

        void LoadOrCreateAnimation()
        {
            if (animation == null)
            {
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
            }
            else
            {
                RefeshSavePath();
            }
        }



        [Button]
        void CreateGPUSkinningGO()
        {
            if (target == null) return;
            if (animation == null || animation.clips == null || animation.clips.Length <= 0) return;

            GameObject gpu = new GameObject($"{target.name}_GPUSkinning");
            var player = gpu.AddComponent<GPUSkinningPlayerMono>();

            SerializedObject so = new SerializedObject(player);
            so.FindProperty("anim").objectReferenceValue = animation;
            so.ApplyModifiedProperties();

        }


        [Button]
        void BakeMesh()
        {
            if (target == null) return;
            var skinRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinRenderer == null) return;
            LoadOrCreateAnimation();

            m_Baker.skinnedMeshRenderer = skinRenderer;
            m_Baker.target = target;
            m_Baker.savePath = m_SavePath;
            m_Baker.rootBoneTransform = rootBoneTransform;
            m_Baker.Init();
        }



        [ValueDropdown("GetMaterialProvider")]
        public string materialProviderName;

        public IEnumerable<string> GetMaterialProvider()
        {
            var result = new List<string>();
            foreach (var item in _cacheMaterialInstance.Values)
            {
                result.Add(item.name);
            }
            return result;
        }


        void CreateMaterial()
        {
            if (string.IsNullOrEmpty(materialProviderName)) return;
            if (animation.material != null) return;
            var materialProvider = GetMaterialProviderInstance(materialProviderName);
            var material = materialProvider.GetMaterial();
            var savePath = $"{m_SavePath}GPUSkinning_{target.name}.mat";
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
            string assetPath = $"{savePath}{GetAnimationName(target.name)}";
            var anim = AssetDatabase.LoadAssetAtPath<GPUSkinningAnimation>(assetPath);
            animation = anim == null ? ScriptableObject.CreateInstance<GPUSkinningAnimation>() : anim;
            if (anim == null)
            {
                AssetDatabase.CreateAsset(animation, assetPath);
            }
        }

        string GetAnimationName(string name)
        {
            return $"GPUSkinning_Animation_{name}.asset";
        }
    }
}
