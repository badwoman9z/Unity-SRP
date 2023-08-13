using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MySRP
{
    [ExecuteAlways]
    public class ClusterDebug : MonoBehaviour
    {
        LightCullPass _lightCull;

    void Update()
        {
            if (_lightCull == null)
            {
                _lightCull = new LightCullPass();
            }

            // 更新光源
            var lights = FindObjectsOfType(typeof(Light)) as Light[];
            _lightCull.UpdateLightBuffer(lights);

            // 划分 cluster
            Camera camera = Camera.main;

            //Debug.Log(camera.transform.position);
            _lightCull.GenerateCluster(camera);

            //// 分配光源
            _lightCull.LightAssign();

            // Debug
            _lightCull.DegbugCluster();
            _lightCull.DebugLightAssign();
        }
    }
}
