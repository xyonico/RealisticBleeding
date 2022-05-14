using HarmonyLib;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticBleeding
{
	/// <summary>
	/// There's a bug in the base game that causes everything in mirrors to have inverted culling if RevealMaskProjection executed commands in that frame.
	/// The reason is because RevealMaskProjection uses CommandBuffer.SetInvertCulling(), while Mirror uses GL.invertCulling.
	/// It seems if a command buffer with that command is executed for that frame, GL.invertCulling is not used.
	/// So to fix it, I changed Mirror to also use CommandBuffer.SetInvertCulling() instead of GL.invertCulling.
	/// </summary>
	public static class MirrorInvertCullingBugPatch
	{
		[HarmonyPatch(typeof(Mirror), "RenderCam")]
		public static class RenderCamPatch
		{
			private static CommandBuffer _invertCullingCommandBuffer;
			private static CommandBuffer _restoreCullingCommandBuffer;

			public static void Prefix(ScriptableRenderContext context, Camera camera, bool ___active, MeshRenderer ___mirrorMesh)
			{
				if (!___active || !___mirrorMesh.isVisible)
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

			public static void Postfix(ScriptableRenderContext context, Camera camera, bool ___active, MeshRenderer ___mirrorMesh)
			{
				if (!___active || !___mirrorMesh.isVisible)
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