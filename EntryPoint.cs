using System;
using System.Collections.Generic;
using RealisticBleeding.Components;
using RealisticBleeding.Systems;
using ThunderRoad;
using UnityEngine;
using Keyboard = UnityEngine.InputSystem.Keyboard;
using Object = UnityEngine.Object;

namespace RealisticBleeding
{
    internal static class EntryPoint
    {
        private static bool _hasLoaded;

        [ModOptionCategory("Features", 0)]
        [ModOptionButton]
        [ModOption("Pause Simulation",
            "Pauses all blood droplet simulation if enabled.\nGood for quickly disabling the mod temporarily to see the performance difference.",
            order = 0)]
        public static bool PauseSimulation;

        internal static SphereCollider Collider { get; private set; }

        public static readonly FastList<Bleeder> Bleeders = new FastList<Bleeder>(512);
        public static readonly FastList<SurfaceBloodDrop> SurfaceBloodDrops = new FastList<SurfaceBloodDrop>(1024);
        public static readonly FastList<FallingBloodDrop> FallingBloodDrops = new FastList<FallingBloodDrop>(1024);

        public static LayerMask SurfaceLayerMask { get; private set; }
        public static LayerMask EnvironmentLayerMask { get; private set; }
        public static LayerMask SurfaceAndEnvironmentLayerMask { get; private set; }

        private static List<BaseSystem> _fixedUpdateSystems;
        private static List<BaseSystem> _updateSystems;

        internal static void OnLoaded()
        {
            if (_hasLoaded) return;
            _hasLoaded = true;

            Debug.Log("Realistic Bleeding loaded!");

            SurfaceLayerMask = LayerMask.GetMask(nameof(LayerName.Avatar), nameof(LayerName.Ragdoll),
                nameof(LayerName.NPC),
                nameof(LayerName.PlayerHandAndFoot));
            EnvironmentLayerMask = LayerMask.GetMask(nameof(LayerName.Default), "NoLocomotion");

            SurfaceAndEnvironmentLayerMask = SurfaceLayerMask | EnvironmentLayerMask;

            var spherePrimitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DontDestroyOnLoad(spherePrimitive);

            Collider = spherePrimitive.GetComponent<SphereCollider>();
            Collider.radius = 0.003f;
            Collider.isTrigger = true;

            var sphereMesh = spherePrimitive.GetComponent<MeshFilter>().sharedMesh;
            spherePrimitive.transform.position = new Vector3(100000, 100000, 100000);

            EffectRevealPatches.PlayPatch.Init();

            _fixedUpdateSystems = new List<BaseSystem>
            {
                new BleederSystem(Bleeders, SurfaceBloodDrops),
                new FallingBloodDropSystem(FallingBloodDrops, SurfaceBloodDrops),
                new SurfaceBloodDropUpdateSystem(SurfaceBloodDrops, FallingBloodDrops, Collider)
            };
            
            _updateSystems = new List<BaseSystem>
            {
                new FallingBloodDropRenderingSystem(FallingBloodDrops, sphereMesh),
                //new DebugSurfaceBloodSystem(SurfaceBloodDrops, sphereMesh, BloodMaterial.DebugMaterial)
            };

            var surfaceBloodDecalSystem = new SurfaceBloodDecalSystem(SurfaceBloodDrops);
            var disposeWithCreatureSystem = new DisposeWithCreatureSystem(SurfaceBloodDrops, Bleeders);
        }

        internal static void OnUpdate()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                var cam = Spectator.local.cam.transform;
                const float hitRange = 10f;

                if (Physics.Raycast(cam.position, cam.forward, out var hit, hitRange, SurfaceLayerMask))
                {
                    var rigidbody = hit.collider.attachedRigidbody;

                    if (rigidbody == null) return;

                    if (rigidbody.TryGetComponent(out RagdollPart part))
                    {
                        var fallingDrop = new FallingBloodDrop(hit.point, Vector3.zero, 0.01f, 8);
                        var surfaceDrop = new SurfaceBloodDrop(in fallingDrop, hit.collider);
                        SurfaceBloodDrops.TryAddNoResize(surfaceDrop);
                    }
                }
            }

            if (PauseSimulation) return;

            var deltaTime = Time.deltaTime;

            foreach (var updateSystem in _updateSystems)
            {
                try
                {
                    updateSystem.Update(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static void OnFixedUpdate()
        {
            if (PauseSimulation) return;

            var deltaTime = Time.deltaTime;

            foreach (var fixedUpdateSystem in _fixedUpdateSystems)
            {
                try
                {
                    fixedUpdateSystem.Update(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static void SpawnBloodDrop(Vector3 position)
        {
            //SurfaceBloodDrop.Spawn(position, Vector3.zero, 0.01f);
        }
    }
}