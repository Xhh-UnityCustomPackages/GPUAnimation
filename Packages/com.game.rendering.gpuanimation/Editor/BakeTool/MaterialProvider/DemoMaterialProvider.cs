using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Game.GPUSkinning.Editor
{
    public class DemoMaterialProvider : IMaterialProvider
    {
        public string name => "Demo";

        public Material GetMaterial()
        {
            Material material = new Material(Shader.Find("GPUSkinning/GPUSkinningDemo"));
            material.enableInstancing = true;
            return material;
        }
    }
}
