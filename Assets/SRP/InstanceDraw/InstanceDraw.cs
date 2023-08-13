using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MySRP
{
    public class InstanceDraw 
    {
        private static CommandBuffer _commandBuffer;
        public static void CheckAndInit(InstanceData instanceData)
            
        {
            instanceData.CheckAndInitBuffer();

            //传入矩阵数据

            instanceData.matrixBuffer.SetData(instanceData.mats);

            //初始化绘制参数

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            if (instanceData.instanceMesh != null)
            {
                args[0] = (uint)instanceData.instanceMesh.GetIndexCount(instanceData.subMeshIndex);
                args[1] = (uint)0;
                args[2] = (uint)instanceData.instanceMesh.GetIndexStart(instanceData.subMeshIndex);
                args[3] = (uint)instanceData.instanceMesh.GetBaseVertex(instanceData.subMeshIndex);
            }
            instanceData.argsBuffer.SetData(args);
        }
        public static Vector4[] BoundToPoint(Bounds b)
        {
            Vector4[] boundingBox = new Vector4[8];
            boundingBox[0] = new Vector4(b.min.x, b.min.y, b.min.z, 1);
            boundingBox[1] = new Vector4(b.max.x, b.max.y, b.max.z, 1);
            boundingBox[2] = new Vector4(boundingBox[0].x, boundingBox[0].y, boundingBox[1].z, 1);
            boundingBox[3] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[0].z, 1);
            boundingBox[4] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[0].z, 1);
            boundingBox[5] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[1].z, 1);
            boundingBox[6] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[1].z, 1);
            boundingBox[7] = new Vector4(boundingBox[1].x, boundingBox[1].y, boundingBox[0].z, 1);
            return boundingBox;
        }
        /*
         不进行剔除的绘制
         */
        public static void Draw(InstanceData instanceData,ScriptableRenderContext context)
        {
            if (instanceData == null)
            {
                return;
            }
            if (instanceData.mats.Length == 0)
            {
                return;
            }
            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer()
                {
                    name = "InstanceDraw"
                };
            }

            CheckAndInit(instanceData);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

            instanceData.argsBuffer.GetData(args);

            args[1] = (uint)instanceData.instanceCount;

            instanceData.argsBuffer.SetData(args);

            instanceData.instanceMaterail.SetBuffer("_validMatrixBuffer", instanceData.matrixBuffer);

            _commandBuffer.DrawMeshInstancedIndirect(instanceData.instanceMesh,
                                               instanceData.subMeshIndex,
                                               instanceData.instanceMaterail,
                                                -1,
                                               instanceData.argsBuffer);
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
        }
        /*
         * 视锥剔除后
         */
        public static void Draw(InstanceData instanceData,ScriptableRenderContext context,ComputeShader cs)
        {
            if (instanceData == null)
            {
                return;
            }
            if (instanceData.mats.Length == 0)
            {
                return;
            }
            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer()
                {
                    name = "InstanceDraw"
                };
            }

            CheckAndInit(instanceData);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

            instanceData.argsBuffer.GetData(args);

            args[1] = (uint)instanceData.instanceCount;

            instanceData.argsBuffer.SetData(args);

            //将 valid buffer 的计数设为0

            instanceData.validMatrixBuffer.SetCounterValue(0);
            /*
                传入视锥的6个平面和物体的AABB
             */
            //是主相机
            Plane[] frustrums = GeometryUtility.CalculateFrustumPlanes(Camera.main);

            Vector4[] planes = new Vector4[6];

            for(int i = 0; i < 6; i++)
            {
                planes[i] = new Vector4(frustrums[i].normal.x, frustrums[i].normal.y, frustrums[i].normal.z, frustrums[i].distance);
            }

            Vector4[] bounds = BoundToPoint(instanceData.instanceMesh.bounds);

            //传参到计算着色器

            int kid = cs.FindKernel("CSMain");

            cs.SetVectorArray("_bounds", bounds);
            cs.SetVectorArray("_planes", planes);
            cs.SetInt("_instanceCount", instanceData.instanceCount);
            cs.SetBuffer(kid, "_matrixBuffer", instanceData.matrixBuffer);
            cs.SetBuffer(kid, "_validMatrixBuffer", instanceData.validMatrixBuffer);
            cs.SetBuffer(kid, "_argsBuffer", instanceData.argsBuffer);

            int nDispatch = (instanceData.instanceCount / 128) + 1;

            cs.Dispatch(kid, nDispatch, 1, 1);

            instanceData.instanceMaterail.SetBuffer("_validMatrixBuffer", instanceData.validMatrixBuffer);

            _commandBuffer.DrawMeshInstancedIndirect(instanceData.instanceMesh,
                                   instanceData.subMeshIndex,
                                   instanceData.instanceMaterail,
                                    -1,
                                   instanceData.argsBuffer);
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
            //instanceData.validMatrixBuffer.SetCounterValue(0);

            uint[] debugargs = new uint[5] { 0, 0, 0, 0, 0 };





        }
    }
}


