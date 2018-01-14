static const int BlockSizeX = 16;
static const int BlockSizeY = 16;
static const int LightProbeSize = 64;

// Fusion.Engine.Graphics.LightManager+RELIGHT_PARAMS
// Marshal.SizeOf = 192
struct RELIGHT_PARAMS {
	float4x4   ShadowViewProjection;          // offset:    0
	float4     LightProbePosition;            // offset:   64
	float4     DirectLightIntensity;          // offset:   80
	float4     DirectLightDirection;          // offset:   96
	float4     ShadowRegion;                  // offset:  112
	float4     SkyAmbient;                    // offset:  128
	float      CubeIndex;                     // offset:  144
	float      Roughness;                     // offset:  148
	float      TargetSize;                    // offset:  152
};

