using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP
{
    public class ComputeMaterial
    {
        int _materialNUMs;

        public ComputeBuffer materialCountBuffer;//用于不同的材质计数
        public ComputeBuffer materialAccumulateBuffer;//用于计算偏移量
        public ComputeBuffer pixelBuffer;//存储各个材质的像素索引
        public ComputeMaterial(int materialNum,Vector2 resolution)
        {
            _materialNUMs = materialNum;


        }


    }

}
