using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace GameWish.Game.Editor
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
                numPixels += animation.bones.Length * 3 /*treat 3 pixels as a float3x4*/ * numFrames;
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


        private void CreateTextureMatrix(string savePath)
        {
            Texture2D texture = new Texture2D(animation.textureWidth, animation.textureHeight, TextureFormat.RGBAHalf, false, true);
            Color[] pixels = texture.GetPixels();
            int pixelIndex = 0;
            for (int clipIndex = 0; clipIndex < animation.clips.Length; ++clipIndex)
            {
                GPUSkinningClip clip = animation.clips[clipIndex];
                GPUSkinningFrame[] frames = clip.frames;
                int numFrames = frames.Length;
                for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
                {
                    GPUSkinningFrame frame = frames[frameIndex];
                    Matrix4x4[] matrices = frame.matrices;
                    int numMatrices = matrices.Length;
                    for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
                    {
                        Matrix4x4 matrix = matrices[matrixIndex];
                        pixels[pixelIndex++] = new Color(matrix.m00, matrix.m01, matrix.m02, matrix.m03);
                        pixels[pixelIndex++] = new Color(matrix.m10, matrix.m11, matrix.m12, matrix.m13);
                        pixels[pixelIndex++] = new Color(matrix.m20, matrix.m21, matrix.m22, matrix.m23);
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply();

            //直接保存为Texture2D
            string savedPath = savePath + $"GPUSKinning_Texture_{target.name}.asset";
            AssetDatabase.CreateAsset(texture, savedPath);
            AssetDatabase.Refresh();

            var textureInfo = AssetDatabase.LoadAssetAtPath<Texture>(savedPath);
            animation.textureAsset = textureInfo;


            // string savedPath = savePath + $"GPUSKinning_Texture_{target.name}.bytes";
            // using (FileStream fileStream = new FileStream(savedPath, FileMode.Create))
            // {
            //     byte[] bytes = texture.GetRawTextureData();
            //     fileStream.Write(bytes, 0, bytes.Length);
            //     fileStream.Flush();
            //     fileStream.Close();
            //     fileStream.Dispose();
            // }
            //
            // AssetDatabase.Refresh();
            // var textureInfo = AssetDatabase.LoadAssetAtPath<TextAsset>(savedPath);
            // animation.texture = textureInfo;
        }
    }
}