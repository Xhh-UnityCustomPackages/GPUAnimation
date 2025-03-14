using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;

namespace GameWish.Game.Editor
{
    [CustomEditor(typeof(RendererRequire))]
    public class RendererRequireEditor : OdinEditor
    {
        private string[] clipsName = null;

        private static GPUSkinningAnimationGroup m_Group;


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var player = target as RendererRequire;
            if (player == null)
            {
                return;
            }

            var animSettingID = serializedObject.FindProperty("m_AnimSettingID").intValue;

            if (m_Group == null)
            {
                m_Group = AssetDatabase.FindAssets("t:GPUSkinningAnimationGroup").Select(
                        x => AssetDatabase.LoadAssetAtPath<GPUSkinningAnimationGroup>(AssetDatabase.GUIDToAssetPath(x)))
                    .FirstOrDefault();
            }

            if (m_Group == null) return;

            var setting = m_Group.GetGPUSkinningAnimationSetting(animSettingID);
            if (setting == null)
                return;


            GPUSkinningAnimation anim = setting.animation;
            SerializedProperty defaultPlayingClipIndex = serializedObject.FindProperty("defaultPlayingClipIndex");
            if (clipsName == null && anim != null)
            {
                List<string> list = new List<string>();
                for (int i = 0; i < anim.clips.Length; ++i)
                {
                    list.Add(anim.clips[i].name);
                }

                clipsName = list.ToArray();

                defaultPlayingClipIndex.intValue = Mathf.Clamp(defaultPlayingClipIndex.intValue, 0, anim.clips.Length);
            }


            if (clipsName != null)
            {
                EditorGUI.BeginChangeCheck();
                defaultPlayingClipIndex.intValue = EditorGUILayout.Popup("Default Playing", defaultPlayingClipIndex.intValue, clipsName);
                if (EditorGUI.EndChangeCheck())
                {
                    if (player.Player != null)
                        player.Player.CrossFade(clipsName[defaultPlayingClipIndex.intValue], 0.2f, () => { Debug.LogError("End"); });
                }
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}