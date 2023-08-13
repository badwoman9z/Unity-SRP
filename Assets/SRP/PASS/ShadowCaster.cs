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
            //���ƽ̨�Ƿ�zBuffer��ת,һ������£�z�᷽���ǳ���Ļ�ڣ�����СԶ�󡣵�����zBuffer��ת������£�z���ǳ���Ļ�⣬������ԶС��
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * view;

            // xyz = xyz * 0.5 + 0.5. 
            // ����xy��(-1,1)ӳ�䵽(0,1),z��(-1,1)��(1,-1)ӳ�䵽(0,1)��(1,0)
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

            //�ٽ�uvӳ�䵽cascadeShadowMap�Ŀռ�
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
            //������ȾĿ��
            _commandBuffer.SetRenderTarget(_shadowMapHandler._renderTargetIdentifier, _shadowMapHandler._renderTargetIdentifier);

            _commandBuffer.SetViewport(new Rect(0, 0, shadowMapResolution, shadowMapResolution));
            //Clear��ͼ
            _commandBuffer.ClearRenderTarget(true, true, Color.black, 1);

            context.ExecuteCommandBuffer(_commandBuffer);
        }
        private void SetupShadowCascade(ScriptableRenderContext context, Vector2 offsetInAtlas, int resolution, ref Matrix4x4 matrixView, ref Matrix4x4 matrixProj)
        {
            _commandBuffer.Clear();
            _commandBuffer.SetViewport(new Rect(offsetInAtlas.x, offsetInAtlas.y, resolution, resolution));
            //����view&proj����
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

            //���㼶����Ӱ
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

                //���㵱ǰ����ļ�����Ӱ��Atlas�ϵ�ƫ��λ��
                var offsetInAtlas = new Vector2(x * cascadeResolution, y * cascadeResolution);
                //����Cascade��ز���
                SetupShadowCascade(context, offsetInAtlas, cascadeResolution, ref viewMat, ref projMat);
                context.DrawShadows(ref shadowDrawingSettings);
                //����Cascade ShadowMap�ռ�ͶӰ����Ͱ�ΧԲ
                var cascadeOffsetAndScale = new Vector4(offsetInAtlas.x, offsetInAtlas.y, cascadeResolution, cascadeResolution) / shadowMapRes;
                var matrixWorldToShadowMapSpace = GetWorldToCascadeShadowMapSpaceMatrix(projMat, viewMat, cascadeOffsetAndScale);
                _worldToCascadeShadowMapMatrices[i] = matrixWorldToShadowMapSpace;
                _cascadeCullingSpheres[i] = shadowSplitData.cullingSphere;
            }


          
            //������Ӱ����Ҫ�Ĳ���
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
            /// ����Matrix4x4[4]����ʾÿ��Cascade�����絽��ͼ�ռ��ת������
            /// </summary>
            public static readonly int WorldToMainLightCascadeShadowMapSpaceMatrices = Shader.PropertyToID("_XWorldToMainLightCascadeShadowMapSpaceMatrices");

            /// <summary>
            /// ����Vector4[4],��ʾÿ��Cascade�Ŀռ�ü���Χ��
            /// </summary>
            public static readonly int CascadeCullingSpheres = Shader.PropertyToID("_XCascadeCullingSpheres");

            //xΪdepthBias,yΪnormalBias,zΪshadowStrength,wΪ��ǰ��cascade shadow����
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

