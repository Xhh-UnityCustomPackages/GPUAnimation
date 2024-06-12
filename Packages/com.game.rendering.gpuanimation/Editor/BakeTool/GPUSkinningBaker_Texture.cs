using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GPUSkinning.Editor
{
    public partial class GPUSkinningBaker
    {

        private void InitTextureInfo()
        {
            int numPixels = 0;

            GPUSkinningClip[] clips = animation.clips;
            int numClips = clips.Length;
            for (int clipIndex = 0; clipIndex < numClips; ++clipIndex)
            {
                GPUSkinningClip clip = clips[clipIndex];
                clip.pixelSegmentation = numPixels;

                GPUSkinningFrame[] frames = clip.frames;
                int numFrames = frames.Length;
                numPixels += animation.bones.Length * 3/*treat 3 pixels as a float3x4*/ * numFrames;
            }

            CalculateTextureSize(numPixels, out animation.textureWidth, out animation.textureHeight);
        }


        private void CalculateTextureSize(int numPixels, out int texWidth, out int texHeight)
        {
            texWidth = 1;
            texHeight = 1;
            while (true)
            {
                if (texWidth * texHeight >= numPixels) break;
                texWidth *= 2;
                if (texWidth * texHeight >= numPixels) break;
                texHeight *= 2;
            }
        }
    }
}
