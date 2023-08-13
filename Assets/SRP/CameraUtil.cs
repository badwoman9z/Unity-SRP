using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP
{
    internal class CameraUtil
    {
        public static void ConfigShaderProperties(CommandBuffer commandBuffer, Camera camera)
        {
            commandBuffer.SetGlobalVector(CameraShaderProperties.WorldSpaceCameraForward, camera.transform.forward);
            var viewMatrix = camera.worldToCameraMatrix;
            //��֪��Ϊʲô���ڶ���������false���������õ���������
            var projectMatrixDirect = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            var projectMatrixRT = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            var matrixVPDirect = projectMatrixDirect * viewMatrix;
            var matrixVPRT = projectMatrixRT * viewMatrix;
            var invMatrixVPDirect = matrixVPDirect.inverse;

            var matrixVPNormalInv = matrixVPRT.inverse;
            //Debug.Log(invMatrixVPDirect);
            commandBuffer.SetGlobalMatrix(CameraShaderProperties.CameraMatrixVPInv, invMatrixVPDirect);
            commandBuffer.SetGlobalMatrix(CameraShaderProperties.CameraMatrixV, viewMatrix);
            commandBuffer.SetGlobalMatrix(CameraShaderProperties.CameraMatrixV_Unity, camera.transform.worldToLocalMatrix);
            
            //commandBuffer.Clear();

        }

        public static void SortCameras(Camera[] cameras)
        {
            for (var i = 0; i < cameras.Length; i++)
            {
                var c = cameras[i];
                if (c.cameraType == CameraType.SceneView)
                {
                    var temp = cameras[cameras.Length - 1];
                    cameras[cameras.Length - 1] = c;
                    cameras[i] = temp;
                    return;
                }
            }
        }
    }

    public static class CameraShaderProperties
    {
        public static readonly int WorldSpaceCameraForward = Shader.PropertyToID("_WorldSpaceCameraForward");
        public static readonly int CameraMatrixVPInv = Shader.PropertyToID("_CameraMatrixVPInv");
        public static readonly int CameraMatrixV = Shader.PropertyToID("_CameraMatrixV");

        /// <summary>
        /// ��_CameraMatrixV�������ڣ�ǰ��ȡ��camera.worldToCameraMatrix����������ȡ��camera.transform.worldToLocal
        /// </summary>
        public static readonly int CameraMatrixV_Unity = Shader.PropertyToID("_CameraMatrixV_Unity");
    }
}