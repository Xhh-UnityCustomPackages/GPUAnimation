using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor.Animations;

namespace Game.Rendering.GPUSkinning.Editor
{
    public class AnimatorControllerGenerator : OdinEditorWindow
    {

        [MenuItem("Tools/GPUSkinning/生成AnimatorController (AnimatorControllerGenerator)")]
        static void OpenWindow()
        {
            var window = GetWindow<AnimatorControllerGenerator>();
            window.titleContent = new GUIContent("AnimatorControllerGenerator");
            window.Show();
        }


        public bool overrideAC = false;

        
        [HideIf("overrideAC")]
        [OnValueChanged("OnTempleteChanged")]
        public AnimatorControllerTempleteSO templeteSO;

        [ShowIf("overrideAC")]
        public AnimatorController controller;
        
        public AnimationControllerInfo controllerInfo;
        

        void OnTempleteChanged()
        {
            if (templeteSO == null)
            {
                controllerInfo.m_Animations.Clear();
                return;
            }

            controllerInfo.name = "AnimationController";
            controllerInfo.savePath = "Assets";
            controllerInfo.m_Animations.Clear();
            foreach (var item in templeteSO.animatorStateInfos)
            {
                // Debug.LogError($"item.stateName:{item.stateName}");
                controllerInfo.m_Animations.Add(new AnimationInfo() { stateName = item.stateName });
            }
        }

        [Button,HorizontalGroup]
        void ResetFormTemplete()
        {
            OnTempleteChanged();
        }

        [Button, HorizontalGroup]
        void ReadFromAC()
        {
            if(controller == null) return;

            controllerInfo.name = controller.name;
            controllerInfo.savePath = AssetDatabase.GetAssetPath(controller).Replace($"/{controllerInfo.fileName}","");
            controllerInfo.m_Animations.Clear();
            
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            foreach (var state in rootStateMachine.states)
            {
                controllerInfo.m_Animations.Add(new AnimationInfo() { stateName = state.state.name,clip = state.state.motion as AnimationClip});
            }
        }

        [Button]
        void Generate()
        {
            // Creates the controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerInfo.filePath);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            // Add StateMachines
            foreach (var item in controllerInfo.m_Animations)
            {
                AnimatorState state = rootStateMachine.AddState(item.stateName);
                if (rootStateMachine.defaultState == null)
                    rootStateMachine.defaultState = state;
                state.motion = (Motion)item.clip;
            }


            controllerInfo.m_Animations.Clear();
        }

        [System.Serializable]
        [HideLabel]
        public class AnimationControllerInfo
        {
            public string name = "AnimationController";
            [FolderPath]
            public string savePath = "Assets";

            public string filePath => $"{savePath}/{fileName}";
            public string fileName=> $"{name}.controller";
            
            
            [TableList]
            // [ListDrawerSettings(Expanded = false, HideAddButton = true, HideRemoveButton = true)]
            public List<AnimationInfo> m_Animations = new();
        }

        [System.Serializable]
        public struct AnimationInfo
        {
            [ReadOnly]
            public string stateName;
            public AnimationClip clip;
        }
    }
}
