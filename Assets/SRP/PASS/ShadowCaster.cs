using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP
{
    public struct ShadowCasterSetting
    {

        public ShadowSetting shadowSetting;
        public CullingResults cullingResults;
        public LightData lightData;
    }

    public class ShadowCaster
    {

        private ShadowMapTextureHandler _shadowMapHandler = new ShadowMapTextureHandler();
        private CommandBuffer _commandBuffer = new CommandBuffer();
        private Matrix4x4[] _worldToCascadeShadowMapMatrices = new Matrix4x4[4];
        private Vector4[] _cascadeCullingSpheres = new Vector4[4];
        public ShadowCaster()
        {
            _commandBuffer.name = "ShadowCaster";
        }
        private static int GetShadowMapResolution(Light light)
        {
            switch (light.shadowResolution)
            {
                case LightShadowResolution.VeryHigh:
                    return 2048;
                case LightShadowResolution.High:
                    return 1024;
                case LightShadowResolution.Medium:
                    return 512;
                case LightShadowResolution.Low:
                    return 256;
            }
            return 256;
        }
        static Matrix4x4 GetWorldToCascadeShadowMapSpaceMatrix(Matrix4x4 proj, Matrix4x4 view, Vector4 cascadeOffsetAndScale)
        {
            //检查平台是否zBuffer反转,一般情况下，z轴方向是朝屏幕内，即近小远大。但是在zBuffer反转的情况下，z轴是朝屏幕外，即近大远小。
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            // xyz = xyz * 0.5 + 0.5. 
            // 即将xy从(-1,1)映射到(0,1),z从(-1,1)或(1,-1)映射到(0,1)或(1,0)
            var textureScaleAndBias = Matrix4x4.identity;
            //x = x * 0.5 + 0.5
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;

            //y = y * 0.5 + 0.5
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;

            //z = z * 0.5 = 0.5
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;

            //再将uv映射到cascadeShadowMap的空间
            var cascadeOffsetAndScaleMatrix = Matrix4x4.identity;

            //x = x * cascadeOffsetAndScale.z + cascadeOffsetAndScale.x
            cascadeOffsetAndScaleMatrix.m00 = cascadeOffsetAndScale.z;
            cascadeOffsetAndScaleMatrix.m03 = cascadeOffsetAndScale.x;

            //y = y * cascadeOffsetAndScale.w + cascadeOffsetAndScale.y
            cascadeOffsetAndScaleMatrix.m11 = cascadeOffsetAndScale.w;
            cascadeOffsetAndScaleMatrix.m13 = cascadeOffsetAndScale.y;

            return cascadeOffsetAndScaleMatrix * textureScaleAndBias * worldToShadow;
        }
        private void ClearAndActiveShadowMapTexture(ScriptableRenderContext context, int shadowMapResolution)
        {
            _commandBuffer.Clear();
            //设置渲染目标
            _commandBuffer.SetRenderTarget(_shadowMapHandler._renderTargetIdentifier, _shadowMapHandler._renderTargetIdentifier);

            _commandBuffer.SetViewport(new Rect(0, 0, shadowMapResolution, shadowMapResolution));
            //Clear贴图
            _commandBuffer.ClearRenderTarget(true, true, Color.black, 1);

            context.ExecuteCommandBuffer(_commandBuffer);
        }
        private void SetupShadowCascade(ScriptableRenderContext context, Vector2 offsetInAtlas, int resolution, ref Matrix4x4 matrixView, ref Matrix4x4 matrixProj)
        {
            _commandBuffer.Clear();
            _commandBuffer.SetViewport(new Rect(offsetInAtlas.x, offsetInAtlas.y, resolution, resolution));
            //设置view&proj矩阵
            _commandBuffer.SetViewProjectionMatrices(matrixView, matrixProj);
            context.ExecuteCommandBuffer(_commandBuffer);
        }
        private void ConfigShadowPCF(CommandBuffer commandBuffer,ShadowSetting shadowSetting)
        {
            bool isPCFEnabled = Utils.IsPCFEnable(shadowSetting._shadowPCFType);
            Utils.SetGlobalShaderKeyWord(commandBuffer, ShaderKeywords.ShadowPCF, isPCFEnabled);
            //Debug.Log(isPCFEnabled);
            if (isPCFEnabled)
            {
                //Debug.Log("Hello World");
                Shader.SetGlobalVector(ShaderProperties.ShdowPCFParams, new Vector4((int)shadowSetting._shadowPCFType, 0, 0, 0));
            }

        }
        private void ConfigShadowSetting(ScriptableRenderContext context,ref ShadowSetting shadowSetting)
        {
            _commandBuffer.Clear();
            this.ConfigShadowPCF(_commandBuffer, shadowSetting);
            context.ExecuteCommandBuffer(_commandBuffer);
        }
        public void Render(ScriptableRenderContext context, Camera camera, ref ShadowCasterSetting shadowCasterSetting)
        {
            ref var lightData = ref shadowCasterSetting.lightData;
            ref var cullingResults = ref shadowCasterSetting.cullingResults;
            var shadowSetting = shadowCasterSetting.shadowSetting;
            if (!lightData.HasMainLight())
            {
                Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(0,0,0,0));
                return;
            }
            if (!cullingResults.GetShadowCasterBounds(lightData.mainLightIndex, out var lightBounds))
            {
                Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(0, 0, 0, 0));
                return;
            }
            this.ConfigShadowSetting(context, ref shadowSetting);
            var lightComponent = lightData.mainLight.light;
            var shadowMapRes = GetShadowMapResolution(lightComponent);
            _shadowMapHandler.AcquireRenderTexuterIfNot(shadowMapRes);
            this.ClearAndActiveShadowMapTexture(context, shadowMapRes);

            //计算级联阴影
            var cascadeAtlasGridSize = Mathf.CeilToInt(Mathf.Sqrt(shadowSetting.cascadeCount));
            var cascadeResolution = shadowMapRes / cascadeAtlasGridSize;
            for (int i = 0; i < shadowSetting.cascadeCount; i++)
            {

                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
                                                            (lightData.mainLightIndex,
                                                            i, shadowSetting.cascadeCount, shadowSetting.cascadeRatio,
                                                            cascadeResolution,
                                                            lightComponent.shadowNearPlane,
                                                            out var viewMat,
                                                            out var projMat,
                                                            out var shadowSplitData
                                                            );

                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, lightData.mainLightIndex)
                {
                    splitData = shadowSplitData
                };
                var x = i % cascadeAtlasGridSize;
                var y = i / cascadeAtlasGridSize;

                //计算当前级别的级联阴影在Atlas上的偏移位置
                var offsetInAtlas = new Vector2(x * cascadeResolution, y * cascadeResolution);
                //设置Cascade相关参数
                SetupShadowCascade(context, offsetInAtlas, cascadeResolution, ref viewMat, ref projMat);
                context.DrawShadows(ref shadowDrawingSettings);
                //计算Cascade ShadowMap空间投影矩阵和包围圆
                var cascadeOffsetAndScale = new Vector4(offsetInAtlas.x, offsetInAtlas.y, cascadeResolution, cascadeResolution) / shadowMapRes;
                var matrixWorldToShadowMapSpace = GetWorldToCascadeShadowMapSpaceMatrix(projMat, viewMat, cascadeOffsetAndScale);
                _worldToCascadeShadowMapMatrices[i] = matrixWorldToShadowMapSpace;
                _cascadeCullingSpheres[i] = shadowSplitData.cullingSphere;
            }


          
            //绘制阴影所需要的参数
            Shader.SetGlobalTexture(ShaderProperties.MainShadowMap, _shadowMapHandler._shadowMapTexture);
            Shader.SetGlobalMatrixArray(ShaderProperties.WorldToMainLightCascadeShadowMapSpaceMatrices, _worldToCascadeShadowMapMatrices);
            Shader.SetGlobalVectorArray(ShaderProperties.CascadeCullingSpheres, _cascadeCullingSpheres);
            Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(lightComponent.shadowBias,
                                                                             lightComponent.shadowNormalBias,
                                                                             lightComponent.shadowStrength, shadowSetting.cascadeCount));

            Shader.SetGlobalVector(ShaderProperties.ShadowMapSize, new Vector4(1.0f / shadowMapRes, 1.0f / shadowMapRes, shadowMapRes, shadowMapRes));




        }
        public static class ShaderProperties
        {

            public static readonly int MainLightMatrixWorldToShadowSpace = Shader.PropertyToID("_XMainLightMatrixWorldToShadowMap");

            //
            /// <summary>
            /// 类型Matrix4x4[4]，表示每级Cascade从世界到贴图空间的转换矩阵
            /// </summary>
            public static readonly int WorldToMainLightCascadeShadowMapSpaceMatrices = Shader.PropertyToID("_XWorldToMainLightCascadeShadowMapSpaceMatrices");

            /// <summary>
            /// 类型Vector4[4],表示每级Cascade的空间裁剪包围球
            /// </summary>
            public static readonly int CascadeCullingSpheres = Shader.PropertyToID("_XCascadeCullingSpheres");

            //x为depthBias,y为normalBias,z为shadowStrength,w为当前的cascade shadow数量
            public static readonly int ShadowParams = Shader.PropertyToID("_ShadowParams");
            public static readonly int MainShadowMap = Shader.PropertyToID("_XMainShadowMap");
            public static readonly int ShdowPCFParams = Shader.PropertyToID("_ShadowPCFParams");
            public static readonly int ShadowMapSize = Shader.PropertyToID("_ShadowMapSize");
        }
        public class ShadowMapTextureHandler
        {
            public RenderTargetIdentifier _renderTargetIdentifier;
            public RenderTexture _shadowMapTexture;
            public void AcquireRenderTexuterIfNot(int resolution)
            {
                if (_shadowMapTexture && _shadowMapTexture.width != resolution)
                {
                    RenderTexture.ReleaseTemporary(_shadowMapTexture);
                    _shadowMapTexture = null;
                }
                if (!_shadowMapTexture)
                {
                    _shadowMapTexture = RenderTexture.GetTemporary(resolution, resolution, 32, RenderTextureFormat.Shadowmap);
                    _renderTargetIdentifier = new RenderTargetIdentifier(_shadowMapTexture);
                }
            }
        }
    }
}

