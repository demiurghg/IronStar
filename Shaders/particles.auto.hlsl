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
// Marshal.SizeOf = 148
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
	float      Temperature;                   // offset:   76
	float      Scattering;                    // offset:   80
	float4     LightmapRegion;                // offset:   84
	float      Gravity;                       // offset:  100
	float      Damping;                       // offset:  104
	float      Size0;                         // offset:  108
	float      Size1;                         // offset:  112
	float      Rotation0;                     // offset:  116
	float      Rotation1;                     // offset:  120
	float      LifeTime;                      // offset:  124
	float      TimeLag;                       // offset:  128
	float      FadeIn;                        // offset:  132
	float      FadeOut;                       // offset:  136
	int        ImageIndex;                    // offset:  140
	uint       Effects;                       // offset:  144
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

// Fusion.Engine.Graphics.SceneRenderer+LIGHTPROBE
// Marshal.SizeOf = 32
struct LIGHTPROBE {
	float4     Position;                      // offset:    0
	float      InnerRadius;                   // offset:   16
	float      OuterRadius;                   // offset:   20
	uint       ImageIndex;                    // offset:   24
	float      Dummy1;                        // offset:   28
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
	float4x4   OcclusionGridMatrix;           // offset:  448
	float4     CascadeScaleOffset0;           // offset:  512
	float4     CascadeScaleOffset1;           // offset:  528
	float4     CascadeScaleOffset2;           // offset:  544
	float4     CascadeScaleOffset3;           // offset:  560
	float4     CameraForward;                 // offset:  576
	float4     CameraRight;                   // offset:  592
	float4     CameraUp;                      // offset:  608
	float4     CameraPosition;                // offset:  624
	float4     Gravity;                       // offset:  640
	float4     LightMapSize;                  // offset:  656
	float4     DirectLightDirection;          // offset:  672
	float4     DirectLightIntensity;          // offset:  688
	float4     SkyAmbientLevel;               // offset:  704
	float4     FogColor;                      // offset:  720
	float      FogAttenuation;                // offset:  736
	float      LinearizeDepthA;               // offset:  740
	float      LinearizeDepthB;               // offset:  744
	int        MaxParticles;                  // offset:  748
	float      DeltaTime;                     // offset:  752
	uint       DeadListSize;                  // offset:  756
	float      CocScale;                      // offset:  760
	float      CocBias;                       // offset:  764
};

