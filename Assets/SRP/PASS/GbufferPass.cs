using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace MySRP
{
    public class GbufferPass 
    {
        CommandBuffer commandBuffer;
        RenderObjectPass _meshRender = new RenderObjectPass(false,"Gbuffer");
        public GbufferPass()
        {
            commandBuffer = new CommandBuffer()
            {
                name = "GbufferPass"
            };
        }
        
        public void Render(ScriptableRenderContext context,Camera camera,ref CullingResults cullingResults)
        {
            //清屏
            commandBuffer.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();

            _meshRender.Render(context, camera,ref cullingResults);

            
            // skybox and Gizmos


            // 提交绘制命令
            
        }
        
    }
}