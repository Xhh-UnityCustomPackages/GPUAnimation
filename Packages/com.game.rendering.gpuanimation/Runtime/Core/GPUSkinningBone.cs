using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GPUSkinning
{
    [System.Serializable]
    public class GPUSkinningBone
    {
        [System.NonSerialized]
        public Transform transform = null;

        public string guid = null;


        public string name = null;
        public Matrix4x4 bindpose;
        public int parentBoneIndex = -1;
        public int[] childrenBonesIndices = null;
    }
}
