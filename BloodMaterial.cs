using UnityEngine;

namespace RealisticBleeding
{
    public static class BloodMaterial
    {
        private static Material _material;
        private static Material _debugMaterial;

        public static Material Material
        {
            get
            {
                if (_material != null)
                {
                    return _material;
                }

                var shader = Shader.Find("ThunderRoad/Lit");

                if (shader == null) return null;
                
                _material = new Material(shader);

                var color = new Color(0.38f, 0f, 0f);
                _material.SetColor("_BaseColor", color);
                _material.SetFloat("_Metallic", 0.8f);
                _material.SetFloat("_Smoothness", 0.8f);

                return _material;
            }
        }

        public static Material DebugMaterial
        {
            get
            {
                if (_debugMaterial != null)
                {
                    return _debugMaterial;
                }

                var shader = Shader.Find("ThunderRoad/Lit");

                if (shader == null) return null;
                
                _debugMaterial = new Material(shader);

                var color = new Color(1f, 0f, 1f);
                _debugMaterial.SetColor("_BaseColor", color);
                _debugMaterial.SetFloat("_Metallic", 0f);
                _debugMaterial.SetFloat("_Smoothness", 0.8f);

                return _debugMaterial;
            }
        }
    }
}