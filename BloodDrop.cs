using System.Collections.Generic;
using UnityEngine;

namespace RealisticBleeding
{
	public class BloodDrop : MonoBehaviour
	{
		private static readonly List<BloodDrop> _activeBloodDrops = new List<BloodDrop>();
		private static readonly Collider[] _colliders = new Collider[16];

		[SerializeField]
		private float _radius = 0.005f;

		[SerializeField]
		private float _surfaceDrag = 40;

		[SerializeField]
		private float _dripDurationRequired = 1f;
		
		[SerializeField]
		private float _maxVelocityToDrip = 0.08f;

		[SerializeField]
		private float _noiseScale = 20;

		[SerializeField]
		private float _noiseMaxAngle = 4;

		private Vector3 _velocity;
		private SphereCollider _myCollider;
		private bool _isOnSurface;
		private Collider _surfaceCollider;
		private Vector3 _surfacePosition;
		private float _dripTime;
		private float _distanceTravelledOnSurface;

		public int LayerMask { get; set; } = ~0;

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
			_myCollider.radius = _radius;
			_myCollider.isTrigger = true;
			
			Destroy(gameObject, 7);
			
			_activeBloodDrops.Add(this);

			if (_activeBloodDrops.Count >= EntryPoint.Configuration.MaxActiveBloodDrips)
			{
				var randomIndex = Random.Range(1, _activeBloodDrops.Count);
				var randomDrop = _activeBloodDrops[randomIndex];
				_activeBloodDrops.RemoveAt(randomIndex);
				Destroy(randomDrop.gameObject);
			}
		}

		private void OnDestroy()
		{
			_activeBloodDrops.Remove(this);
		}

		private void FixedUpdate()
		{
			AddGravityForce();

			if (!_isOnSurface)
			{
				if (Physics.SphereCast(transform.position, _radius, _velocity.normalized, out var hit, _velocity.magnitude, LayerMask,
					QueryTriggerInteraction.Ignore))
				{
					AssignNewSurfaceValues(hit.point, hit.collider);

					_distanceTravelledOnSurface = Random.Range(-100f, 100f);

					return;
				}
			}
			else
			{
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
						_surfaceCollider = null;
					}
				}
				else
				{
					_dripTime = 0;
				}

				return;
			}

			transform.position += _velocity * Time.deltaTime;
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

			var count = Physics.OverlapSphereNonAlloc(position, maxRadius, _colliders, LayerMask, QueryTriggerInteraction.Ignore);

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

			AssignNewSurfaceValues(closestPoint, closestCollider);
		}

		private bool Depenetrate()
		{
			var any = false;

			var count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _colliders, LayerMask, QueryTriggerInteraction.Ignore);

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

		private static bool AnyNaN(Vector3 vector3)
		{
			return float.IsNaN(vector3.x) || float.IsNaN(vector3.y) || float.IsNaN(vector3.z);
		}
	}
}