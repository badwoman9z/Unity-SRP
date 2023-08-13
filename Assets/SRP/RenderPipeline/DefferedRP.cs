using System.Collections;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.VisualScripting.Member;

namespace MySRP
{
    public class DefferedRP : RenderPipeline
    {

        //gbuffer 相关texture
        CommandBuffer _commandBuffer;
        private List<RenderTexture> _GBuffers = new List<RenderTexture>();
        private RenderTargetIdentifier[] _GBufferRTIs;
        private RenderTextureFormat[] _GBufferFormats = {
            RenderTextureFormat.ARGB32,//Albedo
            RenderTextureFormat.ARGB64,///Normal\reflectance
            RenderTextureFormat.ARGB64,//metallic \roughness\clearcoat\clearcoatRoughness\
            RenderTextureFormat.ARGB64,//sheenColor\SubfaceColor
            RenderTextureFormat.RFloat

        };
        private int[] _GBufferNameIDs = {
            ShaderProperties.GBuffer0,
            ShaderProperties.GBuffer1,
            ShaderProperties.GBuffer2,
            ShaderProperties.GBuffer3,
            ShaderProperties.GBuffer4
        };
        int GbufferNums = 5;
        private RenderTexture _depthTexture;
        private RenderTexture _colorTexture;






        /*
         GPU INSTANCE DATA
         */

        InstanceData[] instanceDatas;

        CullingResults cullingResults;


        ShadowSetting _shadowSetting;

        ShadowCaster _shadowPass = new ShadowCaster();
        RenderObjectPass _meshRender = new RenderObjectPass(false, "Gbuffer");
        BlitPass _deferedLightPass = new BlitPass("DefferedLight");
        VolumeCloudPass _volumeCloud = new VolumeCloudPass("VolumeCloud");
        LightConfigurator _lightConfigurator = new LightConfigurator();
        LightCullPass _lightCullPass = new LightCullPass();
        FullScreenPass _lightPass = new FullScreenPass("MutiShaderModel");
        BlitPass _postPass = new BlitPass("PostProcess"); 

        Mesh _fullScreenMesh;
        private Material _MetalLitMaterial;
        private Material _volumeCloudMat;
        private Material _ClothLitMaterial;
        private Material _postMaterial;

        public DefferedRP(MyPipelineAsset asset)
        {
            _commandBuffer = new CommandBuffer()
            {
                name = "DefferedRP"
            };


            //_defferedLightMaterial = new Material(Shader.Find("My Pipeline/DefferedLit"));
            _MetalLitMaterial = Resources.Load("Mat_MetalLit", typeof(Material)) as Material;
            _ClothLitMaterial = Resources.Load("Mat_ClothLit", typeof(Material)) as Material;
            _postMaterial = new Material(Shader.Find("My Pipeline/PostProcess"));
            _volumeCloudMat = Resources.Load("VolumeCloudMat", typeof(Material)) as Material;
            _shadowSetting = asset._shadowSetting;

            instanceDatas = asset.instanceDatas;
        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                Render(context, camera);
            }
            //Render(context, cameras[0]);
        }
        private void Render(ScriptableRenderContext context, Camera camera)
        {
            //Debug.Log(Screen.width);
           // Debug.Log(Screen.height);
           


            //剔除
            camera.TryGetCullingParameters(out var cullingParameters);
            cullingParameters.shadowDistance = Mathf.Min(100, camera.farClipPlane - camera.nearClipPlane);
            cullingResults =  context.Cull(ref cullingParameters);

            //context.SetupCameraProperties(camera);

            CameraUtil.ConfigShaderProperties(_commandBuffer, camera);

            _lightCullPass.Render( ref cullingResults, camera);

            ShadowCastPassRender(context, camera);

            //因为之前shadowpass更改了摄像机参数，现在设置回来
            context.SetupCameraProperties(camera);

            GBufferRender(context, camera,ref  cullingResults);
            
            //DefferedLightPassRender(context, camera);
            DefferedLightMutiShaderModel(context, camera);

            //DrawSkyBox(context, camera);

            //VolumeCloudRender(context);
            // skybox and Gizmos
            //context.DrawSkybox(camera);
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
            context.Submit();
        }

