using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public class Bleeder : MonoBehaviour
	{
		private static bool _hasAssignedLayerMask;
		private static int _layerMask;

		private const float FrequencyRangeMin = 0.75f;
		private const float FrequencyRangeMax = 1.75f;
		private const float DurationRangeMin = 4f;
		private const float DurationRangeMax = 10f;

		public float FrequencyMultiplier { get; set; } = 1;
		public float DurationMultiplier { get; set; } = 1;
		public float SizeMultiplier { get; set; } = 1;
		public Vector2 Dimensions { get; set; } = new Vector2(0.01f, 0.01f);

		private float _timer;
		private float _nextDropTime;
		private float _durationRemaining;

		private void Awake()
		{
			_durationRemaining = Random.Range(DurationRangeMin, DurationRangeMax) * DurationMultiplier * EntryPoint.Configuration.BleedDurationMultiplier;
			
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

				_nextDropTime = Random.Range(FrequencyRangeMin, FrequencyRangeMax) / (FrequencyMultiplier * EntryPoint.Configuration.BloodAmountMultiplier);
				
				var randomOffset = new Vector3(Random.Range(-Dimensions.x, Dimensions.x), 0, Random.Range(-Dimensions.y, Dimensions.y));
				randomOffset *= 0.5f;
				
				var dropPosition = transform.TransformPoint(randomOffset);
				
				var bloodDrop = SpawnBloodDrop(dropPosition, SizeMultiplier);
				bloodDrop.AttachToNearestCollider(0.2f);
			}

			_durationRemaining -= deltaTime;

			if (_durationRemaining <= 0)
			{
				Destroy(gameObject);
			}
		}

		public static BloodDrop SpawnBloodDrop(Vector3 position, float sizeMultiplier = 1)
		{
			var bloodDropObject = new GameObject("Blood Drop");
			var bloodDrop = bloodDropObject.AddComponent<BloodDrop>();
			var decalDrawer = bloodDropObject.AddComponent<BloodDropDecalDrawer>();

			bloodDrop.LayerMask = _layerMask;
			decalDrawer.SizeMultiplier = sizeMultiplier;

			bloodDrop.transform.position = position;

			return bloodDrop;
		}
	}
}