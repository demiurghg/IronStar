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
	float      MaximumOutputValue;            // offset:   32
	float      MinimumOutputValue;            // offset:   36
	float      DitherAmount;                  // offset:   40
	int        Width;                         // offset:   44
	int        Height;                        // offset:   48
	float      MinLogLuminance;               // offset:   52
	float      MaxLogLuminance;               // offset:   56
	float      OneOverLogLuminanceRange;      // offset:   60
	float      LogLuminanceRange;             // offset:   64
};

