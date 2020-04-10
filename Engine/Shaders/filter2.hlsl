#if 0
$ubershader RENDER_QUAD 
$ubershader RENDER_BORDER
$ubershader RENDER_SPOT
#endif

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

#if defined(RENDER_QUAD)

SamplerState	SamplerLinearClamp : register(s0);
Texture2D Source : register(t0);

cbuffer GaussWeightsCB : register(b0) {
	float4 scaleOffset;
};

struct PS_IN {
    float4 position : SV_POSITION;
  	float2 uv : TEXCOORD0;
};


PS_IN VSMain(uint VertexID : SV_VertexID)
{
	PS_IN output;
	
	output.position	=	GenerateQuadCoords( VertexID );
	output.uv 		=	output.position.xy;// * float2(0.5f, -0.5f) + 0.5f;
	output.uv		=	mad( output.uv, scaleOffset.xy, scaleOffset.zw );

	return output;
}


float4 PSMain(PS_IN input) : SV_Target
{
	return Source.SampleLevel(SamplerLinearClamp, input.uv, 0);
}

#endif

//-------------------------------------------------------------------------------

#if defined(RENDER_BORDER) || defined(RENDER_SPOT)

SamplerState	SamplerLinearClamp : register(s0);
Texture2D Source : register(t0);

cbuffer GaussWeightsCB : register(b0) {
	float4 targetSize;
};

struct PS_IN {
    float4 position : SV_POSITION;
	float2 projpos  : TEXCOORD0;
};


PS_IN VSMain(uint VertexID : SV_VertexID)
{
	PS_IN output;
	output.position	=	GenerateQuadCoords( VertexID );
	output.projpos	=	output.position.xy * (1 + targetSize.zw*8) * 1.0f;
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
		
	return value;

#endif
	
#ifdef RENDER_BORDER

	float 	x		=	input.projpos.x;
	float 	y		=	input.projpos.y;

	float	value	=	max( abs(x), abs(y) ) > 1 ? 0 : 1;
		
	return value;
	
#endif

	return float4(1,0,1,1);
}

#endif


