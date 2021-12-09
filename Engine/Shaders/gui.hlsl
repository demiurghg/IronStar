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

float4 VSMain(uint VertexID : SV_VertexID, out float2 uv : TEXCOORD0, out float2 uv2 : TEXCOORD1) : SV_POSITION
{
	float4 wpos = float4(0,0,0,0);
	uv = float2(0,0);
	
	float w = 0.5 * GUIData.Size.x / GUIData.DotsPerUnit;
	float h = 0.5 * GUIData.Size.y / GUIData.DotsPerUnit;
	
	float u	= GUIData.Size.x / MaxGuiWidth;
	float v	= GUIData.Size.y / MaxGuiHeight;
	
	if ( VertexID==0 ) { wpos = float4(-w,-h, 0, 1); uv = float2(0,v); uv2 = float2(0,1); }
	if ( VertexID==1 ) { wpos = float4( w, h, 0, 1); uv = float2(u,0); uv2 = float2(1,0); }
	if ( VertexID==2 ) { wpos = float4(-w, h, 0, 1); uv = float2(0,0); uv2 = float2(0,0); }

	if ( VertexID==3 ) { wpos = float4(-w,-h, 0, 1); uv = float2(0,v); uv2 = float2(0,1); }
	if ( VertexID==4 ) { wpos = float4( w,-h, 0, 1); uv = float2(u,v); uv2 = float2(1,1); }
	if ( VertexID==5 ) { wpos = float4( w, h, 0, 1); uv = float2(u,0); uv2 = float2(1,0); }
	
	float4 vpos	=	mul( wpos, GUIData.WorldTransform );
	float4 ppos	=	mul( vpos, Camera.ViewProjection );

	return ppos;
}


//------------------------------------------------------------------------------
//	Pixel Shader
//------------------------------------------------------------------------------


float4 PSMain(float4 position : SV_POSITION, float2 uv : TEXCOORD0, float2 uv2 : TEXCOORD1 ) : SV_Target
{
	float	glitchX		=	((GUIData.GlitchSeed / 715) % 715);
	float	glitchY		=	((GUIData.GlitchSeed % 715) % 715);
	float2	glitchUV	=	float2( glitchX, glitchY ) * GUIData.Size.zw * 8;
	float4 	glitch		=	GlitchTexture	.Sample( PointSampler, uv2 + glitchUV * 2 - 1 );
	
	float glitchLevel	=	0.0f;
	
	float w,h;
	GuiTexture.GetDimensions(w,h);
	float2 offset = float2(0.5f/w, 0.5f/h);

	float4 color		=	float4(0,0,0,0);
		   color.r 		=	GuiTexture  	.Sample( LinearSampler, uv + offset + float2(glitch.x - glitch.y * 0.1, -glitch.z) * glitchLevel ).r;
		   color.g 		=	GuiTexture  	.Sample( LinearSampler, uv + offset + float2(glitch.x - glitch.y * 0.2, -glitch.z) * glitchLevel ).g;
		   color.b 		=	GuiTexture  	.Sample( LinearSampler, uv + offset + float2(glitch.x - glitch.y * 0.3, -glitch.z) * glitchLevel ).b;
	float4 noiseTex		=	NoiseTexture	.Sample( LinearSampler, uv2 * GUIData.Size.xy / 256.0f ) * 2 - 1;
	float4 rgbTex		=	RgbTexture  	.Sample( LinearSampler, uv2 * GUIData.Size.xy / 1.0f );
	
	color = pow(abs(color), 2.2f);
	
	//color.rgb *= glitch.a;
	/*color.rgb += noiseTex.rgb * 0.01f;*/
	
	color.rgb *= rgbTex.rgb * 2;
	
	//color = glitch;
	
	//color.a = lerp(0.3f, 1.0f, color.a);
	return float4(2*color.rgb,1);
}









