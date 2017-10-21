static const int MaxSurfels = 65536;
static const int BlockSize = 256;

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
// Marshal.SizeOf = 256
struct LIGHTENPARAMS {
	float4x4   LightView;                     // offset:    0
	float4x4   LightProjection;               // offset:   64
	float4     ShadowRegion;                  // offset:  128
	float4     LightPosition;                 // offset:  144
	float4     LightIntensity;                // offset:  160
};

// Fusion.Engine.Graphics.InstantRadiosity+DRAWPARAMS
// Marshal.SizeOf = 256
struct DRAWPARAMS {
	float4x4   ViewProjection;                // offset:    0
};

