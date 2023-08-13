using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MySRP
{
    [CreateAssetMenu(menuName ="RenderPipeline/InstanceData")]
    [System.Serializable]
    public class InstanceData : ScriptableObject
    {
         public Matrix4x4[] mats;

        [HideInInspector] public ComputeBuffer matrixBuffer;
        [HideInInspector] public ComputeBuffer validMatrixBuffer;
        [HideInInspector] public ComputeBuffer argsBuffer;

        [HideInInspector] public int subMeshIndex = 0;
        [HideInInspector] public int instanceCount;

        public Mesh instanceMesh;
        public Material instanceMaterail;

        public Vector3 center = new Vector3(0, 0, 0);
        public int randomInstanceNum = 5;
        public float distanceMin = 5.0f;
        public float distanceMax = 50.0f;
        public float heightMin = -0.5f;
        public float heightMax = 0.5f;
        // 随机生成
        public void GenerateRandomData()
        {
            instanceCount = randomInstanceNum;

            // 生成变换矩阵
            mats = new Matrix4x4[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
                float distance = Mathf.Sqrt(Random.Range(0.0f, 1.0f)) * (distanceMax - distanceMin) + distanceMin;
                float height = Random.Range(heightMin, heightMax);

                Vector3 pos = new Vector3(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance);
                Vector3 dir = pos - center;

                Quaternion q = new Quaternion();
                q.SetLookRotation(dir, new Vector3(0, 1, 0));

                Matrix4x4 m = Matrix4x4.Rotate(q);
                m.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));

                mats[i] = m;
            }

            if (matrixBuffer != null)
            {
                matrixBuffer.Release(); matrixBuffer = null;
            }
            if (validMatrixBuffer != null)
            {
                validMatrixBuffer.Release(); validMatrixBuffer = null;
            }
            if (argsBuffer != null)
            {
                argsBuffer.Release(); argsBuffer = null;
            }
            

            Debug.Log("Instance Data Generate Success");
        }
        public void CheckAndInitBuffer()
        {
            if (matrixBuffer != null && validMatrixBuffer != null && argsBuffer != null)
            {
                return;
            }

            int sizeofMatrix4x4 = 4 * 4 * 4;
            matrixBuffer = new ComputeBuffer(instanceCount, sizeofMatrix4x4);

            validMatrixBuffer = new ComputeBuffer(instanceCount, sizeofMatrix4x4, ComputeBufferType.Append);

            argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);


        }

    }
}


