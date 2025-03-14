using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameWish.Game
{
    [CreateAssetMenu(fileName = "GPUSkinningAnimationGroup", menuName = "Tools/GPU Skinning/GPUSkinningAnimationGroup")]
    public class GPUSkinningAnimationGroup : ScriptableObject
    {
        public List<GPUSkinningAnimationSetting> animations = new();


        public GPUSkinningAnimationSetting GetGPUSkinningAnimationSetting(int animID)
        {
            foreach (var anim in animations)
            {
                if (anim.id == animID)
                    return anim;
            }

            Debug.LogError($"animID:{animID} 不存在 GPUSkinningAnimationGroup 中");
            return null;
        }

        [Button("收集项目中的全部动画")]
        void CollectAnimationFromProject()
        {
            if (EditorUtility.DisplayDialog("提示", "收集项目中的全部动画,这会清空你现有的动画设置", "确定"))
            {
                animations.Clear();

                int id = 0;
                var guids = AssetDatabase.FindAssets("t:GPUSkinningAnimation");
                foreach (var guid in guids)
                {
                    var animation = AssetDatabase.LoadAssetAtPath<GPUSkinningAnimation>(AssetDatabase.GUIDToAssetPath(guid));

                    GPUSkinningAnimationSetting setting = new();
                    setting.id = id;
                    setting.animation = animation;
                    animations.Add(setting);
                    id++;
                }
            }
        }
    }

    [System.Serializable]
    public class GPUSkinningAnimationSetting
    {
        public int id;
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
        public GPUSkinningAnimation animation;
        public int maxCount = 100;
    }
}