static const int BlockSizeX = 32;
static const int BlockSizeY = 32;

// Fusion.Engine.Graphics.SsaoFilter+Params
// Marshal.SizeOf = 512
struct Params {
	float4x4   ProjMatrix;                    // offset:    0
	float4x4   View;                          // offset:   64
	float4x4   ViewProj;                      // offset:  128
	float4x4   InvViewProj;                   // offset:  192
	float4x4   InvProj;                       // offset:  256
	float4     InputSize;                     // offset:  320
	float      CameraTangentX;                // offset:  336
	float      CameraTangentY;                // offset:  340
	float      LinDepthScale;                 // offset:  344
	float      LinDepthBias;                  // offset:  348
	float      PowerIntensity;                // offset:  352
	float      LinearIntensity;               // offset:  356
	float      FadeoutDistance;               // offset:  360
	float      DiscardDistance;               // offset:  364
	float      AcceptRadius;                  // offset:  368
	float      RejectRadius;                  // offset:  372
};

