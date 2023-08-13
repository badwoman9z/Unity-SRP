using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MySRP
{
    public static class ShaderKeywords
    {

        public const string ShadowPCF = "X_SHADOW_PCF";
        public const string ShadowBiasCasterVertex = "X_SHADOW_BIAS_CASTER_VERTEX";
        public const string ShadowBiasReceiverPixel = "X_SHADOW_BIAS_RECEIVER_PIXEL";

    }
    public class Utils
    {
              
        public static void SetGlobalShaderKeyWord(CommandBuffer commandBuffer,string keyword,bool enable)
        {
            if (enable)
            {
                commandBuffer.EnableShaderKeyword(keyword);
            }
            else
            {
                commandBuffer.DisableShaderKeyword(keyword);
            }
        }
        public static bool IsPCFEnable(ShadowPCFType shadowPCF)
        {
            switch (shadowPCF)
            {
                case ShadowPCFType.None:
                return false;
                case ShadowPCFType.PCF1:
                case ShadowPCFType.PCF3:
                case ShadowPCFType.PCF3Fast:
                case ShadowPCFType.PCF5:
                return true;
            }
            return false;
        }
        public static Mesh CreateFullscreenMesh()
        {
            
            
            Vector3[] positions =
            {
                new Vector3(-1.0f,  -1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
            };
            Vector2[] uvs = {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(0,1),
                new Vector2(1,1),
            };
            int[] indices = { 0, 2, 1, 1, 2, 3 };
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;
            mesh.uv = uvs;
            return mesh;
        }
    }
}

