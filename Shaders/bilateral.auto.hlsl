static const int BilateralBlockSizeX = 16;
static const int BilateralBlockSizeY = 16;

// Fusion.Engine.Graphics.BilateralFilter+FilterParams
// Marshal.SizeOf = 16
struct FilterParams {
	float      LinDepthScale;                 // offset:    0
	float      LinDepthBias;                  // offset:    4
	float      DepthFactor;                   // offset:    8
	float      ColorFactor;                   // offset:   12
};

