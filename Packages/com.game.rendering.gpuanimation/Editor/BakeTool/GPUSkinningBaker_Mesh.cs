using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Game.GPUSkinning.Editor
{
    public partial class GPUSkinningBaker
    {
        void CreateGPUSkinningMeshes(string savePath)
        {
            Mesh newMesh = CreateNewMesh(skinnedMeshRenderer.sharedMesh, "GPUSkinning_Mesh");
            string savedMeshPath = savePath + $"GPUSKinning_Mesh_{target.name}.asset";
            AssetDatabase.CreateAsset(newMesh, savedMeshPath);
            AssetDatabase.Refresh();

            animation.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedMeshPath);
        }

        private Mesh CreateNewMesh(Mesh mesh, string meshName)
        {
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            Color[] colors = mesh.colors;
            Vector2[] uv = mesh.uv;

            Mesh newMesh = new Mesh();
            newMesh.name = meshName;
            newMesh.vertices = mesh.vertices;
            if (normals != null && normals.Length > 0) { newMesh.normals = normals; }
            if (tangents != null && tangents.Length > 0) { newMesh.tangents = tangents; }
            if (colors != null && colors.Length > 0) { newMesh.colors = colors; }
            if (uv != null && uv.Length > 0) { newMesh.uv = uv; }

            int numVertices = mesh.vertexCount;
            BoneWeight[] boneWeights = mesh.boneWeights;
            Vector4[] uv2 = new Vector4[numVertices];
            Vector4[] uv3 = new Vector4[numVertices];
            Transform[] smrBones = skinnedMeshRenderer.bones;
            for (int i = 0; i < numVertices; ++i)
            {
                BoneWeight boneWeight = boneWeights[i];

                BoneWeightSortData[] weights = new BoneWeightSortData[4];
                weights[0] = new BoneWeightSortData() { index = boneWeight.boneIndex0, weight = boneWeight.weight0 };
                weights[1] = new BoneWeightSortData() { index = boneWeight.boneIndex1, weight = boneWeight.weight1 };
                weights[2] = new BoneWeightSortData() { index = boneWeight.boneIndex2, weight = boneWeight.weight2 };
                weights[3] = new BoneWeightSortData() { index = boneWeight.boneIndex3, weight = boneWeight.weight3 };
                System.Array.Sort(weights);

                GPUSkinningBone bone0 = GetBoneByTransform(smrBones[weights[0].index]);
                GPUSkinningBone bone1 = GetBoneByTransform(smrBones[weights[1].index]);
                GPUSkinningBone bone2 = GetBoneByTransform(smrBones[weights[2].index]);
                GPUSkinningBone bone3 = GetBoneByTransform(smrBones[weights[3].index]);

                Vector4 skinData_01 = new Vector4();
                skinData_01.x = GetBoneIndex(bone0);
                skinData_01.y = weights[0].weight;
                skinData_01.z = GetBoneIndex(bone1);
                skinData_01.w = weights[1].weight;
                uv2[i] = skinData_01;

                Vector4 skinData_23 = new Vector4();
                skinData_23.x = GetBoneIndex(bone2);
                skinData_23.y = weights[2].weight;
                skinData_23.z = GetBoneIndex(bone3);
                skinData_23.w = weights[3].weight;
                uv3[i] = skinData_23;
            }
            newMesh.SetUVs(1, new List<Vector4>(uv2));
            newMesh.SetUVs(2, new List<Vector4>(uv3));

            newMesh.triangles = mesh.triangles;
            return newMesh;
        }


        private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
        {
            public int index = 0;

            public float weight = 0;

            public int CompareTo(BoneWeightSortData b)
            {
                return weight > b.weight ? -1 : 1;
            }
        }


        private int GetBoneIndex(GPUSkinningBone bone)
        {
            return System.Array.IndexOf(animation.bones, bone);
        }


        private GPUSkinningBone GetBoneByTransform(Transform transform)
        {
            GPUSkinningBone[] bones = animation.bones;
            int numBones = bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                if (bones[i].transform == transform)
                {
                    return bones[i];
                }
            }
            return null;
        }
    }
}
