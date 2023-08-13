
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace MySRP
{
    public class MyPipeline : RenderPipeline



    {

        private ShadowCaster _shadowPass = new ShadowCaster();
        CullingResults cullingResults;
        private LightConfigurator _lightConfigurator = new LightConfigurator();
        bool dynamicBatchingFlag;
        bool instance;
        ShadowSetting _shadowSetting;
        private RenderObjectPass _opauquePass = new RenderObjectPass(false);

        public MyPipeline(MyPipelineAsset settings)
        {
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.useScriptableRenderPipelineBatching = settings.useSRPBacth;
            dynamicBatchingFlag = settings.dynamicBatching;
            instance = settings.instanceFlag;
            _shadowSetting = settings._shadowSetting;
        }


        void DrawOpaque(ScriptableRenderContext context, Camera camera)
        {


            context.SetupCameraProperties(camera);
            ShaderTagId shaderTagId = new ShaderTagId("MyForwardBase");
            var sortSettings = new SortingSettings(camera);
            sortSettings.criteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortSettings)
            {
                enableDynamicBatching = dynamicBatchingFlag,
                enableInstancing = instance,                
            };
            drawingSettings.perObjectData |= PerObjectData.LightData;
            drawingSettings.perObjectData |= PerObjectData.LightIndices;
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            filteringSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }
        void DrawTransparent(ScriptableRenderContext context, Camera camera)
        {
            context.SetupCameraProperties(camera);
            ShaderTagId shaderTagId = new ShaderTagId("MyForwardBase");
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            var sortSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortSettings)
            {
                enableDynamicBatching = dynamicBatchingFlag,
                enableInstancing = instance
            };
            drawingSettings.sortingSettings = sortSettings;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        void Render(ScriptableRenderContext context, Camera camera)

        {

            camera.TryGetCullingParameters(out var cullingParameters);
            cullingParameters.shadowDistance = Mathf.Min(100, camera.farClipPlane - camera.nearClipPlane);
            cullingResults = context.Cull(ref cullingParameters);

          
            LightData lightData = _lightConfigurator.SetupShaderLightingParams(context, ref cullingResults);

            ShadowCasterSetting shadowCasterSetting = new ShadowCasterSetting();
            shadowCasterSetting.lightData = lightData;
            shadowCasterSetting.shadowSetting = _shadowSetting;
            shadowCasterSetting.cullingResults = cullingResults;
            _shadowPass.Render(context, camera,ref shadowCasterSetting);
            _opauquePass.Render(context, camera, ref cullingResults);
            //DrawOpaque(context, camera);

            context.DrawSkybox(camera);


            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }


           

        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                Render(context, camera);
            }
            context.Submit();
        }
    }
}

