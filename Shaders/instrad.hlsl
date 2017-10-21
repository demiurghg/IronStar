
#if 0
$ubershader LIGHTEN
$ubershader DRAW
#endif

#include "instrad.auto.hlsl"

//-----------------------------------------------------------------------------

#ifdef LIGHTEN
RWStructuredBuffer<SURFEL> Surfels : register(u0);
Texture2D		ShadowMap	:	register(t0);
Texture2D		LightMap	:	register(t1);

cbuffer CB1 : register(b0) { 
	LIGHTENPARAMS Params; 
};

[numthreads(BlockSize,1,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location		=	dispatchThreadId.xyz;
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
	clip( 1-length(input.Local.xy) );
	return float4(input.Color.rgb, 0.5);
}

#endif