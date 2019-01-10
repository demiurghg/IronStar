static const int NoiseSizeX = 64;
static const int NoiseSizeY = 64;
static const int BlockSizeX = 16;
static const int BlockSizeY = 16;
static const int NumHistogramBins = 256;
static const float Epsilon = 0.0002441406;

// Fusion.Engine.Graphics.HdrFilter+PARAMS
// Marshal.SizeOf = 128
struct PARAMS {
	float      AdaptationRate;                // offset:    0
	float      LuminanceLowBound;             // offset:    4
	float      LuminanceHighBound;            // offset:    8
	float      KeyValue;                      // offset:   12
	float      BloomAmount;                   // offset:   16
	float      DirtMaskLerpFactor;            // offset:   20
	float      DirtAmount;                    // offset:   24
	float      Saturation;                    // offset:   28
	float      DitherAmount;                  // offset:   32
	int        Width;                         // offset:   36
	int        Height;                        // offset:   40
	float      EVMin;                         // offset:   44
	float      EVMax;                         // offset:   48
	float      EVRange;                       // offset:   52
	float      EVRangeInverse;                // offset:   56
	float      AdaptEVMin;                    // offset:   60
	float      AdaptEVMax;                    // offset:   64
	uint       NoiseX;                        // offset:   68
	uint       NoiseY;                        // offset:   72
};

