static const int VTVirtualPageCount = 1024;
static const int VTPageSize = 128;
static const int VTMaxMip = 5;
static const int LightTypeOmni = 1;
static const int LightTypeOmniShadow = 2;
static const int LightTypeSpotShadow = 3;
static const int LightSpotShapeSquare = 65536;
static const int LightSpotShapeRound = 131072;

// Fusion.Engine.Graphics.SceneRenderer+BATCH
// Marshal.SizeOf = 320
struct BATCH {
	float4x4   Projection;                    // offset:    0
	float4x4   View;                          // offset:   64
	float4x4   World;                         // offset:  128
	float4     ViewPos;                       // offset:  192
	float4     BiasSlopeFar;                  // offset:  208
	float4     Color;                         // offset:  224
	float4     Ambient;                       // offset:  240
	float4     ViewBounds;                    // offset:  256
	float      VTPageScaleRCP;                // offset:  272
	int        AssignmentGroup;               // offset:  276
};

// Fusion.Engine.Graphics.SceneRenderer+SUBSET
// Marshal.SizeOf = 16
struct SUBSET {
	float4     Rectangle;                     // offset:    0
};

// Fusion.Engine.Graphics.SceneRenderer+LIGHTDATA
// Marshal.SizeOf = 656
struct LIGHTDATA {
	float4x4   View;                          // offset:    0
	float4x4   Projection;                    // offset:   64
	float4     ViewPosition;                  // offset:  512
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

