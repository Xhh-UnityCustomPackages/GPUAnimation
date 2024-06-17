using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

namespace GameWish.Game
{
    public class GPUSkinningUtil
    {

        public static Texture2D CreateTexture2D(TextAsset textureRawData, GPUSkinningAnimation anim)
        {
            if (textureRawData == null || anim == null)
            {
                return null;
            }

            Texture2D texture = new Texture2D(anim.textureWidth, anim.textureHeight, TextureFormat.RGBAHalf, false, true);
            texture.name = "GPUSkinningTextureMatrix";
            texture.filterMode = FilterMode.Point;
            texture.LoadRawTextureData(textureRawData.bytes);
            texture.Apply(false, true);

            return texture;
        }

        public static string BoneHierarchyPath(GPUSkinningBone[] bones, int boneIndex)
        {
            if (bones == null || boneIndex < 0 || boneIndex >= bones.Length)
            {
                return null;
            }

            GPUSkinningBone bone = bones[boneIndex];
            string path = bone.name;
            while (bone.parentBoneIndex != -1)
            {
                bone = bones[bone.parentBoneIndex];
                path = bone.name + "/" + path;
            }
            return path;
        }


        public static string MD5(string input)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] bytValue, bytHash;
            bytValue = System.Text.Encoding.UTF8.GetBytes(input);
            bytHash = md5.ComputeHash(bytValue);
            md5.Clear();
            string sTemp = string.Empty;
            for (int i = 0; i < bytHash.Length; i++)
            {
                sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
            }
            return sTemp.ToLower();
        }
    }
}
