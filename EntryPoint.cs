using System.IO;
using DefaultEcs;
using DefaultEcs.System;
using HarmonyLib;
using RealisticBleeding.Components;
using RealisticBleeding.Messages;
using RealisticBleeding.Systems;
using ThunderRoad;
using UnityEngine;
using YamlDotNet.Serialization;
using Object = UnityEngine.Object;

namespace RealisticBleeding
{
	internal static class EntryPoint
	{
		private const string HarmonyID = "com.xyonico.realistic-bleeding";

		private static bool _hasLoaded;

		public static Configuration Configuration { get; private set; }

		public static readonly World World = new World();

		private static ISystem<float> _fixedUpdateSystem;
		private static ISystem<float> _updateSystem;

		internal static SphereCollider Collider { get; private set; }

		internal static void OnLoaded()
		{
			var configPath = Path.Combine(Path.GetDirectoryName(typeof(EntryPoint).Assembly.Location), "config.yaml");
			Debug.Log(configPath);
			var deserializer = new DeserializerBuilder().Build();
			Configuration = deserializer.Deserialize<Configuration>(File.ReadAllText(configPath));

			if (_hasLoaded) return;
			_hasLoaded = true;

			Debug.Log("Realistic Bleeding loaded!");

			var harmony = new Harmony(HarmonyID);
			harmony.PatchAll(typeof(EntryPoint).Assembly);

			var surfaceLayerMask = LayerMask.GetMask(nameof(LayerName.Avatar), nameof(LayerName.Ragdoll), nameof(LayerName.NPC),
				nameof(LayerName.PlayerHandAndFoot));
			var environmentLayerMask = LayerMask.GetMask(nameof(LayerName.Default), "NoLocomotion");
			
			var spherePrimitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			Object.DontDestroyOnLoad(spherePrimitive);

			Collider = spherePrimitive.GetComponent<SphereCollider>();
			Collider.radius = 0.003f;
			Collider.isTrigger = true;

			var sphereMesh = spherePrimitive.GetComponent<MeshFilter>().sharedMesh;
			spherePrimitive.transform.position = new Vector3(100000, 100000, 100000);
			
			World.SetMaxCapacity<LayerMasks>(1);
			World.Set(new LayerMasks(surfaceLayerMask, environmentLayerMask));

			World.SetMaxCapacity<DeltaTimeMultiplier>(1);
			World.Set(new DeltaTimeMultiplier(1));

			var surfaceBloodDropSet = World.GetEntities().With<BloodDrop>().With<SurfaceCollider>().AsSet();
			var shouldUpdateSurfaceBloodDropSet = World.GetEntities().With<BloodDrop>().With<SurfaceCollider>()
				.WhenAdded<ShouldUpdate>().WhenChanged<ShouldUpdate>().AsSet();
			var fallingBloodDropSet = World.GetEntities().With<BloodDrop>().Without<SurfaceCollider>().AsSet();
			var didUpdateBloodDropSet = World.GetEntities().With<BloodDrop>().With<SurfaceCollider>()
				.WhenAdded<DidUpdate>().WhenChanged<DidUpdate>().AsSet();

			_fixedUpdateSystem = new SequentialSystem<float>(
				new BleederSystem(World),
				new FallingBloodDropSystem(fallingBloodDropSet),
				new SurfaceBloodDropOptimizationSystem(surfaceBloodDropSet, Configuration.MaxActiveBloodDrips),
				new SurfaceBloodDropVelocityRandomnessSystem(shouldUpdateSurfaceBloodDropSet),
				new SurfaceBloodDropPhysicsSystem(shouldUpdateSurfaceBloodDropSet, Collider, Configuration.BloodSurfaceFrictionMultiplier),
				new BloodDropDrippingSystem(shouldUpdateSurfaceBloodDropSet),
				new LifetimeSystem(World.GetEntities().With<Lifetime>().AsSet()),
				new ActionSystem<float>(_ => shouldUpdateSurfaceBloodDropSet.Complete()));

			_updateSystem = new SurfaceBloodDecalSystem(didUpdateBloodDropSet);

			World.Subscribe((in BloodDropHitSurface hitSurface) =>
			{
				var surfaceCollider = new SurfaceCollider(hitSurface.Collider, Vector3.zero);
				ref var bloodDrop = ref hitSurface.Entity.Get<BloodDrop>();

				bloodDrop.Position = hitSurface.Collider.transform.InverseTransformPoint(bloodDrop.Position);
				hitSurface.Entity.Set(surfaceCollider);
			});
		}

		internal static void OnUpdate()
		{
			_updateSystem.Update(Time.deltaTime);
			
			if (Input.GetKeyDown(KeyCode.T))
			{
				var cam = Spectator.local.cam.transform;
				const float hitRange = 10f;

				var layerMask = LayerMask.GetMask(nameof(LayerName.Avatar), nameof(LayerName.Ragdoll), nameof(LayerName.NPC),
					nameof(LayerName.PlayerHandAndFoot));

				if (Physics.Raycast(cam.position, cam.forward, out var hit, hitRange, layerMask))
				{
					var rigidbody = hit.collider.attachedRigidbody;

					if (rigidbody == null) return;

					if (rigidbody.TryGetComponent(out RagdollPart part))
					{
						SpawnBloodDrop(hit.point);
						var creature = part.ragdoll.creature;

						//NoseBleed.SpawnOn(creature, 1, 1, 0.4f);
						//MouthBleed.SpawnOn(creature, 1, 1);
					}
				}
			}
		}

		internal static void OnFixedUpdate()
		{
			_fixedUpdateSystem.Update(Time.deltaTime);
		}

		private static void SpawnBloodDrop(Vector3 position)
		{
			BloodDrop.Spawn(position, Vector3.zero, 0.01f);
		}
	}
}