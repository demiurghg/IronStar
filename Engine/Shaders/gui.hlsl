#if 0
$ubershader	DEFAULT
#endif

#include "auto/gui.fxi"

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
	float4 wpos = float4(0,0,0,0);
	uv = float2(0,0);
	
	if ( VertexID==0 ) { wpos = float4(-1.33f,-1, 0, 1); uv = float2(0,1); }
	if ( VertexID==1 ) { wpos = float4( 1.33f, 1, 0, 1); uv = float2(1,0); }
	if ( VertexID==2 ) { wpos = float4(-1.33f, 1, 0, 1); uv = float2(0,0); }

	if ( VertexID==3 ) { wpos = float4(-1.33f,-1, 0, 1); uv = float2(0,1); }
	if ( VertexID==4 ) { wpos = float4( 1.33f,-1, 0, 1); uv = float2(1,1); }
	if ( VertexID==5 ) { wpos = float4( 1.33f, 1, 0, 1); uv = float2(1,0); }
	
	float4 vpos	=	mul( wpos, GUIData.WorldTransform );
	float4 ppos	=	mul( vpos, Camera.ViewProjection );

	return ppos;
}


//------------------------------------------------------------------------------
//	Pixel Shader
//------------------------------------------------------------------------------


float4 PSMain(float4 position : SV_POSITION, float2 uv : TEXCOORD0 ) : SV_Target
{
	float4 color =	Texture.Sample( LinearSampler, uv );
	color = pow(color, 2.2f);
	//color.a = lerp(0.3f, 1.0f, color.a);
	return float4(color.rgb * 10,1);
}









