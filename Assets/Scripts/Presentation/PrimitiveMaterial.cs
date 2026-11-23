using System;
using UnityEngine;

namespace MouseKeeper
{
    static class PrimitiveMaterial
    {
        static Material material;

        public static Material Shared
        {
            get
            {
                if (material == null)
                {
                    material = Resources.Load<Material>("PrimitiveMaterial");
                    if (material == null || material.shader == null || !material.shader.isSupported)
                        throw new InvalidOperationException("The URP primitive material is missing or unsupported.");
                }
                return material;
            }
        }
    }
}
