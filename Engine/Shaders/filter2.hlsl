#if 0
$ubershader CLEAR|COPY COLOR|DEPTH
$ubershader RENDER_BORDER
$ubershader RENDER_SPOT
#endif

#include "auto/filter2.fxi"

//-------------------------------------------------------------------------------

float4 GenerateQuadCoords( int vertexID )
{
	if (vertexID==0) return float4( -1, -1, 0.5f, 1 );
	if (vertexID==1) return float4( -1,  1, 0.5f, 1 );
	if (vertexID==2) return float4(  1, -1, 0.5f, 1 );
	if (vertexID==3) return float4(  1,  1, 0.5f, 1 );
	return float4(0,0,0,0);
}

//-------------------------------------------------------------------------------

#if defined(COPY) || defined(CLEAR)

struct PS_IN 
{
    float4 position : SV_POSITION;
  	float2 uv : TEXCOORD0;
};


PS_IN VSMain(uint VertexID : SV_VertexID)
{
	PS_IN output;
	
	output.position	=	GenerateQuadCoords( VertexID );
	output.uv 		=	output.position.xy;// * float2(0.5f, -0.5f) + 0.5f;
	output.uv		=	mad( output.uv, CData.ScaleOffset.xy, CData.ScaleOffset.zw );

	return output;
}


float4 PSMain(PS_IN input) : SV_Target
{
#ifdef COLOR	
	#ifdef COPY
		return Source.SampleLevel(SamplerLinearClamp, input.uv, 0) * CData.Color;
	#endif
	#ifdef CLEAR
		return CData.Color;
	#endif
#endif	
#ifdef DEPTH	
	#ifdef COPY
		return Source.SampleLevel(SamplerLinearClamp, input.uv, 0).rrrr;
	#endif
	#ifdef CLEAR
		return CData.Color.r;
	#endif
#endif	
}

#endif

//-------------------------------------------------------------------------------

#if defined(RENDER_BORDER) || defined(RENDER_SPOT)

struct PS_IN {
    float4 position : SV_POSITION;
	float2 projpos  : TEXCOORD0;
};


PS_IN VSMain(uint VertexID : SV_VertexID)
{
	PS_IN output;
	output.position	=	GenerateQuadCoords( VertexID );
	output.projpos	=	output.position.xy * (1 + CData.TargetSize.zw*8) * 1.0f;
	return output;
}


float4 PSMain(PS_IN input) : SV_Target
{
	float2 	vpos = input.position.xy;
	float4 	zero = float4(0,0,0,0);
	float4 	one  = float4(1,1,1,1);
	
#ifdef RENDER_SPOT

	float	value;
	value 	= 	saturate( 1-length(input.projpos.xy) );
	value 	*=	value;
		
	return value * CData.Color;

#endif
	
#ifdef RENDER_BORDER

	float 	x		=	input.projpos.x;
	float 	y		=	input.projpos.y;

	float	value	=	max( abs(x), abs(y) ) > 1 ? 0 : 1;
		
	return value * CData.Color;
	
#endif

	return float4(1,0,1,1);
}

#endif


