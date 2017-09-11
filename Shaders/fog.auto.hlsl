static const int BlockSizeX = 4;
static const int BlockSizeY = 4;
static const int BlockSizeZ = 4;

// Fusion.Engine.Graphics.Fog+FogConsts
// Marshal.SizeOf = 160
struct FogConsts {
	float4x4   MatrixWVP;                     // offset:    0
	float3     SunPosition;                   // offset:   64
	float4     SunColor;                      // offset:   80
	float      Turbidity;                     // offset:   96
	float3     Temperature;                   // offset:  100
	float      SkyIntensity;                  // offset:  112
	float3     Ambient;                       // offset:  116
	float      Time;                          // offset:  128
	float3     ViewPos;                       // offset:  132
	float      SunAngularSize;                // offset:  136
};

