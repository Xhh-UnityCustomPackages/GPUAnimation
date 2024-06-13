using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Game.GPUSkinning
{
    public class GPUSkinningAnimation : ScriptableObject
    {
        public string guid = null;
        public new string name = null;
        public GPUSkinningBone[] bones = null;
        public int rootBoneIndex = 0;
        public GPUSkinningClip[] clips = null;


        public Bounds bounds;
        public int textureWidth = 0;
        public int textureHeight = 0;


        public Mesh mesh = null;
        public TextAsset texture = null;
        public Material material = null;




        public GPUSkinningClip GetGPUSkinningClip(string name)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < clips.Length; ++i)
            {
                if (clips[i].name == name)
                {
                    return clips[i];
                }
            }

            return null;
        }

    }
}
