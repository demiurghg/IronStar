static const int MaxSurfels = 65536;
static const int BlockSize = 256;
static const int BlockSize3D = 4;

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

// Fusion.Engine.Graphics.InstantRadiosity+SURFEL
// Marshal.SizeOf = 48
struct SURFEL {
	float4     Position;                      // offset:    0
	float4     NormalArea;                    // offset:   16
	float4     Intensity;                     // offset:   32
};

// Fusion.Engine.Graphics.InstantRadiosity+LIGHTENPARAMS
// Marshal.SizeOf = 512
struct LIGHTENPARAMS {
	float4x4   CascadeViewProjection0;        // offset:    0
	float4x4   CascadeViewProjection1;        // offset:   64
	float4x4   CascadeViewProjection2;        // offset:  128
	float4x4   CascadeViewProjection3;        // offset:  192
	float4     CascadeScaleOffset0;           // offset:  256
	float4     CascadeScaleOffset1;           // offset:  272
	float4     CascadeScaleOffset2;           // offset:  288
	float4     CascadeScaleOffset3;           // offset:  304
	float4     DirectLightDirection;          // offset:  320
	float4     DirectLightIntensity;          // offset:  336
	int        LightCount;                    // offset:  352
};

// Fusion.Engine.Graphics.InstantRadiosity+DRAWPARAMS
// Marshal.SizeOf = 512
struct DRAWPARAMS {
	float4x4   ViewProjection;                // offset:    0
};

// Fusion.Engine.Graphics.InstantRadiosity+LMPARAMS
// Marshal.SizeOf = 512
struct LMPARAMS {
	float4x4   ViewProjection;                // offset:    0
};

