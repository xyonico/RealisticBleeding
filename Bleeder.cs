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

		public static Bleeder Spawn(Vector3 position, Quaternion rotation, Transform parent)
		{
			var bleederTransform = new GameObject("Bleeder").transform;
			var bleeder = bleederTransform.gameObject.AddComponent<Bleeder>();
			bleederTransform.parent = parent;
			bleederTransform.position = position;
			bleederTransform.rotation = rotation;

			return bleeder;
		}

		private void Awake()
		{
			_durationRemaining = Random.Range(DurationRangeMin, DurationRangeMax) * DurationMultiplier * EntryPoint.Configuration.BleedDurationMultiplier;
			
			if (!_hasAssignedLayerMask)
			{
				_layerMask = LayerMask.GetMask(nameof(LayerName.Avatar), nameof(LayerName.Ragdoll), nameof(LayerName.NPC),
					nameof(LayerName.PlayerHandAndFoot));
				
				_hasAssignedLayerMask = true;
			}
			
			_nextDropTime = Random.Range(FrequencyRangeMin, FrequencyRangeMax) / (FrequencyMultiplier * EntryPoint.Configuration.BloodAmountMultiplier);
			_nextDropTime *= 0.4f; // The first drop should spawn sooner.
		}

		private void OnDisable()
		{
			Destroy(gameObject);
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
				var randomVelocity = Random.insideUnitSphere * 0.75f;
				var gravityDir = Physics.gravity.normalized;

				if (Vector3.Dot(randomVelocity.normalized, gravityDir) > 0)
				{
					randomVelocity = Vector3.ProjectOnPlane(randomVelocity, gravityDir);
				}
				
				bloodDrop.Velocity = randomVelocity;
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