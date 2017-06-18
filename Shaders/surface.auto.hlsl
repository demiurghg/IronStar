static const int VTVirtualPageCount = 1024;
static const int VTPageSize = 128;
static const int VTMaxMip = 5;
static const int LightTypeOmni = 1;
static const int LightTypeOmniShadow = 2;
static const int LightTypeSpotShadow = 3;
static const int LightSpotShapeSquare = 65536;
static const int LightSpotShapeRound = 131072;

// Fusion.Engine.Graphics.SceneRenderer+STAGE
// Marshal.SizeOf = 512
struct STAGE {
	float4x4   Projection;                    // offset:    0
	float4x4   View;                          // offset:   64
	float4x4   CascadeViewProjection0;        // offset:  128
	float4x4   CascadeViewProjection1;        // offset:  192
	float4x4   CascadeViewProjection2;        // offset:  256
	float4x4   CascadeViewProjection3;        // offset:  320
	float4     ViewPos;                       // offset:  384
	float4     BiasSlopeFar;                  // offset:  400
	float4     Ambient;                       // offset:  416
	float4     ViewBounds;                    // offset:  432
	float4     DirectLightDirection;          // offset:  448
	float4     DirectLightIntensity;          // offset:  464
	float      VTPageScaleRCP;                // offset:  480
};

// Fusion.Engine.Graphics.SceneRenderer+INSTANCE
// Marshal.SizeOf = 96
struct INSTANCE {
	float4x4   World;                         // offset:    0
	float4     Color;                         // offset:   64
	int        AssignmentGroup;               // offset:   80
};

// Fusion.Engine.Graphics.SceneRenderer+SUBSET
// Marshal.SizeOf = 16
struct SUBSET {
	float4     Rectangle;                     // offset:    0
};

// Fusion.Engine.Graphics.SceneRenderer+LIGHTINDEX
// Marshal.SizeOf = 8
struct LIGHTINDEX {
	uint       Offset;                        // offset:    0
	uint       Count;                         // offset:    4
};

// Fusion.Engine.Graphics.SceneRenderer+LIGHT
// Marshal.SizeOf = 116
struct LIGHT {
	float4x4   ViewProjection;                // offset:    0
	float4     PositionRadius;                // offset:   64
	float4     IntensityFar;                  // offset:   80
	float4     ShadowScaleOffset;             // offset:   96
	int        LightType;                     // offset:  112
};

// Fusion.Engine.Graphics.SceneRenderer+DECAL
// Marshal.SizeOf = 184
struct DECAL {
	float4x4   DecalMatrixInv;                // offset:    0
	float4     BasisX;                        // offset:   64
	float4     BasisY;                        // offset:   80
	float4     BasisZ;                        // offset:   96
	float4     EmissionRoughness;             // offset:  112
	float4     ImageScaleOffset;              // offset:  128
	float4     BaseColorMetallic;             // offset:  144
	float      ColorFactor;                   // offset:  160
	float      SpecularFactor;                // offset:  164
	float      NormalMapFactor;               // offset:  168
	float      FalloffFactor;                 // offset:  172
	int        AssignmentGroup;               // offset:  176
	float      MipBias;                       // offset:  180
};

