static const int BLOCK_SIZE = 256;
static const int MAX_INJECTED = 4096;
static const int MAX_PARTICLES = 65536;
static const int MAX_IMAGES = 512;
static const int ParticleFX_Beam = 1;
static const int ParticleFX_Lit = 2;
static const int ParticleFX_LitShadow = 3;
static const int ParticleFX_Shadow = 4;
static const int LightTypeOmni = 1;
static const int LightTypeSpotShadow = 3;
static const int LightSpotShapeRound = 131072;
static const int LightSpotShapeSquare = 65536;

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
// Marshal.SizeOf = 116
struct LIGHT {
	float4x4   ViewProjection;                // offset:    0
	float4     PositionRadius;                // offset:   64
	float4     IntensityFar;                  // offset:   80
	float4     ShadowScaleOffset;             // offset:   96
	int        LightType;                     // offset:  112
};

// Fusion.Engine.Graphics.SceneRenderer+LIGHTINDEX
// Marshal.SizeOf = 8
struct LIGHTINDEX {
	uint       Offset;                        // offset:    0
	uint       Count;                         // offset:    4
};

// Fusion.Engine.Graphics.ParticleSystem+PARAMS
// Marshal.SizeOf = 256
struct PARAMS {
	float4x4   View;                          // offset:    0
	float4x4   Projection;                    // offset:   64
	float4     CameraForward;                 // offset:  128
	float4     CameraRight;                   // offset:  144
	float4     CameraUp;                      // offset:  160
	float4     CameraPosition;                // offset:  176
	float4     Gravity;                       // offset:  192
	float      LinearizeDepthA;               // offset:  208
	float      LinearizeDepthB;               // offset:  212
	int        MaxParticles;                  // offset:  216
	float      DeltaTime;                     // offset:  220
	uint       DeadListSize;                  // offset:  224
	float      CocScale;                      // offset:  228
	float      CocBias;                       // offset:  232
};

