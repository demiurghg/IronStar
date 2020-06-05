
#if 0
$ubershader	COMPUTE_COC
$ubershader	EXTRACT
$ubershader BLUR BACKGROUND
$ubershader COMPOSE
#endif

#include "auto/dof.fxi"

float LinearizeDepth(float z)
{
	float a	=	Camera.LinearizeDepthScale;
	float b = 	Camera.LinearizeDepthBias;
	return 1.0f / (z * a + b);
}


/*------------------------------------------------------------------------------
	Compute circle of confusion
------------------------------------------------------------------------------*/

#ifdef COMPUTE_COC

[numthreads(BlockSize,BlockSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY		=	dispatchThreadId.xy;
	int2	storeXY		=	dispatchThreadId.xy;
	
	float 	depth		=	LinearizeDepth( DepthBuffer[ loadXY ].r );
	
	float	F			=	Dof.FocalLength;
	float	P			=	Dof.FocalDistance * 1000;
	float	A			=	Dof.ApertureDiameter;
	float	D			=	abs(depth) * 320.0f; // convert feet to milimeters
	
	float	coc_num		=	F * ( P - D );
	float	coc_denom	=	D * ( P - F );
	float 	coc			=	A * coc_num / coc_denom;
	
	float	coc_bg		=	max( 0,  coc  * Dof.PixelDensity );
	float	coc_fg		=	max( 0, -coc  * Dof.PixelDensity );
	
	CocTarget[ storeXY.xy ]	=	float4( coc_bg, coc_fg, 0, 0 );
}

#endif


/*------------------------------------------------------------------------------
	Extract Background/Foreground :
------------------------------------------------------------------------------*/

#ifdef EXTRACT

[numthreads(BlockSize,BlockSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY00	=	dispatchThreadId.xy * 2 + uint2(0,0);
	int2	loadXY01	=	dispatchThreadId.xy * 2 + uint2(0,1);
	int2	loadXY10	=	dispatchThreadId.xy * 2 + uint2(1,0);
	int2	loadXY11	=	dispatchThreadId.xy * 2 + uint2(1,1);
	int2	storeXY		=	dispatchThreadId.xy;
	
	float2	coc00		=	CocTexture[ loadXY00 ].rg;
	float2	coc01		=	CocTexture[ loadXY01 ].rg;
	float2	coc10		=	CocTexture[ loadXY10 ].rg;
	float2	coc11		=	CocTexture[ loadXY11 ].rg;
	
	float	epsilon		=	1 /  8192.0f;
	float2 	coc_weight	=	coc00 + coc01 + coc10 + coc11 + epsilon;
	
	float3	image00		=	HdrSource[ loadXY00 ].rgb;
	float3	image01		=	HdrSource[ loadXY01 ].rgb;
	float3	image10		=	HdrSource[ loadXY10 ].rgb;
	float3	image11		=	HdrSource[ loadXY11 ].rgb;
	
	float3	background	=	image00 * coc00.g
						+	image01 * coc01.g
						+	image10 * coc10.g
						+	image11 * coc11.g
						;
	
	float3	foreground	=	image00 * coc00.r
						+	image01 * coc01.r
						+	image10 * coc10.r
						+	image11 * coc11.r
						;
	
	Background[ storeXY	]	=	float4( background / coc_weight.g, 1 );
	Foreground[ storeXY	]	=	float4( foreground / coc_weight.r, 1 );
}

#endif


/*------------------------------------------------------------------------------
	COMPOSE :
------------------------------------------------------------------------------*/

#ifdef COMPOSE

[numthreads(BlockSize,BlockSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	float targetWidth;
	float targetHeight;
	
	HdrSource.GetDimensions( targetWidth, targetHeight );
	
	int2	loadXY		=	dispatchThreadId.xy;
	int2	storeXY		=	dispatchThreadId.xy;
	float2	loadUV		=	(loadXY + float2(0.5f,0.5f)) / float2( targetWidth, targetHeight );
	
	float2	coc			=	CocTexture[ loadXY ].rg;
	float3	hdrImage	=	HdrSource[ loadXY ].rgb;
	float3  bokehBG		=	BokehBackground.SampleLevel( LinearClamp, loadUV, 0 ).rgb;
	float3  bokehFG		=	BokehBackground.SampleLevel( LinearClamp, loadUV, 0 ).rgb;
	
	hdrImage	=	lerp( hdrImage, bokehBG, saturate( coc.g * 10 ) );
	
	HdrTarget[ storeXY	]	=	float4( hdrImage, 1 );
}

#endif

