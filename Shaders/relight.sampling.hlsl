

#if 0
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
static const float3 poissonBeckmann[32]= {
	PoissonBeckmann(  0.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.75,   0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  1.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.75,   0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  1.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.25,   1.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.75,   1.25, KERNEL_SIZE, ROUGHNESS ),

	PoissonBeckmann( -0.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.75,   0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -1.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.75,   0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -1.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.25,   1.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.75,   1.25, KERNEL_SIZE, ROUGHNESS ),

	PoissonBeckmann( -0.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.75,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -1.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.75,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -1.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.24,  -1.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann( -0.75,  -1.25, KERNEL_SIZE, ROUGHNESS ),

	PoissonBeckmann(  0.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.75,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  1.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.75,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  1.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.25,  -1.25, KERNEL_SIZE, ROUGHNESS ),
	PoissonBeckmann(  0.75,  -1.25, KERNEL_SIZE, ROUGHNESS ),
};
#endif
