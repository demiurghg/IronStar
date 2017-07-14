static const int BLOCK_SIZE = 256;
static const int MAX_INJECTED = 4096;
static const int MAX_PARTICLES = 65536;
static const int MAX_IMAGES = 512;
static const uint ParticleFX_Beam = 1;
static const uint ParticleFX_Lit = 2;
static const uint ParticleFX_LitShadow = 3;
static const uint ParticleFX_Shadow = 4;
static const uint LightmapRegionSize = 1024;
static const uint LightmapWidth = 4096;
static const uint LightmapHeight = 2048;
static const uint LightTypeOmni = 1;
static const uint LightTypeSpotShadow = 3;
static const uint LightSpotShapeRound = 131072;
static const uint LightSpotShapeSquare = 65536;

// Fusion.Engine.Graphics.Particle
// Marshal.SizeOf = 144
struct Particle {
	float3     Position;                      // offset:    0
	float3     Velocity;                      // offset:   12
	float3     Acceleration;                  // offset:   24
	float3     TailPosition;                  // offset:   36
	float4     Color0;                        // offset:   48
	float4     Color1;                        // offset:   64
	float4     LightmapRegion;                // offset:   80
	float      Gravity;                       // offset:   96
	float      Damping;                       // offset:  100
	float      Size0;                         // offset:  104
	float      Size1;                         // offset:  108
	float      Rotation0;                     // offset:  112
	float      Rotation1;                     // offset:  116
	float      LifeTime;                      // offset:  120
	float      TimeLag;                       // offset:  124
	float      FadeIn;                        // offset:  128
	float      FadeOut;                       // offset:  132
	int        ImageIndex;                    // offset:  136
	uint       Effects;                       // offset:  140
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

// Fusion.Engine.Graphics.SceneRenderer+LIGHTINDEX
// Marshal.SizeOf = 8
struct LIGHTINDEX {
	uint       Offset;                        // offset:    0
	uint       Count;                         // offset:    4
};

// Fusion.Engine.Graphics.ParticleSystem+PARAMS
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
	float      LinearizeDepthA;               // offset:  640
	float      LinearizeDepthB;               // offset:  644
	int        MaxParticles;                  // offset:  648
	float      DeltaTime;                     // offset:  652
	uint       DeadListSize;                  // offset:  656
	float      CocScale;                      // offset:  660
	float      CocBias;                       // offset:  664
};

