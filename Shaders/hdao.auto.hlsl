static const int BlockSizeX = 32;
static const int BlockSizeY = 32;

// Fusion.Engine.Graphics.SsaoFilter+Params
// Marshal.SizeOf = 64
struct Params {
	float4     InputSize;                     // offset:    0
	float      CameraTangentX;                // offset:   16
	float      CameraTangentY;                // offset:   20
	float      LinDepthScale;                 // offset:   24
	float      LinDepthBias;                  // offset:   28
	float      PowerIntensity;                // offset:   32
	float      LinearIntensity;               // offset:   36
	float      FadeoutDistance;               // offset:   40
	float      DiscardDistance;               // offset:   44
	float      AcceptRadius;                  // offset:   48
	float      RejectRadius;                  // offset:   52
	float      RejectRadiusRcp;               // offset:   56
};

