using UnityEngine;

using UnityEngine.Rendering;
namespace MySRP
{
    [CreateAssetMenu(menuName = "Rendering/My Pipeline")]
    public class MyPipelineAsset : RenderPipelineAsset

    {
        [SerializeField]
        public bool dynamicBatching;
        [SerializeField]
        public bool instanceFlag;
        [SerializeField]
        public bool useSRPBacth;
        [SerializeField]
        public ShadowSetting _shadowSetting = new ShadowSetting();
        public InstanceData[] instanceDatas;

        protected override RenderPipeline CreatePipeline()
        {
            return new DefferedRP(this);
        }
    }
}

