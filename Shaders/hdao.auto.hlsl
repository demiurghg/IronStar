static const int BlockSizeX = 32;
static const int BlockSizeY = 32;
static const int InterleaveBlockSizeX = 16;
static const int InterleaveBlockSizeY = 16;
static const int PatternSize = 256;

// Fusion.Engine.Graphics.SsaoFilter+HdaoParams
// Marshal.SizeOf = 80
struct HdaoParams {
	float4     InputSize;                     // offset:    0
	float      CameraTangentX;                // offset:   16
	float      CameraTangentY;                // offset:   20
	float      LinDepthScale;                 // offset:   24
	float      LinDepthBias;                  // offset:   28
	float      FadeoutDistance;               // offset:   32
	float      DiscardDistance;               // offset:   36
	float      RejectRadius;                  // offset:   40
	float      RejectRadiusRcp;               // offset:   44
	int2       WriteOffset;                   // offset:   48
};