        /*
         各RenderPass
         */
        private void ShadowCastPassRender(ScriptableRenderContext context,Camera camera)
        {
            LightData lightData = _lightConfigurator.SetupShaderLightingParams(context, ref cullingResults);
            ShadowCasterSetting shadowCasterSetting = new ShadowCasterSetting();
            shadowCasterSetting.lightData = lightData;
            shadowCasterSetting.shadowSetting = _shadowSetting;
            shadowCasterSetting.cullingResults = cullingResults;
            _shadowPass.Render(context, camera, ref shadowCasterSetting);
        }
        private void GBufferRender(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {

            this.ConfigGBufferTexture(context, camera);
            _meshRender.Render(context, camera, ref cullingResults);

            //Debug.Log(instanceDatas[0]);

            ComputeShader cullingCs = Resources.Load<ComputeShader>("Shaders/InstanceCull"); ;

            if (instanceDatas != null)
            {

                for (int i = 0; i < instanceDatas.Length; i++)
                {
                    InstanceDraw.Draw(instanceDatas[i], context, cullingCs);
                }

            }


        }

        private void DefferedLightPassRender(ScriptableRenderContext context,Camera camera)
        {

            this.CreateColorTextureIfNot(context,camera);
            
            _deferedLightPass.Render(context, _MetalLitMaterial, _GBufferRTIs[0], _colorTexture);

            
        }
        private void DefferedLightMutiShaderModel(ScriptableRenderContext context, Camera camera)
        {
            this.CreateColorTextureIfNot(context, camera);
            _commandBuffer.SetRenderTarget(_colorTexture, _depthTexture);
            _commandBuffer.ClearRenderTarget(false, true, Color.black);
            context.ExecuteCommandBuffer(_commandBuffer);

            _commandBuffer.Clear();

            _lightPass.Render(context, ref _fullScreenMesh, _MetalLitMaterial, camera);
            _lightPass.Render(context, ref _fullScreenMesh, _ClothLitMaterial, camera);

            context.DrawSkybox(camera);

            _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            _commandBuffer.SetGlobalTexture("_BlitTex", _colorTexture);
            context.ExecuteCommandBuffer(_commandBuffer);

            _commandBuffer.Clear();            
            _volumeCloud.Render(context, _volumeCloudMat, _colorTexture, BuiltinRenderTextureType.CameraTarget);



        }
        private void DrawSkyBox(ScriptableRenderContext context,Camera camera)
        {
            _commandBuffer.SetRenderTarget(_colorTexture, _depthTexture);
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();

            context.DrawSkybox(camera);
        }
        private void VolumeCloudRender(ScriptableRenderContext context)
        {
            _volumeCloud.Render(context, _volumeCloudMat, _colorTexture, BuiltinRenderTextureType.CameraTarget);
        }
        
        
        
        
        private void CreateColorTextureIfNot(ScriptableRenderContext context,Camera camera)
        {
            if (_colorTexture)
            {
                if (_colorTexture.width != camera.pixelWidth || _colorTexture.height != camera.pixelHeight)
                {
                    this.ReleaseColorTexture();
                }
            }
            if (_colorTexture == null)
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                descriptor.depthBufferBits = 0;
                descriptor.depthBufferBits = 0;
                descriptor.sRGB = true;
                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                descriptor.enableRandomWrite = true;
                _colorTexture = RenderTexture.GetTemporary(descriptor);
                _colorTexture.Create();
                
            }
        }
        private void ReleaseColorTexture()
        {
            if (_colorTexture)
            {
                RenderTexture.ReleaseTemporary(_colorTexture);
                _colorTexture = null;
            }
        }
        
        /*
         * 
         * GBuffer 相关
         * 
         * */
        
        


        private void ConfigGBufferTexture(ScriptableRenderContext context,Camera camera)
        {
            this.CreateGBuffersIfNot(context,camera);
            //Debug.Log(_GBuffers);
            this.CreateDepthBufferIfNot(context, camera);
          
            _commandBuffer.SetRenderTarget(_GBufferRTIs, _depthTexture);
            _commandBuffer.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
        }
        //回收Gbuffer资源
        private void RelaseGBuffers()
        {
            if (_GBuffers.Count > 0)
            {
                foreach (var g in _GBuffers)
                {
                    RenderTexture.ReleaseTemporary(g);
                }
            }
            _GBuffers.Clear();
            _GBufferRTIs = null;
        }
        private void ReleaseDepthTexture()
        {
            if (_depthTexture)
            {
                RenderTexture.ReleaseTemporary(_depthTexture);
                _depthTexture = null;
            }
        }
        //创建Gbuffer
        private void CreateGBuffersIfNot(ScriptableRenderContext context,Camera camera)
        {
            if (_GBuffers.Count > 0)
            {
                var g0 = _GBuffers[0];
                if (g0.width != camera.pixelWidth || g0.height != camera.pixelHeight)
                {
                    this.RelaseGBuffers();
                    //Debug.Log("change");
                }
            }
            if (_GBuffers.Count == 0)
            {
                _GBufferRTIs = new RenderTargetIdentifier[GbufferNums];
               
                for (int i = 0; i < GbufferNums; i++)
                {
                    //Debug.Log(Screen.width);
                    RenderTextureDescriptor descriptor = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight, _GBufferFormats[i], 0, 1);
                    var rt = RenderTexture.GetTemporary(descriptor);
                    rt.filterMode = FilterMode.Bilinear;
                    rt.Create();
                    _GBuffers.Add(rt);
                    _commandBuffer.SetGlobalTexture(_GBufferNameIDs[i], rt);
                    _GBufferRTIs[i] = rt;
                }
                context.ExecuteCommandBuffer(_commandBuffer);
                _commandBuffer.Clear();
            }
        }

        private void CreateDepthBufferIfNot(ScriptableRenderContext context,Camera camera)
        {
            if (_depthTexture)
            {
                if (_depthTexture.width != camera.pixelWidth || _depthTexture.height != camera.pixelHeight)
                {
                    this.ReleaseDepthTexture();
                }
            }
            if (!_depthTexture)
            {
                RenderTextureDescriptor depthDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight ,RenderTextureFormat.Depth, 32, 1);
                _depthTexture = RenderTexture.GetTemporary(depthDesc);
                _depthTexture.Create();
                _commandBuffer.SetGlobalTexture(ShaderProperties.CameraDepthTexture, _depthTexture);
                context.ExecuteCommandBuffer(_commandBuffer);
                _commandBuffer.Clear();

            }
        }



        public static class ShaderProperties {

            public static readonly int GBuffer0 = Shader.PropertyToID("_Albedo");
            public static readonly int GBuffer1 = Shader.PropertyToID("_Normal");
            public static readonly int GBuffer2 = Shader.PropertyToID("_MetalOrClearCoat");
            public static readonly int GBuffer3 = Shader.PropertyToID("_Sheen");
            public static readonly int GBuffer4 = Shader.PropertyToID("_Material");
            public static readonly int CameraDepthTexture = Shader.PropertyToID("_DepthTexture");
        }
        

    }
}
