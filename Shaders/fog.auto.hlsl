static const int FogSizeX = 128;
static const int FogSizeY = 64;
static const int FogSizeZ = 192;
static const int BlockSizeX = 4;
static const int BlockSizeY = 4;
static const int BlockSizeZ = 4;
static const uint LightTypeOmni = 1;
static const uint LightTypeSpotShadow = 3;
static const uint LightSpotShapeRound = 131072;
static const uint LightSpotShapeSquare = 65536;

// Fusion.Engine.Graphics.SceneRenderer+LIGHT
// Marshal.SizeOf = 120
struct LIGHT {
	float4x4   ViewProjection;                // offset:    0
	float4     PositionRadius;                // offset:   64
	float4     IntensityFar;                  // offset:   80
	float4     ShadowScaleOffset;             // offset:   96
	uint       LightType;                     // offset:  112
	float      SourceRadius;                  // offset:  116
};

// Fusion.Engine.Graphics.SceneRenderer+LIGHTINDEX
// Marshal.SizeOf = 8
struct LIGHTINDEX {
	uint       Offset;                        // offset:    0
	uint       Count;                         // offset:    4
};

// Fusion.Engine.Graphics.Fog+PARAMS
// Marshal.SizeOf = 1024
struct PARAMS {
	float4x4   View;                          // offset:    0
	float4x4   Projection;                    // offset:   64
	float4x4   ViewProjection;                // offset:  128
	float4x4   CameraMatrix;                  // offset:  192
	float4x4   InvertedViewMatrix;            // offset:  256
	float4x4   CascadeViewProjection0;        // offset:  320
	float4x4   CascadeViewProjection1;        // offset:  384
	float4x4   CascadeViewProjection2;        // offset:  448
	float4x4   CascadeViewProjection3;        // offset:  512
	float4     CascadeScaleOffset0;           // offset:  576
	float4     CascadeScaleOffset1;           // offset:  592
	float4     CascadeScaleOffset2;           // offset:  608
	float4     CascadeScaleOffset3;           // offset:  624
	float4     DirectLightDirection;          // offset:  640
	float4     DirectLightIntensity;          // offset:  656
	float4     CameraForward;                 // offset:  672
	float4     CameraRight;                   // offset:  688
	float4     CameraUp;                      // offset:  704
	float4     CameraPosition;                // offset:  720
	float      CameraTangentX;                // offset:  736
	float      CameraTangentY;                // offset:  740
};

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

