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
// Marshal.SizeOf = 116
struct Particle {
	float3     Position;                      // offset:    0
	float3     Velocity;                      // offset:   12
	float3     Acceleration;                  // offset:   24
	float3     Color;                         // offset:   36
	float      Alpha;                         // offset:   48
	float      Roughness;                     // offset:   52
	float      Metallic;                      // offset:   56
	float      Intensity;                     // offset:   60
	float      Scattering;                    // offset:   64
	float      Gravity;                       // offset:   68
	float      Damping;                       // offset:   72
	float      Size0;                         // offset:   76
	float      Size1;                         // offset:   80
	float      Rotation0;                     // offset:   84
	float      Rotation1;                     // offset:   88
	float      LifeTime;                      // offset:   92
	float      TimeLag;                       // offset:   96
	float      FadeIn;                        // offset:  100
	float      FadeOut;                       // offset:  104
	int        ImageIndex;                    // offset:  108
	uint       Effects;                       // offset:  112
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
	float4x4   ViewInverted;                  // offset:   64
	float4x4   Projection;                    // offset:  128
	float4x4   ProjectionFPV;                 // offset:  192
	float4x4   ViewProjection;                // offset:  256
	float4x4   CascadeViewProjection0;        // offset:  320
	float4x4   CascadeViewProjection1;        // offset:  384
	float4x4   CascadeViewProjection2;        // offset:  448
	float4x4   CascadeViewProjection3;        // offset:  512
	float4x4   OcclusionGridMatrix;           // offset:  576
	float4     CascadeScaleOffset0;           // offset:  640
	float4     CascadeScaleOffset1;           // offset:  656
	float4     CascadeScaleOffset2;           // offset:  672
	float4     CascadeScaleOffset3;           // offset:  688
	float4     CameraForward;                 // offset:  704
	float4     CameraRight;                   // offset:  720
	float4     CameraUp;                      // offset:  736
	float4     CameraPosition;                // offset:  752
	float4     Gravity;                       // offset:  768
	float4     LightMapSize;                  // offset:  784
	float4     DirectLightDirection;          // offset:  800
	float4     DirectLightIntensity;          // offset:  816
	float4     SkyAmbientLevel;               // offset:  832
	float4     FogColor;                      // offset:  848
	float      FogAttenuation;                // offset:  864
	float      LinearizeDepthA;               // offset:  868
	float      LinearizeDepthB;               // offset:  872
	int        MaxParticles;                  // offset:  876
	float      DeltaTime;                     // offset:  880
	uint       DeadListSize;                  // offset:  884
	float      CocScale;                      // offset:  888
	float      CocBias;                       // offset:  892
	uint       IntegrationSteps;              // offset:  896
};

