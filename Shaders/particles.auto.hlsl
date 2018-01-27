static const int BLOCK_SIZE = 256;
static const int MAX_INJECTED = 4096;
static const int MAX_PARTICLES = 65536;
static const int MAX_IMAGES = 512;
static const uint ParticleFX_Hard = 0;
static const uint ParticleFX_HardLit = 1;
static const uint ParticleFX_HardLitShadow = 2;
static const uint ParticleFX_Soft = 3;
static const uint ParticleFX_SoftLit = 4;
static const uint ParticleFX_SoftLitShadow = 5;
static const uint ParticleFX_Distortive = 6;
static const uint LightmapRegionSize = 1024;
static const uint LightmapWidth = 4096;
static const uint LightmapHeight = 2048;
static const uint LightTypeOmni = 1;
static const uint LightTypeSpotShadow = 3;
static const uint LightSpotShapeRound = 131072;
static const uint LightSpotShapeSquare = 65536;

// Fusion.Engine.Graphics.Particle
// Marshal.SizeOf = 184
struct Particle {
	float3     Position;                      // offset:    0
	float3     Velocity;                      // offset:   12
	float3     Acceleration;                  // offset:   24
	float3     TailPosition;                  // offset:   36
	float3     Color;                         // offset:   48
	float      Alpha;                         // offset:   60
	float      Roughness;                     // offset:   64
	float      Metallic;                      // offset:   68
	float      Intensity;                     // offset:   72
	float      Temperature0;                  // offset:   76
	float      Temperature1;                  // offset:   80
	float4     LightmapRegion;                // offset:   84
	float3     LightBasisX;                   // offset:  100
	float3     LightBasisY;                   // offset:  112
	float3     LightBasisZ;                   // offset:  124
	float      Gravity;                       // offset:  136
	float      Damping;                       // offset:  140
	float      Size0;                         // offset:  144
	float      Size1;                         // offset:  148
	float      Rotation0;                     // offset:  152
	float      Rotation1;                     // offset:  156
	float      LifeTime;                      // offset:  160
	float      TimeLag;                       // offset:  164
	float      FadeIn;                        // offset:  168
	float      FadeOut;                       // offset:  172
	int        ImageIndex;                    // offset:  176
	uint       Effects;                       // offset:  180
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

// Fusion.Engine.Graphics.SceneRenderer+LIGHTINDEX
// Marshal.SizeOf = 8
struct LIGHTINDEX {
	uint       Offset;                        // offset:    0
	uint       Count;                         // offset:    4
};

// Fusion.Engine.Graphics.ParticleStream+PARAMS
// Marshal.SizeOf = 1024
struct PARAMS {
	float4x4   View;                          // offset:    0
	float4x4   Projection;                    // offset:   64
	float4x4   ViewProjection;                // offset:  128
	float4x4   CascadeViewProjection0;        // offset:  192
	float4x4   CascadeViewProjection1;        // offset:  256
	float4x4   CascadeViewProjection2;        // offset:  320
	float4x4   CascadeViewProjection3;        // offset:  384
	float4     CascadeScaleOffset0;           // offset:  448
	float4     CascadeScaleOffset1;           // offset:  464
	float4     CascadeScaleOffset2;           // offset:  480
	float4     CascadeScaleOffset3;           // offset:  496
	float4     CameraForward;                 // offset:  512
	float4     CameraRight;                   // offset:  528
	float4     CameraUp;                      // offset:  544
	float4     CameraPosition;                // offset:  560
	float4     Gravity;                       // offset:  576
	float4     LightMapSize;                  // offset:  592
	float4     DirectLightDirection;          // offset:  608
	float4     DirectLightIntensity;          // offset:  624
	float4     AmbientLevel;                  // offset:  640
	float4     FogColor;                      // offset:  656
	float      FogAttenuation;                // offset:  672
	float      LinearizeDepthA;               // offset:  676
	float      LinearizeDepthB;               // offset:  680
	int        MaxParticles;                  // offset:  684
	float      DeltaTime;                     // offset:  688
	uint       DeadListSize;                  // offset:  692
	float      CocScale;                      // offset:  696
	float      CocBias;                       // offset:  700
};

