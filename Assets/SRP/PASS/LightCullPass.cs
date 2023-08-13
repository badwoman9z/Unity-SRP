using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP
{


    public class LightCullPass 
    {
        struct ClusterBox
        {
            public Vector3 p0, p1, p2, p3, p4, p5, p6, p7;

        };
        static int SIZE_OF_CLUSTETBOX = 8 * 3 * 4;


        struct PointLight
        {
            public Vector3 color;
            public float intensity;
            public Vector3 position;
            public float radius;
        };
        static int SIZE_OF_LIGHT = 32;


        struct LightIndex
        {
            public int count;
            public int start;
        };
        static int SIZE_OF_INDEX = sizeof(int) * 2;


        public static int numClusterX = 16;
        public static int numClusterY = 16;
        public static int numClusterZ = 16;
        public static int maxNumLights = 1024;
        public static int maxNumLightsPerCluster = 128;


        ComputeShader clusterGenerateCS;
        ComputeShader lightAssignCS;

        public ComputeBuffer clusterBuffer;     // 簇列表
        public ComputeBuffer lightBuffer;       // 光源列表
        public ComputeBuffer lightAssignBuffer; // 光源分配结果
        public ComputeBuffer assignTable;       // 光源分配索引表


        public LightCullPass()
        {
            int numClusters = numClusterX * numClusterY * numClusterZ;

            //初始化buffer
            lightBuffer = new ComputeBuffer(maxNumLights, SIZE_OF_LIGHT);
            clusterBuffer = new ComputeBuffer(numClusters, SIZE_OF_CLUSTETBOX);
            lightAssignBuffer = new ComputeBuffer(numClusters * maxNumLightsPerCluster, sizeof(uint));
            assignTable = new ComputeBuffer(numClusters, SIZE_OF_INDEX);

            //加载计算着色器
            clusterGenerateCS = Resources.Load<ComputeShader>("Shaders/ClusterGenerate");
            lightAssignCS = Resources.Load<ComputeShader>("Shaders/LightAssign");
        }
        public void UpdateLightBuffer(Light[] lights)
        {
            PointLight[] plihgts = new PointLight[maxNumLights];
            int cnt = 0;
            for(int i = 0; i < lights.Length; i++)
            {
                PointLight p ;
                if (lights[i].type == LightType.Point)
                {
                    p.position = lights[i].transform.position;
                    p.intensity = lights[i].intensity;
                    p.color = new Vector3(lights[i].color.r, lights[i].color.g, lights[i].color.b);
                    p.radius = lights[i].range;
                    plihgts[cnt++] = p;
                }

            }
            lightBuffer.SetData(plihgts);
            lightAssignCS.SetInt("_numLights", cnt);
        }
        public void UpdateLightBuffer(VisibleLight[] lights)
        {
            PointLight[] plights = new PointLight[maxNumLights];
            int cnt = 0;

            for (int i = 0; i < lights.Length; i++)
            {
                var vl = lights[i].light;
                if (vl.type != LightType.Point) continue;

                PointLight pl;
                pl.color = new Vector3(vl.color.r, vl.color.g, vl.color.b);
                pl.intensity = vl.intensity;
                pl.position = vl.transform.position;
                pl.radius = vl.range;

                plights[cnt++] = pl;
            }
            lightBuffer.SetData(plights);

            // 传递光源数量
            lightAssignCS.SetInt("_numLights", cnt);
        }

        // 为每一个 cluster 分配光源
        public void LightAssign()
        {
            lightAssignCS.SetInt("_maxNumLightsPerCluster", maxNumLightsPerCluster);
            lightAssignCS.SetFloat("_numClusterX", numClusterX);
            lightAssignCS.SetFloat("_numClusterY", numClusterY);
            lightAssignCS.SetFloat("_numClusterZ", numClusterZ);

            var kid = lightAssignCS.FindKernel("LightAssign");
            lightAssignCS.SetBuffer(kid, "_clusterBuffer", clusterBuffer);
            lightAssignCS.SetBuffer(kid, "_lightBuffer", lightBuffer);
            lightAssignCS.SetBuffer(kid, "_lightAssignBuffer", lightAssignBuffer);
            lightAssignCS.SetBuffer(kid, "_assignTable", assignTable);

            lightAssignCS.Dispatch(kid, numClusterZ, 1, 1);
        }

        public void GenerateCluster(Camera camera)
        {
            //设置参数

            Matrix4x4 viewMat = camera.worldToCameraMatrix;
            Matrix4x4 viewMatInv = viewMat.inverse;

            Matrix4x4 vpMat = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * viewMat;
            Matrix4x4 vpMatInverse = vpMat.inverse;

            clusterGenerateCS.SetMatrix("_viewMatrix", viewMat);
            clusterGenerateCS.SetMatrix("_viewMatrixInv", viewMatInv);
            clusterGenerateCS.SetMatrix("_vpMatrix", vpMat);
            clusterGenerateCS.SetMatrix("_vpMatrixInv", vpMatInverse);
            clusterGenerateCS.SetFloat("_near", camera.nearClipPlane);
            clusterGenerateCS.SetFloat("_far", camera.farClipPlane);
            clusterGenerateCS.SetFloat("_fovh", camera.fieldOfView);
            clusterGenerateCS.SetFloat("_numClusterX", numClusterX);
            clusterGenerateCS.SetFloat("_numClusterY", numClusterY);
            clusterGenerateCS.SetFloat("_numClusterZ", numClusterZ);

            var kid = clusterGenerateCS.FindKernel("ClusterGenerate");
            clusterGenerateCS.SetBuffer(kid, "_clusterBuffer", clusterBuffer);
            clusterGenerateCS.Dispatch(kid, numClusterZ, 1, 1);
        }


        // 向光照 shader 传递变量
        public void SetShaderParameters()
        {
            Shader.SetGlobalFloat("_numClusterX", numClusterX);
            Shader.SetGlobalFloat("_numClusterY", numClusterY);
            Shader.SetGlobalFloat("_numClusterZ", numClusterZ);

            Shader.SetGlobalBuffer("_lightBuffer", lightBuffer);
            Shader.SetGlobalBuffer("_lightAssignBuffer", lightAssignBuffer);
            Shader.SetGlobalBuffer("_assignTable", assignTable);
        }

        public void Render(ref CullingResults cullingResults, Camera camera)
        {
            this.GenerateCluster(camera);

            this.UpdateLightBuffer(cullingResults.visibleLights.ToArray());

            this.LightAssign();

            this.SetShaderParameters();
        }

        void DrawBox(ClusterBox box, Color color)
        {
            Debug.DrawLine(box.p0, box.p1, color);
            Debug.DrawLine(box.p0, box.p2, color);
            Debug.DrawLine(box.p0, box.p4, color);

            Debug.DrawLine(box.p6, box.p2, color);
            Debug.DrawLine(box.p6, box.p7, color);
            Debug.DrawLine(box.p6, box.p4, color);

            Debug.DrawLine(box.p5, box.p1, color);
            Debug.DrawLine(box.p5, box.p7, color);
            Debug.DrawLine(box.p5, box.p4, color);

            Debug.DrawLine(box.p3, box.p1, color);
            Debug.DrawLine(box.p3, box.p2, color);
            Debug.DrawLine(box.p3, box.p7, color);
        }
        public void DegbugCluster()
        {
            ClusterBox[] boxes = new ClusterBox[numClusterX * numClusterY * numClusterZ];
            clusterBuffer.GetData(boxes, 0, 0, numClusterX * numClusterY * numClusterZ);
            foreach(var box in boxes)
            {
                DrawBox(box, Color.gray);
            }
        }
        public void DebugLightAssign()
        {
            int numClusters = numClusterX * numClusterY * numClusterZ;

            ClusterBox[] boxes = new ClusterBox[numClusters];
            clusterBuffer.GetData(boxes, 0, 0, numClusters);

            LightIndex[] indices = new LightIndex[numClusters];
            assignTable.GetData(indices, 0, 0, numClusters);

            uint[] assignBuf = new uint[numClusters * maxNumLightsPerCluster];
            lightAssignBuffer.GetData(assignBuf, 0, 0, numClusters * maxNumLightsPerCluster);

            Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow };

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i].count > 0)
                {
                    uint firstLightId = assignBuf[indices[i].start];
                    DrawBox(boxes[i], colors[firstLightId % 4]);
                    //Debug.Log(assignBuf[indices[i].start]);   // log light id
                }

            }
        }
    }
}


