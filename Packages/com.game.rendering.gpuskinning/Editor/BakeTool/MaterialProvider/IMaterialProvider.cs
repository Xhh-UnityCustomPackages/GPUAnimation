using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameWish.Game.Editor
{
    public interface IMaterialProvider
    {
        string name { get; }
        Material GetMaterial();
    }
}
