using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP {

    public class FullScreenPass
    {      
        
        CommandBuffer _commandBuffer;

        public FullScreenPass(string PassName)
        {
            _commandBuffer = new CommandBuffer() { 
                name = PassName
            };

           
        }

        public void Render(ScriptableRenderContext context,ref Mesh fullScreenMesh,Material mat,Camera camera)
        {
            if (!fullScreenMesh)
            {
                fullScreenMesh = Utils.CreateFullscreenMesh();
            }
            _commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            _commandBuffer.DrawMesh(fullScreenMesh, Matrix4x4.identity, mat);
            _commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            context.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();

        }

    }


}



