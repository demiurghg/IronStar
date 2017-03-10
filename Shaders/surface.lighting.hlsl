
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#include "brdf.fxi"

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( PSInput input, Texture3D<uint2> clusterTable, float2 vpSize, float3 baseColor, float3 worldNormal, float roughness, float metallic )
{
	uint i,j,k;
	float3 result		=	float3(0,0,0);
	float slice			= 	1 - exp(-input.ProjPos.w*0.03);
	int3 loadUVW		=	int3( input.Position.xy/vpSize*float2(16,8), slice * 24 );
	
	uint2	data		=	clusterTable.Load( int4(loadUVW,0) ).rg;
	uint	index		=	data.r;
	uint 	lightCount	=	data.g & 0xFFFF;
	uint 	decalCount	=	data.g >> 16;
	
	float3 totalLight	=	0;

	float3 	worldPos	= 	input.WorldPos.xyz;
	float3 	normal 		=	normalize( worldNormal );
	
	float3	viewDir		=	Batch.ViewPos.xyz - worldPos.xyz;
	float	viewDistance=	length( viewDir );
	float3	viewDirN	=	normalize( viewDir );

	float	decalSlope		=	dot( viewDirN, normal );
	float	decalBaseMip	=	log2( input.ProjPos.w / decalSlope );

	//----------------------------------------------------------------------------------------------
	
	[loop]
	for (i=0; i<decalCount; i++) {
		uint idx = LightIndexTable.Load( lightCount + index + i );
		
		DECAL decal = DecalDataTable[idx];

		float4x4 decalMatrixI	=	decal.DecalMatrixInv;
		float3	 decalColor		=	decal.BaseColorMetallic.rgb;
		float3	 glowColor		=	decal.EmissionRoughness.rgb;
		float3	 decalR			=	decal.EmissionRoughness.a;
		float3	 decalM			=	decal.BaseColorMetallic.a;
		float4	 scaleOffset	=	decal.ImageScaleOffset;
		float	 falloff		=	decal.FalloffFactor;
		float 	 mipDecalBias	=	decal.MipBias;
		
		float4 decalPos	=	mul(float4(worldPos,1), decalMatrixI);
		
		if ( abs(decalPos.x)<1 && abs(decalPos.y)<1 && abs(decalPos.z)<1 && Batch.AssignmentGroup==decal.AssignmentGroup ) {
		
			//float2 uv			=	mad(mad(decalPos.xy, float2(-0.5,0.5), float2(0.5,0.5), offsetScale.zw, offsetScale.xy); 
			float2 uv			=	mad(decalPos.xy, scaleOffset.xy, scaleOffset.zw); 
		
			float4 decalImage	= 	DecalImages.SampleLevel( DecalSampler, uv, decalBaseMip + mipDecalBias );
			float3 localNormal  = 	decalImage.xyz * 2 - 1;
			float3 decalNormal	=	localNormal.x * decal.BasisX + localNormal.y * decal.BasisY - localNormal.z * decal.BasisZ;
			float factor		=	decalImage.a * saturate(falloff - abs(decalPos.z)*falloff);
			
			totalLight.rgb		+=	 glowColor * factor;
		
			baseColor 	= lerp( baseColor.rgb, decalColor, decal.ColorFactor * factor );
			roughness 	= lerp( roughness, decalR, decal.SpecularFactor * factor );
			metallic 	= lerp( metallic,  decalM, decal.SpecularFactor * factor );
			///normal		= lerp( normal, decalNormal, decal.NormalMapFactor * factor );

			normal		= localNormal;
		}
	}
	
	//----------------------------------------------------------------------------------------------

	float3	diffuse 	=	lerp( baseColor, float3(0,0,0), metallic );
	float3	specular  	=	lerp( float3(0.04f,0.04f,0.04f), baseColor, metallic );

	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<lightCount; i++) {
		uint idx = LightIndexTable.Load( index + i );
		float3 position		=	LightDataTable[idx].PositionRadius.xyz;
		float  radius		=	LightDataTable[idx].PositionRadius.w;
		float3 intensity	=	LightDataTable[idx].IntensityFar.rgb;
		
		float3 lightDir		= 	position - worldPos.xyz;
		float  falloff		= 	LinearFalloff( length(lightDir), radius );
		float  nDotL		= 	saturate( dot(normal, normalize(lightDir)) );
		
		totalLight.rgb 		+= 	falloff * Lambert ( normal.xyz,  lightDir, intensity, diffuse );
		totalLight.rgb 		+= 	falloff * nDotL * CookTorrance( normal.xyz, viewDirN, lightDir, intensity, specular, roughness );
	}
	
	return totalLight;
}

