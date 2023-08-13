using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MySRP
{
    public class PerObjectMaterialProperties : MonoBehaviour

    {
        static MaterialPropertyBlock block;
        static int baseColorId = Shader.PropertyToID("_BaseColor");
        static int metallicId = Shader.PropertyToID("_Metallic");
        static int roughnessId = Shader.PropertyToID("_Roughness");
        [SerializeField]
        Color baseColor = Color.white;
        [SerializeField, Range(0f, 1f)]
        float metallic = 0f, roughness = 0.5f;

        private void OnValidate()
        {
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }
            block.SetColor(baseColorId, baseColor);
            block.SetFloat(metallicId, metallic);
            block.SetFloat(roughnessId, roughness);
            GetComponent<Renderer>().SetPropertyBlock(block);
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        void Awake()
        {
            OnValidate();
        }
    }
}
