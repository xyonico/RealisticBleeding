using UnityEngine;

namespace RealisticBleeding.Components
{
	public readonly struct LayerMasks
	{
		public readonly LayerMask Surface;
		public readonly LayerMask Environment;
		public readonly LayerMask Combined;

		public LayerMasks(LayerMask surface, LayerMask environment)
		{
			Surface = surface;
			Environment = environment;
			
			Combined = Surface | Environment;
		}
	}
}