using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;

namespace GameWish.Game.Editor
{
    [CustomEditor(typeof(GPUSkinningPlayerMono))]
    public class GPUSkinningPlayerMonoEditor : OdinEditor
    {
        private string[] clipsName = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var player = target as GPUSkinningPlayerMono;
            if (player == null)
            {
                return;
            }

            GPUSkinningAnimation anim = serializedObject.FindProperty("anim").objectReferenceValue as GPUSkinningAnimation;
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
                        player.Player.Play(clipsName[defaultPlayingClipIndex.intValue]);
                }
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
