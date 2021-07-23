using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public class Bleeder : MonoBehaviour
	{
		private static bool _hasAssignedLayerMask;
		private static int _layerMask;

		private const float FrequencyRangeMin = 0.7f;
		private const float FrequencyRangeMax = 1.2f;
		private const float DurationRangeMin = 2;
		private const float DurationRangeMax = 4;

		public float FrequencyMultiplier { get; set; } = 1;
		public float DurationMultiplier { get; set; } = 1;
		public float SizeMultiplier { get; set; } = 1;
		public Vector2 Dimensions { get; set; } = new Vector2(0.01f, 0.01f);

		private float _timer;
		private float _nextDropTime;
		private float _durationRemaining;

		private void Awake()
		{
			_durationRemaining = Random.Range(DurationRangeMin, DurationRangeMax) * DurationMultiplier;
			
			if (!_hasAssignedLayerMask)
			{
				_layerMask = LayerMask.GetMask(nameof(LayerName.Avatar), nameof(LayerName.Ragdoll), nameof(LayerName.NPC),
					nameof(LayerName.PlayerHandAndFoot));
				
				_hasAssignedLayerMask = true;
			}
		}

		private void Update()
		{
			var deltaTime = Time.deltaTime;

			_timer += deltaTime;

			if (_timer > _nextDropTime)
			{
				_timer -= _nextDropTime;

				_nextDropTime = Random.Range(FrequencyRangeMin, FrequencyRangeMax) / FrequencyMultiplier;
				
				SpawnDroplet();
			}

			_durationRemaining -= deltaTime;

			if (_durationRemaining <= 0)
			{
				Destroy(gameObject);
			}
		}

		private void SpawnDroplet()
		{
			var bloodDropObject = new GameObject("Blood Drop");
			var bloodDrop = bloodDropObject.AddComponent<BloodDrop>();
			var decalDrawer = bloodDropObject.AddComponent<BloodDropDecalDrawer>();

			bloodDrop.LayerMask = _layerMask;

			var randomOffset = new Vector3(Random.Range(-Dimensions.x, Dimensions.x), 0, Random.Range(-Dimensions.y, Dimensions.y));
			randomOffset *= 0.5f;

			bloodDrop.transform.position = transform.TransformPoint(randomOffset);
			bloodDrop.AttachToNearestCollider(0.2f);
		}
	}
}