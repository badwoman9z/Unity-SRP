#ifndef MYRP_LIGHTINPUTE_INCLUDED
#define MYRP_LIGHTINPUTE_INCLUDED




int _lightCount;
int _mainLightIndex;
struct DirectionOrPointLight{
	float4 color;
	float4 directionOrPosition;

};
struct PointLight
{
    float3 color;
    float intensity;
    float3 position;
    float radius;
};
struct LightIndex
{
    int count;
    int start;
};

StructuredBuffer<DirectionOrPointLight> lights;

StructuredBuffer<PointLight> _lightBuffer;
StructuredBuffer<uint> _lightAssignBuffer;
StructuredBuffer<LightIndex> _assignTable;

float _numClusterX;
float _numClusterY;
float _numClusterZ;






#endif // MYRP_LIGHTINPUTE_INCLUDED