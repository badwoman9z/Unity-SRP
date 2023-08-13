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

            // ���¹�Դ
            var lights = FindObjectsOfType(typeof(Light)) as Light[];
            _lightCull.UpdateLightBuffer(lights);

            // ���� cluster
            Camera camera = Camera.main;

            //Debug.Log(camera.transform.position);
            _lightCull.GenerateCluster(camera);

            //// �����Դ
            _lightCull.LightAssign();

            // Debug
            _lightCull.DegbugCluster();
            _lightCull.DebugLightAssign();
        }
    }
}
