using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MySRP
{
    public enum ShadowPCFType
    {
        None = 0,
        PCF1,
        PCF3Fast,
        PCF3,
        PCF5,
    }
    [System.Serializable]
    public class ShadowSetting
    {
        [SerializeField]
        [Range(10, 500)]
        [Tooltip("��Զ��Ӱ����")]
        private float _maxShadowDistance = 100;

        [SerializeField]
        [Range(1, 4)]
        [Tooltip("������Ӱ����")]
        private int _shadowCascadeCount = 1;

        [SerializeField]
        [Range(1, 100)]
        [Tooltip("1������Ӱ����")]
        private float _cascadeRatio1 = 1;

        [SerializeField]
        [Range(1, 100)]
        [Tooltip("2������Ӱ����")]
        private float _cascadeRatio2 = 0;
        [SerializeField]
        [Range(1, 100)]
        [Tooltip("3������Ӱ����")]
        private float _cascadeRatio3 = 0;

        [SerializeField]
        [Range(1, 100)]
        [Tooltip("4������Ӱ����")]
        private float _cascadeRatio4 = 0;
        [SerializeField]
        public ShadowPCFType _shadowPCFType = ShadowPCFType.None;
        public int cascadeCount
        {
            get
            {
                return _shadowCascadeCount;
            }
        }

        public Vector3 cascadeRatio
        {
            get
            {
                var total = _cascadeRatio1;
                if (_shadowCascadeCount > 1)
                {
                    total += _cascadeRatio2;
                }
                if (_shadowCascadeCount > 2)
                {
                    total += _cascadeRatio3;
                }
                if (_shadowCascadeCount > 3)
                {
                    total += _cascadeRatio4;
                }
                return new Vector3(_cascadeRatio1 / total, _cascadeRatio2 / total, _cascadeRatio3 / total);
            }
        }



        public float shadowDistance
        {
            get
            {
                return _maxShadowDistance;
            }
        }
    }
}
