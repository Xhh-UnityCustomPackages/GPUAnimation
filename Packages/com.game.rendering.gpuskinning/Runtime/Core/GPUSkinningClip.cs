using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameWish.Game
{
    public enum GPUSkinningWrapMode
    {
        Once,
        Loop
    }

    [System.Serializable]
    public class GPUSkinningClip
    {
        public string name;
        public float length;
        public float frameRate;
        public int pixelSegmentation = 0;

        public GPUSkinningWrapMode wrapMode = GPUSkinningWrapMode.Once;
        public GPUSkinningFrame[] frames = null;
        public GPUSkinningAnimEvent[] events = null;



        public int totalFrameCount => (int)(frameRate * length);
    }
}
