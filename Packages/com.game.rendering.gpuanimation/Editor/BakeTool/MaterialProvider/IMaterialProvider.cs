using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GPUSkinning.Editor
{
    public interface IMaterialProvider
    {
        string name { get; }
        Material GetMaterial();
    }
}
