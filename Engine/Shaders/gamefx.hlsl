#if 0
$ubershader	PAIN
#endif

#include "auto/gamefx.fxi"

//------------------------------------------------------------------------------
//	FSQuad
//------------------------------------------------------------------------------

float4 FSQuad( uint VertexID ) 
{
	return float4((VertexID == 0) ? 3.0f : -1.0f, (VertexID == 2) ? 3.0f : -1.0f, 1.0f, 1.0f);
}

float2 FSQuadUV ( uint VertexID )
{
	return float2((VertexID == 0) ? 2.0f : -0.0f, 1-((VertexID == 2) ? 2.0f : -0.0f));
}


//------------------------------------------------------------------------------
//	Vertex Shader
//------------------------------------------------------------------------------

float4 VSMain(uint VertexID : SV_VertexID, out float2 uv : TEXCOORD) : SV_POSITION
{
	uv = FSQuadUV( VertexID );
	return FSQuad( VertexID );
}


//------------------------------------------------------------------------------
//	Pixel Shader
//------------------------------------------------------------------------------

float2 RadialOffset( float2 input, float factor )
{
	return (input.xy - 0.5) / pow(factor,0.5f) + 0.5f;
}

float Triangle( float t )
{
	if (t<0.5) return t*2;
	else return 2-t*2;
}

float TriangleSharp( float t )
{
	return saturate(2 * Triangle(t)-1);
}

float4 PSMain(float4 position : SV_POSITION, float2 uv : TEXCOORD0 ) : SV_Target
{
	float	t0		=	frac(Params.Time);
	float	t1		=	frac(Params.Time + 0.25);
	float	t2		=	frac(Params.Time + 0.50);
	float	t3		=	frac(Params.Time + 0.75);
	float4 cloud0	=	CloudTex.SampleLevel( LinearClamp, RadialOffset(uv,1+4*t0), 0 ).r * TriangleSharp(t0);
	float4 cloud1	=	CloudTex.SampleLevel( LinearClamp, RadialOffset(uv,1+4*t1), 0 ).g * TriangleSharp(t1);
	float4 cloud2	=	CloudTex.SampleLevel( LinearClamp, RadialOffset(uv,1+4*t2), 0 ).b * TriangleSharp(t2);
	float4 cloud3	=	CloudTex.SampleLevel( LinearClamp, RadialOffset(uv,1+4*t3), 0 ).a * TriangleSharp(t3);

	float  cloud	=	(cloud0 + cloud1 + cloud2 + cloud3)/4;
	
	float4 source	=	Source.SampleLevel( LinearClamp, uv, 0 );

	float4 pain		=	PainTex.SampleLevel( LinearClamp, uv, 0 );
	
	//	apply pain effect :
	source.rgb	=	lerp( source.rgb, pain.rgb, pain.a * saturate(pow(Params.PainAmount,0.5)) );
	
	//	apply death effect :
	float blendFactor	=	saturate( Params.DeathFactor * 0.2f );
	//float fadeFactor	=	saturate( Params.DeathFactor * 0.1f - 1 ) * 0.5f;
	source.rgb	=	lerp( source.rgb, (source.rgb * pain.rgb), blendFactor );
	
	return source;
}









