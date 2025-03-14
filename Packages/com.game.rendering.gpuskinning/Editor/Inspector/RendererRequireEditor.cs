using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;

namespace GameWish.Game.Editor
{
    [CustomEditor(typeof(RendererRequire), true)]
    public class RendererRequireEditor : OdinEditor
    {
        private string[] clipsName = null;
        private string[] settingsName = null;
        private static GPUSkinningAnimationGroup m_Group;


        private SerializedProperty m_AnimSettingID;
        private SerializedProperty m_DefaultPlayingClipIndex;

        protected override void OnEnable()
        {
            base.OnEnable();
            // m_Color = serializedObject.FindProperty("color");
            m_AnimSettingID = serializedObject.FindProperty("m_AnimSettingID");
            m_DefaultPlayingClipIndex = serializedObject.FindProperty("defaultPlayingClipIndex");
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var player = target as RendererRequire;

            // EditorGUILayout.PropertyField(m_Color);


            if (m_Group == null)
            {
                m_Group = AssetDatabase.FindAssets("t:GPUSkinningAnimationGroup").Select(
                        x => AssetDatabase.LoadAssetAtPath<GPUSkinningAnimationGroup>(AssetDatabase.GUIDToAssetPath(x)))
                    .FirstOrDefault();
            }

            if (m_Group == null)
            {
                EditorGUILayout.LabelField("GPUSkinningAnimationGroup is null");
                return;
            }

            if (!Application.isPlaying)
            {
                LoadSettingsName();
                if (settingsName != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    m_AnimSettingID.intValue = EditorGUILayout.Popup("Animation Setting", m_AnimSettingID.intValue, settingsName);
                    if (GUILayout.Button("Ping", GUILayout.Width(40f)))
                    {
                        EditorGUIUtility.PingObject(m_Group.animations[m_AnimSettingID.intValue].animation);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }


            var setting = m_Group.GetGPUSkinningAnimationSetting(m_AnimSettingID.intValue);
            if (setting == null) return;


            //defaultPlayingClipIndex
            GPUSkinningAnimation anim = setting.animation;
            LoadClipsName(anim);

            if (clipsName != null)
            {
                EditorGUI.BeginChangeCheck();
                m_DefaultPlayingClipIndex.intValue = EditorGUILayout.Popup("Default Playing", m_DefaultPlayingClipIndex.intValue, clipsName);
                if (EditorGUI.EndChangeCheck())
                {
                    if (player.Player != null)
                        player.Player.CrossFade(clipsName[m_DefaultPlayingClipIndex.intValue], 0.2f);
                }
            }


            serializedObject.ApplyModifiedProperties();
        }

        void LoadSettingsName()
        {
            if (settingsName == null)
            {
                List<string> list = new List<string>();
                for (int i = 0; i < m_Group.animations.Count; ++i)
                {
                    list.Add($"{m_Group.animations[i].id}:{m_Group.animations[i].animation.name}");
                }

                settingsName = list.ToArray();
                m_AnimSettingID.intValue = Mathf.Clamp(m_AnimSettingID.intValue, 0, m_Group.animations.Count);
            }
        }

        void LoadClipsName(GPUSkinningAnimation anim)
        {
            if (clipsName == null && anim != null)
            {
                List<string> list = new List<string>();
                for (int i = 0; i < anim.clips.Length; ++i)
                {
                    list.Add(anim.clips[i].name);
                }

                clipsName = list.ToArray();

                m_DefaultPlayingClipIndex.intValue = Mathf.Clamp(m_DefaultPlayingClipIndex.intValue, 0, anim.clips.Length);
            }
        }
    }
}