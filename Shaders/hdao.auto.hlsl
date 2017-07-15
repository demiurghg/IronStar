
// Fusion.Engine.Graphics.SsaoFilter+Params
// Marshal.SizeOf = 512
struct Params {
	float4x4   ProjMatrix;                    // offset:    0
	float4x4   View;                          // offset:   64
	float4x4   ViewProj;                      // offset:  128
	float4x4   InvViewProj;                   // offset:  192
	float4x4   InvProj;                       // offset:  256
	float      PowerIntensity;                // offset:  320
	float      LinearIntensity;               // offset:  324
	float      FadeoutDistance;               // offset:  328
	float      DiscardDistance;               // offset:  332
	float      AcceptRadius;                  // offset:  336
	float      RejectRadius;                  // offset:  340
};

