using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering.GPUSkinning.Editor
{
    [CreateAssetMenu(fileName = "AnimatorControllerTempleteSO", menuName = "Tools/GPU Skinning/AnimatorControllerTempleteSO")]
    public class AnimatorControllerTempleteSO : ScriptableObject
    {
        public List<AnimatorStateInfo> animatorStateInfos = new();
    }

    [System.Serializable]
    public class AnimatorStateInfo
    {
        public string stateName;
    }
}
