using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameWish.Game
{
    [System.Serializable]
    public class GPUSkinningBone
    {
        [System.NonSerialized]
        public Transform transform = null;


        public string name = null;
        public Matrix4x4 bindpose;
        public int parentBoneIndex = -1;
        public int[] childrenBonesIndices = null;
    }
}
