static const int VTVirtualPageCount = 1024;
static const int VTPageSize = 128;
static const int VTMaxMip = 6;
static const int LightTypeOmni = 1;
static const int LightTypeOmniShadow = 2;
static const int LightTypeSpotShadow = 3;
static const int LightSpotShapeSquare = 65536;
static const int LightSpotShapeRound = 131072;

// Fusion.Engine.Graphics.SceneRenderer+STAGE
// Marshal.SizeOf = 1024
struct STAGE {
	float4x4   Projection;                    // offset:    0
	float4x4   View;                          // offset:   64
	float4x4   GradientToNormal;              // offset:  128
	float4x4   CascadeViewProjection0;        // offset:  192
	float4x4   CascadeViewProjection1;        // offset:  256
	float4x4   CascadeViewProjection2;        // offset:  320
	float4x4   CascadeViewProjection3;        // offset:  384
	float4x4   CascadeGradientMatrix0;        // offset:  448
	float4x4   CascadeGradientMatrix1;        // offset:  512
	float4x4   CascadeGradientMatrix2;        // offset:  576
	float4x4   CascadeGradientMatrix3;        // offset:  640
	float4     CascadeScaleOffset0;           // offset:  704
	float4     CascadeScaleOffset1;           // offset:  720
	float4     CascadeScaleOffset2;           // offset:  736
	float4     CascadeScaleOffset3;           // offset:  752
	float4     ViewPos;                       // offset:  768
	float4     BiasSlopeFar;                  // offset:  784
	float4     Ambient;                       // offset:  800
	float4     ViewBounds;                    // offset:  816
	float4     DirectLightDirection;          // offset:  832
	float4     DirectLightIntensity;          // offset:  848
	float      DirectLightAngularSize;        // offset:  864
	float      VTPageScaleRCP;                // offset:  868
	float      ShadowGradientBiasX;           // offset:  872
	float      ShadowGradientBiasY;           // offset:  876
	float      GradientScaler;                // offset:  880
	float      DebugGradientScale;            // offset:  884
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
// Marshal.SizeOf = 120
struct LIGHT {
	float4x4   ViewProjection;                // offset:    0
	float4     PositionRadius;                // offset:   64
	float4     IntensityFar;                  // offset:   80
	float4     ShadowScaleOffset;             // offset:   96
	int        LightType;                     // offset:  112
	float      SourceRadius;                  // offset:  116
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

