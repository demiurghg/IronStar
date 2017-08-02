static const int BlockSizeX = 32;
static const int BlockSizeY = 32;
static const int InterleaveBlockSizeX = 16;
static const int InterleaveBlockSizeY = 16;
static const int BilateralBlockSizeX = 16;
static const int BilateralBlockSizeY = 16;
static const int PatternSize = 256;

// Fusion.Engine.Graphics.SsaoFilter+HdaoParams
// Marshal.SizeOf = 80
struct HdaoParams {
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
	float      Dummy0;                        // offset:   60
	int2       WriteOffset;                   // offset:   64
};

// Fusion.Engine.Graphics.SsaoFilter+FilterParams
// Marshal.SizeOf = 16
struct FilterParams {
	float      LinDepthScale;                 // offset:    0
	float      LinDepthBias;                  // offset:    4
	float      DepthFactor;                   // offset:    8
	float      NormalFactor;                  // offset:   12
};

