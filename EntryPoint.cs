using System;
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
using Random = UnityEngine.Random;

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

		private static EffectData _bloodDropDecalData;

		internal static void OnLoaded()
		{
			try
			{
				var configPath = Path.Combine(Application.streamingAssetsPath, "Mods/RealisticBleeding/config.yaml");

				if (!File.Exists(configPath))
				{
					Configuration = new Configuration();
				}
				else
				{
					var deserializer = new DeserializerBuilder().Build();
					Configuration = deserializer.Deserialize<Configuration>(File.ReadAllText(configPath));
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);

				Configuration = new Configuration();
			}

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

			var bleedersSet = World.GetEntities().With<Bleeder>().AsSet();
			EffectRevealPatches.PlayPostPatch.ActiveBleeders = bleedersSet;

			_fixedUpdateSystem = new SequentialSystem<float>(
				new BleederSystem(World, Configuration.BloodAmountMultiplier, Configuration.BloodStreakWidthMultiplier),
				new FallingBloodDropSystem(fallingBloodDropSet),
				new SurfaceBloodDropOptimizationSystem(surfaceBloodDropSet, Configuration.MaxActiveBloodDrips),
				new SurfaceBloodDropVelocityRandomnessSystem(shouldUpdateSurfaceBloodDropSet),
				new SurfaceBloodDropPhysicsSystem(shouldUpdateSurfaceBloodDropSet, Collider, Configuration.BloodSurfaceFrictionMultiplier),
				new BloodDropDrippingSystem(shouldUpdateSurfaceBloodDropSet),
				new SurfaceBloodDecalSystem(shouldUpdateSurfaceBloodDropSet),
				new LifetimeSystem(World.GetEntities().With<Lifetime>().AsSet()),
				new ActionSystem<float>(_ => shouldUpdateSurfaceBloodDropSet.Complete()));

			_updateSystem = new SequentialSystem<float>(
				new FallingBloodDropRenderingSystem(fallingBloodDropSet, sphereMesh, BloodMaterial.Material));

			var disposeWithCreatureSystem = new DisposeWithCreatureSystem(World);

			World.Subscribe((in BloodDropHitSurface hitSurface) =>
			{
				var surfaceCollider = new SurfaceCollider(hitSurface.Collider, Vector3.zero);
				ref var bloodDrop = ref hitSurface.Entity.Get<BloodDrop>();

				bloodDrop.Position = hitSurface.Collider.transform.InverseTransformPoint(bloodDrop.Position);
				hitSurface.Entity.Set(surfaceCollider);

				var rb = hitSurface.Collider.attachedRigidbody;
				if (!rb) return;

				if (rb.TryGetComponent(out RagdollPart ragdollPart))
				{
					hitSurface.Entity.Set(new DisposeWithCreature(ragdollPart.ragdoll.creature));
				}
			});

			World.Subscribe<BloodDropHitEnvironment>(OnBloodDropHitEnvironment);
		}

		private static void OnBloodDropHitEnvironment(in BloodDropHitEnvironment message)
		{
			ref var bloodDrop = ref message.Entity.Get<BloodDrop>();

			if (_bloodDropDecalData == null)
			{
				_bloodDropDecalData = Catalog.GetData<EffectData>("DropBlood");
			}

			var rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), message.Normal) * Quaternion.LookRotation(message.Normal);
			var instance = _bloodDropDecalData.Spawn(bloodDrop.Position, rotation);
			instance.Play();

			if (instance.effects == null || instance.effects.Count == 0) return;

			var effectDecal = instance.effects[0] as EffectDecal;

			if (effectDecal == null) return;

			const float decalScale = 0.1f;
			effectDecal.meshRenderer.transform.localScale = new Vector3(decalScale, decalScale, decalScale);
		}

		internal static void OnUpdate()
		{
			try
			{
				_updateSystem.Update(Time.deltaTime);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
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