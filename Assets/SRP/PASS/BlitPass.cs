using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MySRP
{
    public class BlitPass
    {
        private CommandBuffer _commandBuffer;

        public BlitPass(string passName)
        {
            _commandBuffer = new CommandBuffer()
            {
                name = passName
            };
        }
        public void Render(ScriptableRenderContext context, Material passMaterial,RenderTargetIdentifier source,RenderTargetIdentifier desc)
        {

            _commandBuffer.Blit(source, desc, passMaterial);            
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
           
        }
    }
}


