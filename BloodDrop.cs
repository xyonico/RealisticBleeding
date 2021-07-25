using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public class BloodDrop : MonoBehaviour
	{
		internal static readonly List<BloodDrop> ActiveBloodDrops = new List<BloodDrop>();
		
		private static readonly Collider[] _colliders = new Collider[16];

		[SerializeField]
		private float _radius = 0.003f;

		[SerializeField]
		private float _surfaceDrag = 55;

		[SerializeField]
		private Vector2 _dripDurationRequiredRange = new Vector2(0.25f, 0.75f);

		[SerializeField]
		private float _maxVelocityToDrip = 0.08f;

		[SerializeField]
		private float _noiseScale = 20;

		[SerializeField]
		private float _noiseMaxAngle = 6;

		private Vector3 _velocity;
		private SphereCollider _myCollider;
		private bool _isOnSurface;
		private Collider _surfaceCollider;
		private Vector3 _surfacePosition;
		private float _dripTime;
		private float _distanceTravelledOnSurface;
		private Renderer _renderer;
		private float _dripDurationRequired;

		public LayerMask SurfaceLayerMask { get; set; } = ~0;
		public LayerMask EnvironmentLayerMask { get; set; } = LayerMask.GetMask(LayerName.Default.ToString());
		public bool HasUpdated { get; set; } = true;

		private EffectData _bloodDropDecalData;

		public Vector3 Velocity
		{
			get => _velocity;
			set => _velocity = value;
		}

		public Collider SurfaceCollider => _surfaceCollider;
		public Vector3 LastSurfaceNormal { get; private set; }

		private void Awake()
		{
			_myCollider = gameObject.AddComponent<SphereCollider>();
			_myCollider.isTrigger = true;
			transform.localScale = new Vector3(_radius * 2, _radius * 2, _radius * 2);

			var rendererObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			var rendererTransform = rendererObject.transform;
			rendererTransform.parent = transform;
			rendererTransform.localPosition = Vector3.zero;
			rendererTransform.localScale = Vector3.one;

			Destroy(rendererObject.GetComponent<Collider>());
			_renderer = rendererObject.GetComponent<Renderer>();
			_renderer.sharedMaterial = BloodMaterial.Material;

			_dripDurationRequired = Random.Range(_dripDurationRequiredRange.x, _dripDurationRequiredRange.y);

			Destroy(gameObject, Random.Range(5f, 8f));

			ActiveBloodDrops.Insert(Random.Range(0, ActiveBloodDrops.Count), this);
		}

		private void OnDestroy()
		{
			ActiveBloodDrops.Remove(this);
		}

		private void FixedUpdate()
		{
			if (_isOnSurface) return;
			
			AddGravityForce();
			
			var combinedLayerMask = SurfaceLayerMask | EnvironmentLayerMask;

			if (Physics.SphereCast(transform.position, _radius, _velocity.normalized, out var hit, _velocity.magnitude * Time.deltaTime,
				combinedLayerMask,
				QueryTriggerInteraction.Ignore))
			{
				if (EnvironmentLayerMask.Contains(hit.collider.gameObject.layer))
				{
					OnCollidedWithEnvironment(hit);

					return;
				}

				AssignNewSurfaceValues(hit.point, hit.collider);

				_distanceTravelledOnSurface = Random.Range(-100f, 100f);

				_renderer.enabled = false;

				return;
			}

			transform.forward = _velocity;
			var size = Mathf.Lerp(1, 3.5f, Mathf.InverseLerp(0, 4, _velocity.magnitude));
			_renderer.transform.localScale = new Vector3(1, 1, size);
			
			transform.position += _velocity * Time.deltaTime;
		}

		public bool DoUpdate()
		{
			if (!_isOnSurface) return false;

			AddGravityForce();

			transform.position = _surfaceCollider.transform.TransformPoint(_surfacePosition);

			var prevPos = transform.position;

			_velocity = RandomizeVector(_velocity);

			Depenetrate();

			_velocity *= 1 - Time.deltaTime * _surfaceDrag;

			var newPos = transform.position + _velocity * Time.deltaTime;

			transform.position = newPos;

			if (!Depenetrate())
			{
				var closestPoint = _surfaceCollider.ClosestPoint(newPos);

				LastSurfaceNormal = (newPos - closestPoint).normalized;

				AssignNewSurfaceValues(closestPoint, _surfaceCollider);
			}

			var velocity = (prevPos - transform.position).magnitude;

			_distanceTravelledOnSurface += velocity * _noiseScale;

			var maxVelocity = _maxVelocityToDrip * Time.deltaTime;

			if (velocity < maxVelocity && Vector3.Dot(LastSurfaceNormal, Physics.gravity.normalized) > 0)
			{
				_dripTime += Time.deltaTime;

				if (_dripTime >= _dripDurationRequired)
				{
					_dripTime = 0;
					_isOnSurface = false;
					_renderer.enabled = true;

					var rb = _surfaceCollider.attachedRigidbody;
					_velocity = rb ? rb.GetPointVelocity(transform.position) : Vector3.zero;

					_surfaceCollider = null;
				}

				return false;
			}

			_dripTime = 0;
			_renderer.enabled = false;
				
			HasUpdated = true;

			return true;
		}

		private void AssignNewSurfaceValues(Vector3 point, Collider surfaceCollider)
		{
			_isOnSurface = true;
			_surfaceCollider = surfaceCollider;

			transform.position = point;
			_surfacePosition = _surfaceCollider.transform.InverseTransformPoint(point);
		}

		private void AddGravityForce()
		{
			_velocity += Physics.gravity * Time.deltaTime;
		}

		public void AttachToNearestCollider(float maxRadius)
		{
			var position = transform.position;

			var count = Physics.OverlapSphereNonAlloc(position, maxRadius, _colliders, SurfaceLayerMask, QueryTriggerInteraction.Ignore);

			var closestDistanceSqr = float.MaxValue;
			var closestPoint = position;
			Collider closestCollider = null;

			for (var i = 0; i < count; i++)
			{
				var col = _colliders[i];

				var point = col.ClosestPoint(position);
				var distanceSqr = (point - position).sqrMagnitude;

				if (distanceSqr < 0.0001f)
				{
					if (Physics.ComputePenetration(_myCollider, position, Quaternion.identity,
						col, col.transform.position, col.transform.rotation,
						out var direction, out var distance))
					{
						distanceSqr = distance * distance;
						point = position + direction * distance;
					}
				}

				if (distanceSqr < closestDistanceSqr)
				{
					closestDistanceSqr = distanceSqr;
					closestCollider = col;
					closestPoint = point;
				}
			}

			if (closestCollider == null) return;

			_renderer.enabled = false;
			_distanceTravelledOnSurface = Random.Range(-100f, 100f);

			AssignNewSurfaceValues(closestPoint, closestCollider);
		}

		private bool Depenetrate()
		{
			var any = false;

			var count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _colliders, SurfaceLayerMask, QueryTriggerInteraction.Ignore);

			for (var i = 0; i < count; i++)
			{
				var col = _colliders[i];

				if (col == _myCollider) continue;

				var colTransform = col.transform;

				if (Physics.ComputePenetration(_myCollider, transform.position, Quaternion.identity,
					col, colTransform.position, colTransform.rotation,
					out var direction, out var distance))
				{
					if (Mathf.Abs(distance) < 0.001f) continue;

					var offset = direction * distance;

					if (!AnyNaN(offset))
					{
						transform.position += offset;

						_velocity = Vector3.ProjectOnPlane(_velocity, direction);

						AssignNewSurfaceValues(transform.position, col);

						any = true;
					}
				}
			}

			if (any)
			{
				var velocityDir = _velocity.normalized;
				var gravityDir = Physics.gravity.normalized;
				var tangent = Vector3.Cross(velocityDir, gravityDir).normalized;

				LastSurfaceNormal = Vector3.Cross(velocityDir, tangent);
			}

			return any;
		}

		private Vector3 RandomizeVector(Vector3 vector)
		{
			var randomMultiplier = Mathf.Acos(Mathf.Clamp01(Mathf.Abs(Vector3.Dot(LastSurfaceNormal, Physics.gravity.normalized))));
			randomMultiplier *= Mathf.InverseLerp(0, 0.1f, vector.magnitude);

			var randomRotation = new Vector3
			{
				x = Mathf.PerlinNoise(_distanceTravelledOnSurface, 0) * 2f,
				y = Mathf.PerlinNoise(0, _distanceTravelledOnSurface) * 2f,
				z = Mathf.PerlinNoise(-_distanceTravelledOnSurface, 0) * 2f
			};

			randomRotation -= Vector3.one;

			randomRotation *= randomMultiplier;
			randomRotation *= _noiseMaxAngle;

			return Quaternion.Euler(randomRotation) * vector;
		}

		private void OnCollidedWithEnvironment(RaycastHit hit)
		{
			Destroy(gameObject);
			
			if (_bloodDropDecalData == null)
			{
				_bloodDropDecalData = Catalog.GetData<EffectData>("DropBlood");
			}

			var rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), hit.normal) * Quaternion.LookRotation(hit.normal);
			var instance = _bloodDropDecalData.Spawn(hit.point, rotation);
			instance.Play();

			if (instance.effects == null || instance.effects.Count == 0) return;
			
			var effectDecal = instance.effects[0] as EffectDecal;
			
			if (effectDecal == null) return;
			
			const float decalScale = 0.1f;
			effectDecal.meshRenderer.transform.localScale = new Vector3(decalScale, decalScale, decalScale);
		}

		private static bool AnyNaN(Vector3 vector3)
		{
			return float.IsNaN(vector3.x) || float.IsNaN(vector3.y) || float.IsNaN(vector3.z);
		}
	}
}