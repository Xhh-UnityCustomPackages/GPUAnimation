using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Game.GPUSkinning
{
    public class GPUSkinningAnimation : ScriptableObject
    {
        public string guid = null;
        public string name = null;
        public GPUSkinningBone[] bones = null;
        public int rootBoneIndex = 0;
        public GPUSkinningClip[] clips = null;


        public Bounds bounds;
        public int textureWidth = 0;
        public int textureHeight = 0;


        public Mesh mesh = null;

    }
}
