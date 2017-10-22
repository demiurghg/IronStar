
#if 0
$ubershader LIGHTEN
$ubershader DRAW
$ubershader LIGHTMAP
#endif

#include "instrad.auto.hlsl"

//-----------------------------------------------------------------------------

#ifdef LIGHTEN
RWStructuredBuffer<SURFEL> Surfels : register(u0);
Texture2D		ShadowMap	:	register(t0);
Texture2D		LightMap	:	register(t1);
SamplerComparisonState ShadowSampler : register(s0);

cbuffer CB1 : register(b0) { 
	LIGHTENPARAMS params; 
};


float ProjectShadow ( float3 worldPos, float4x4 viewProjection, out float4 projection )
{	
	float4 temp = 	mul( float4(worldPos,1), viewProjection );
	temp.xy 	/= 	temp.w;
	temp.w   	= 	1;
	
	projection	=	temp;
	
	return	max(abs(projection.x), abs(projection.y));//length(temp.xy);
}


[numthreads(BlockSize,1,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int location		=	dispatchThreadId.x;

	SURFEL surfel =	Surfels[ location ];
	
	float4	projection		= float4(0,0,0,0);
	float4	bestProjection 	= float4(0,0,0,0);
	float4	bestScaleOffset = float4(0,0,0,0);
	
	float	bias			= 0.95;
	float	fade			= 1;

	//------------------------------------------------------
	//	select cascade :
	
	float3 worldPos = surfel.Position + surfel.NormalArea.xyz * 1.0f;
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection3, projection ) < 1 ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset3;
	}
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection2, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset2;
	}
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection1, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset1;
	}
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection0, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset0;
	}
	
	//------------------------------------------------------
	
	float2	uv				=	mad( bestProjection.xy, bestScaleOffset.xy, bestScaleOffset.zw );
	float   depthcmp		= 	projection.z;
	float3	shadow			=	ShadowMap.SampleCmpLevelZero( ShadowSampler, uv, depthcmp );
	
	float3	normal			=	normalize( surfel.NormalArea.xyz );
	float	area			=	surfel.NormalArea.w;
	float	lightDir		=	-normalize(params.DirectLightDirection);
	
	float3	lighting		=	params.DirectLightIntensity * max(0, dot( normal, lightDir )) * area;
	
	surfel.Intensity.rgb	=	shadow * lighting;
	
	Surfels[ location ] = surfel;
}

#endif

//-----------------------------------------------------------------------------

#ifdef LIGHTMAP

StructuredBuffer<SURFEL> Surfels : register(t0);
RWTexture3D<float4>		LightMap		:	register(t0);

cbuffer CB1 : register(b0) { 
	LMPARAMS params; 
};


[numthreads(BlockSize3D,BlockSize3D,BlockSize3D)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location		=	dispatchThreadId.xyz;
	
	float3 position		=	location.xyz - float3(32,32,32);
	float3 light		=	float3(0,0,0);
	
	for (int i=0; i<1024*5; i++) {
	
		SURFEL surfel = Surfels[i];
		float3	n	=	surfel.NormalArea.xyz;
		float	a	=	surfel.NormalArea.w;
		float3 	p	=	surfel.Position.xyz;
		float3	lt	=	surfel.Intensity.rgb;
		
		float3	d	=	position - p;
		float3	r	=	length(d);
				d	=	normalize(d);
		
		light	+=	lt * max( 0, dot( d, n ) ) * a / (r*r+a);
	
	}
	
	LightMap[ location ] = float4(light,0);

}

#endif

//-----------------------------------------------------------------------------

#ifdef DRAW

cbuffer CB1 : register(b0) { 
	DRAWPARAMS Params; 
};

struct VSOutput {
	int vertexID : TEXCOORD0;
};

struct GSOutput {
	float4	Position  : SV_Position;
	float4	Color     : COLOR0;
	float2	Local	  : TEXCOORD0;
};


VSOutput VSMain( uint vertexID : SV_VertexID )
{
	VSOutput output;
	output.vertexID = vertexID;
	return output;
}

StructuredBuffer<SURFEL> Surfels : register(t0);

[maxvertexcount(6)]
void GSMain( point VSOutput inputPoint[1], inout TriangleStream<GSOutput> outputStream )
{
	GSOutput p0, p1, p2, p3;
	
	SURFEL surfel =	Surfels[ inputPoint[0].vertexID ];
	
	float radius	=	sqrt(surfel.NormalArea.w/3.1415);
	
	float3	axis	=	normalize( surfel.NormalArea.xyz );
	float3 	rt		=	cross( axis, float3(0,1,0) );

	if (length(rt)<0.01f) {
		rt	=	cross( axis, float3(1,0,0) );
	}
	
	float3 up	=	cross( rt, axis );
	
	rt = normalize( rt ) * radius;
	up = normalize( up ) * radius;
	
	p0.Position	=	mul( surfel.Position + float4(  rt + up + axis * 0.1f, 0 ), Params.ViewProjection );
	p1.Position	=	mul( surfel.Position + float4( -rt + up + axis * 0.1f, 0 ), Params.ViewProjection );
	p2.Position	=	mul( surfel.Position + float4( -rt - up + axis * 0.1f, 0 ), Params.ViewProjection );
	p3.Position	=	mul( surfel.Position + float4(  rt - up + axis * 0.1f, 0 ), Params.ViewProjection );
	
	p0.Color	=	surfel.Intensity;
	p1.Color	=	surfel.Intensity;
	p2.Color	=	surfel.Intensity;
	p3.Color	=	surfel.Intensity;
	
	p0.Local	=	float2( 1, 1 );
	p1.Local	=	float2(-1, 1 );
	p2.Local	=	float2(-1,-1 );
	p3.Local	=	float2( 1,-1 );
	
	outputStream.Append(p0);
	outputStream.Append(p1);
	outputStream.Append(p3);
	outputStream.Append(p2);
}


float4 PSMain( GSOutput input, float4 vpos : SV_POSITION ) : SV_Target
{
	clip( 1-2*length(input.Local.xy) );
	return float4(input.Color.rgb, 0.5);
}

#endif