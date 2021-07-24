using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RainyReignGames.RevealMask;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticBleeding
{
	/// <summary>
	/// There's a bug in the base game that causes everything in mirrors to have inverted culling if RevealMaskProjection executed commands in that frame.
	/// This patch fixes that by ensuring the Mirror script doesn't invert culling if RevealMaskProjection executed commands in that frame.
	/// </summary>
	public static class MirrorInvertCullingBugPatch
	{
		/*
		[HarmonyPatch(typeof(RevealMaskProjection), "Project", typeof(Matrix4x4), typeof(Matrix4x4), typeof(Texture), typeof(Vector4), typeof(float),
			typeof(List<RevealMaterialController>), typeof(RevealData[]), typeof(RevealMaskProjection.OnCompleted))]
		public static class ProjectPatch
		{
			public static bool ShouldInvertThisFrame = true;

			static ProjectPatch()
			{
				RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
			}

			private static void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
			{
				if (!ShouldInvertThisFrame)
				{
					Debug.Log("Should invert TRUE");
					ShouldInvertThisFrame = true;
				}
			}

			public static void Postfix()
			{
				Debug.Log("Should invert FALSE");
				ShouldInvertThisFrame = false;
			}
		}
		*/

		[HarmonyPatch(typeof(Mirror), "RenderCam")]
		public static class RenderCamPatch
		{
			private static CommandBuffer _invertCullingCommandBuffer;
			private static CommandBuffer _restoreCullingCommandBuffer;

			public static void Prefix(ScriptableRenderContext context, Camera camera, Collider ___workingArea)
			{
				if (___workingArea && !___workingArea.bounds.Contains(camera.transform.position))
				{
					return;
				}

				if (_invertCullingCommandBuffer == null)
				{
					_invertCullingCommandBuffer = new CommandBuffer();
					_invertCullingCommandBuffer.SetInvertCulling(true);
				}
				
				context.ExecuteCommandBuffer(_invertCullingCommandBuffer);
			}

			public static void Postfix(ScriptableRenderContext context, Camera camera, Collider ___workingArea)
			{
				if (___workingArea && !___workingArea.bounds.Contains(camera.transform.position))
				{
					return;
				}
				
				if (_restoreCullingCommandBuffer == null)
				{
					_restoreCullingCommandBuffer = new CommandBuffer();
					_restoreCullingCommandBuffer.SetInvertCulling(false);
				}
				
				context.ExecuteCommandBuffer(_restoreCullingCommandBuffer);
			}
		}
	}
}