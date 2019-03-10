static const int VTVirtualPageCount = 1024;
static const int VTPageSize = 128;
static const int VTMaxMip = 6;
static const int VTMipSelectorScale = 2048;
static const uint LightTypeOmni = 1;
static const uint LightTypeOmniShadow = 2;
static const uint LightTypeSpotShadow = 3;
static const uint LightTypeAmbient = 4;
static const uint LightSpotShapeSquare = 65536;
static const uint LightSpotShapeRound = 131072;
static const uint LightProbeSize = 128;
static const uint LightProbeMaxSpecularMip = 4;
static const uint LightProbeDiffuseMip = 5;
static const uint InstanceGroupStatic = 1;
static const uint InstanceGroupDynamic = 4;
static const uint InstanceGroupCharacter = 8;
static const uint InstanceGroupWeapon = 16;

// Fusion.Engine.Graphics.SceneRenderer+STAGE
// Marshal.SizeOf = 1024
struct STAGE {
	float4x4   Projection;                    // offset:    0
	float4x4   ProjectionFPV;                 // offset:   64
	float4x4   View;                          // offset:  128
	float4x4   CascadeViewProjection0;        // offset:  192
	float4x4   CascadeViewProjection1;        // offset:  256
	float4x4   CascadeViewProjection2;        // offset:  320
	float4x4   CascadeViewProjection3;        // offset:  384
	float4x4   CascadeGradientMatrix0;        // offset:  448
	float4x4   CascadeGradientMatrix1;        // offset:  512
	float4x4   CascadeGradientMatrix2;        // offset:  576
	float4x4   CascadeGradientMatrix3;        // offset:  640
	float4x4   OcclusionGridMatrix;           // offset:  704
	float4     CascadeScaleOffset0;           // offset:  768
	float4     CascadeScaleOffset1;           // offset:  784
	float4     CascadeScaleOffset2;           // offset:  800
	float4     CascadeScaleOffset3;           // offset:  816
	float4     ViewPos;                       // offset:  832
	float4     BiasSlopeFar;                  // offset:  848
	float4     SkyAmbientLevel;               // offset:  864
	float4     ViewBounds;                    // offset:  880
	float4     DirectLightDirection;          // offset:  896
	float4     DirectLightIntensity;          // offset:  912
	float4     FogColor;                      // offset:  928
	float      FogAttenuation;                // offset:  944
	float      DirectLightAngularSize;        // offset:  948
	float      VTPageScaleRCP;                // offset:  952
	float      VTGradientScaler;              // offset:  956
	float      SsaoWeight;                    // offset:  960
};

// Fusion.Engine.Graphics.SceneRenderer+INSTANCE
// Marshal.SizeOf = 128
struct INSTANCE {
	float4x4   World;                         // offset:    0
	float4     Color;                         // offset:   64
	float4     LMRegion;                      // offset:   80
	int        Group;                         // offset:   96
};

// Fusion.Engine.Graphics.SceneRenderer+SUBSET
// Marshal.SizeOf = 48
struct SUBSET {
	float4     Rectangle;                     // offset:    0
	float4     Color;                         // offset:   16
	float      MaxMip;                        // offset:   32
	float      Dummy1;                        // offset:   36
	float      Dummy2;                        // offset:   40
	float      Dummy3;                        // offset:   44
};

// Fusion.Engine.Graphics.SceneRenderer+LIGHTINDEX
// Marshal.SizeOf = 8
struct LIGHTINDEX {
	uint       Offset;                        // offset:    0
	uint       Count;                         // offset:    4
};

// Fusion.Engine.Graphics.SceneRenderer+LIGHTPROBE
// Marshal.SizeOf = 96
struct LIGHTPROBE {
	float4x4   MatrixInv;                     // offset:    0
	float4     Position;                      // offset:   64
	uint       ImageIndex;                    // offset:   80
	float      NormalizedWidth;               // offset:   84
	float      NormalizedHeight;              // offset:   88
	float      NormalizedDepth;               // offset:   92
};

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

