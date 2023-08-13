using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP
{


    public class VolumeCloudPass : BlitPass
    {
        private GameObject cloudBox;
        private Transform cloudTransform;
        private Vector3 boundsMin;
        private Vector3 boundsMax;
        public VolumeCloudPass(string passName) : base(passName)
        {
            cloudBox = GameObject.Find("VolumeCloud");

            if (cloudBox != null)
            {
                cloudTransform = cloudBox.GetComponent<Transform>();
            }
        }

        public void Render(ScriptableRenderContext context, Material passMaterial, RenderTargetIdentifier source, RenderTargetIdentifier desc)
        {
            if(cloudTransform != null)
            {
                boundsMin = cloudTransform.position - cloudTransform.localScale / 2;
                boundsMax = cloudTransform.position + cloudTransform.localScale / 2;
                Shader.SetGlobalVector(ShaderProperties.boundsMin, boundsMin);
                Shader.SetGlobalVector (ShaderProperties.boundsMax, boundsMax);
            }
            base.Render(context, passMaterial, source, desc);
        }
        public static class ShaderProperties
        {
            public static readonly int boundsMin = Shader.PropertyToID("_boundsMin");
            public static readonly int boundsMax = Shader.PropertyToID("_boundsMax");

        }

    }
}

