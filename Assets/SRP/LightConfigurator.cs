using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP {
    struct DirectionOrPointLight
    {
        public Vector4 color;
        public Vector4 directionOrPosition;
        
    };
    public struct LightData
    {
        public int mainLightIndex;
        public VisibleLight mainLight;

        public bool HasMainLight()
        {
            return mainLightIndex >= 0;
        }
    }
    public class LightConfigurator
    {
        ComputeBuffer lightBuffer;
        const int maxVisibleLights = 32;
        DirectionOrPointLight[] lights = new DirectionOrPointLight[maxVisibleLights];

        public LightConfigurator()
        {
            lightBuffer = new ComputeBuffer(32, 8 * 4);
        }

        public class LightShaderProperties {
            public static int visibleLightId = Shader.PropertyToID("lights");
            public static int lightCount = Shader.PropertyToID("_lightCount");
            public static int mainLightIndex = Shader.PropertyToID("_mainLightIndex");

        }

        private static int CompareLightRenderMode(LightRenderMode m1, LightRenderMode m2)
        {
            if (m1 == m2)
            {
                return 0;
            }
            if (m1 == LightRenderMode.ForcePixel)
            {
                return -1;
            }
            if (m2 == LightRenderMode.ForcePixel)
            {
                return 1;
            }
            if (m1 == LightRenderMode.Auto)
            {
                return -1;
            }
            if (m2 == LightRenderMode.Auto)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// 如果有多个平行光，按LightRenderMode、intensity对其排序
        /// </summary>
        private static int CompareLight(Light l1, Light l2)
        {
            if (l1.renderMode == l2.renderMode)
            {
                return (int)Mathf.Sign(l2.intensity - l1.intensity);
            }
            var ret = CompareLightRenderMode(l1.renderMode, l2.renderMode);
            if (ret == 0)
            {
                ret = (int)Mathf.Sign(l2.intensity - l1.intensity);
            }
            return ret;
        }

        private static int GetMainLightIndex(NativeArray<VisibleLight> lights)
        {
            Light mainLight = null;
            var mainLightIndex = -1;
            var index = 0;
            foreach (var light in lights)
            {
                if (light.lightType == LightType.Directional)
                {
                    var lightComp = light.light;
                    if (lightComp.renderMode == LightRenderMode.ForceVertex)
                    {
                        continue;
                    }
                    if (!mainLight)
                    {
                        mainLight = lightComp;
                        mainLightIndex = index;
                    }
                    else
                    {
                        if (CompareLight(mainLight, lightComp) > 0)
                        {
                            mainLight = lightComp;
                            mainLightIndex = index;
                        }
                    }
                }
                index++;
            }
            return mainLightIndex;
        }
        public LightData SetupShaderLightingParams(ScriptableRenderContext context, ref CullingResults cullingResults)
        {
            var visibleLights = cullingResults.visibleLights;

            var mainLightIndex = GetMainLightIndex(visibleLights);


            var lightMapIndex = cullingResults.GetLightIndexMap(Allocator.Temp);
            for (int i = 0; i < visibleLights.Length; i++)
            {
                if (i == maxVisibleLights)
                {
                    break;
                }
                switch (visibleLights[i].lightType)
                {
                    case LightType.Directional:
                        lightMapIndex[i] = -1;
                        lights[i].color = new Vector4(cullingResults.visibleLights[i].light.color.r,
                                            cullingResults.visibleLights[i].light.color.g,
                                            cullingResults.visibleLights[i].light.color.b,
                                            cullingResults.visibleLights[i].light.intensity);
                        Vector4 v = cullingResults.visibleLights[i].localToWorldMatrix.GetColumn(2);
                        v.x = -v.x;
                        v.y = -v.y;
                        v.z = -v.z;
                        lights[i].directionOrPosition = v;
                        break;
                    case LightType.Point:
                        lightMapIndex[i] = i;
                        lights[i].color = cullingResults.visibleLights[i].finalColor;
                        Vector4 positionAndRange = visibleLights[i].light.gameObject.transform.position;
                        positionAndRange.w = visibleLights[i].range;
                        lights[i].directionOrPosition = positionAndRange;
                        break;
                    default:
                        lightMapIndex[i] = -1;
                        break;
                }


            }
            lightBuffer.SetData(lights);
            Shader.SetGlobalBuffer(LightShaderProperties.visibleLightId, lightBuffer);
            Shader.SetGlobalInt(LightShaderProperties.lightCount, cullingResults.visibleLights.Length);
            Shader.SetGlobalInt(LightShaderProperties.mainLightIndex, mainLightIndex);
            return new LightData()
            {
                mainLightIndex = mainLightIndex,
                mainLight = mainLightIndex >= 0 && mainLightIndex < visibleLights.Length ? visibleLights[mainLightIndex] : default(VisibleLight),
            };
        }

    }


}


