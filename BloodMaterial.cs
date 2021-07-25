using UnityEngine;

namespace RealisticBleeding
{
	public static class BloodMaterial
	{
		private static Material _material;
	
		public static Material Material
		{
			get
			{
				if (_material != null)
				{
					return _material;
				}
			
				var shader = Shader.Find("Universal Render Pipeline/Lit");
				_material = new Material(shader);

				var color = new Color(0.38f, 0f, 0f);
				_material.SetColor("_BaseColor", color);
				_material.SetFloat("_Metallic", 0.8f);
				_material.SetFloat("_Smoothness", 0.8f);
				_material.enableInstancing = true;

				return _material;
			}
		}
	}
}