

#if 1
static const uint sampleCount = 12;
static const float3 poissonBeckmann[sampleCount]= {
	PoissonBeckmann(  0.25f,   0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.25f,  -0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.25f,  -0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.25f,   0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	
	PoissonBeckmann(  0.75f,   0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.75f,  -0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.75f,  -0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.75f,   0.25f,  KERNEL_SIZE,  ROUGHNESS ),
	
	PoissonBeckmann(  0.25f,   0.75f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.25f,  -0.75f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.25f,  -0.75f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.25f,   0.75f,  KERNEL_SIZE,  ROUGHNESS ),//*/

	// PoissonBeckmann( -0.6828758f,   0.5264853f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann( -0.9846674f,   0.1491582f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann( -0.3335175f,   0.1175671f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann( -0.1510262f,   0.9201540f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.0776904f,   0.4907993f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann( -0.6843108f,  -0.1940148f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.0070783f,  -0.1083425f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann( -0.5304128f,  -0.6343142f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.4250475f,   0.2877348f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.4858001f,   0.7253821f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.8246021f,   0.1354496f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.2029337f,  -0.4910559f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann( -0.1761186f,  -0.9231045f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.5713789f,  -0.2682010f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.3672436f,  -0.8677061f,  KERNEL_SIZE,  ROUGHNESS ),
	// PoissonBeckmann(  0.7550967f,  -0.6394721f,  KERNEL_SIZE,  ROUGHNESS ),
};
#else
static const uint sampleCount = 32;
static const float3 poissonBeckmann[sampleCount]= {
	PoissonBeckmann( -0.0099f,  -0.8555f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.3696f,  -0.5552f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.3186f,  -0.7533f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.2759f,  -0.8027f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.0563f,  -0.3613f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.2117f,  -0.4811f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.4346f,  -0.3196f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.3163f,   0.0196f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.6108f,  -0.5687f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.1760f,  -0.2182f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.4087f,  -0.0192f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.1477f,   0.0935f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.6555f,  -0.7307f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.8992f,  -0.4187f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.6640f,  -0.3913f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.6569f,  -0.1262f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.1998f,   0.3491f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.5268f,   0.3956f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.3180f,   0.6964f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.8838f,   0.0372f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.7099f,   0.6767f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.7338f,  -0.0873f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.5343f,   0.2804f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.2972f,   0.4673f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.0532f,   0.9108f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.3166f,  -0.2858f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.8590f,   0.3371f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.4409f,   0.7241f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.7408f,   0.6400f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.0927f,   0.6486f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.0885f,   0.2783f, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.8007f,   0.3416f, KERNEL_SIZE, ROUGHNESS ),
};
#endif
