
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MySRP {

    public class ForWardRP : RenderPipeline
    {
        CullingResults cullingResults;
        //µ∆π‚≈‰÷√
        private LightConfigurator _lightConfigurator = new LightConfigurator();
        //“ı”∞≈‰÷√
        private ShadowCaster _shadowPass = new ShadowCaster();
        ShadowSetting _shadowSetting;


        bool dynamicBatchingFlag;
        bool instance;


        private RenderObjectPass _opauquePass = new RenderObjectPass(false);


        // Start is called before the first frame update
        public ForWardRP(MyPipelineAsset settings)
        {
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.useScriptableRenderPipelineBatching = settings.useSRPBacth;
            dynamicBatchingFlag = settings.dynamicBatching;
            instance = settings.instanceFlag;
            _shadowSetting = settings._shadowSetting;
        }

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            camera.TryGetCullingParameters(out var cullingParameters);
            cullingParameters.shadowDistance = Mathf.Min(100, camera.farClipPlane - camera.nearClipPlane);
            cullingResults = context.Cull(ref cullingParameters);

            LightData lightData = _lightConfigurator.SetupShaderLightingParams(context, ref cullingResults);
            ShadowCasterSetting shadowCasterSetting = new ShadowCasterSetting();
            shadowCasterSetting.lightData = lightData;
            shadowCasterSetting.shadowSetting = _shadowSetting;
            shadowCasterSetting.cullingResults = cullingResults;
            _shadowPass.Render(context, camera, ref shadowCasterSetting);

           
            _opauquePass.Render(context, camera, ref cullingResults);

            context.DrawSkybox(camera);
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

