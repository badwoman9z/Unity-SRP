using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MySRP
{
    public class ComputeMaterial
    {
        int _materialNUMs;

        public ComputeBuffer materialCountBuffer;//���ڲ�ͬ�Ĳ��ʼ���
        public ComputeBuffer materialAccumulateBuffer;//���ڼ���ƫ����
        public ComputeBuffer pixelBuffer;//�洢�������ʵ���������
        public ComputeMaterial(int materialNum,Vector2 resolution)
        {
            _materialNUMs = materialNum;


        }


    }

}
