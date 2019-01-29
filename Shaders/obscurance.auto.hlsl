static const int BlockSizeX = 4;
static const int BlockSizeY = 4;
static const int BlockSizeZ = 4;

// Fusion.Engine.Graphics.Lights.ObscuranceMap+BAKE_PARAMS
// Marshal.SizeOf = 192
struct BAKE_PARAMS {
	float4x4   ShadowViewProjection;          // offset:    0
	float4x4   OcclusionGridTransform;        // offset:   64
	float4     LightDirection;                // offset:  128
};

