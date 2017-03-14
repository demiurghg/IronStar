
#if 0
$ubershader INITIALIZE|INJECTION|SIMULATION|DRAW|DRAW_SHADOW|RENDER_LIGHTMAP|ALLOC_LIGHTMAP
#endif

#include "particles.auto.hlsl"

cbuffer CB1 : register(b0) { 
	PARAMS Params; 
};

cbuffer CB2 : register(b1) { 
	float4 Images[MAX_IMAGES]; 
};


//-----------------------------------------------
//	States :
//-----------------------------------------------
SamplerState	Sampler	 : 	register(s0);

//-----------------------------------------------
//	SRVs :
//-----------------------------------------------
Texture2D						Texture 				: 	register(t0);
StructuredBuffer<Particle>		injectionBuffer			:	register(t1);
StructuredBuffer<Particle>		particleBufferGS		:	register(t2);
StructuredBuffer<float2>		sortParticleBufferGS	:	register(t3);
StructuredBuffer<float4>		particleLighting		:	register(t4);
Texture2D						DepthValues				: 	register(t5);

//-----------------------------------------------
//	UAVs :
//-----------------------------------------------
RWStructuredBuffer<Particle>	particleBuffer		: 	register(u0);

#ifdef INJECTION
ConsumeStructuredBuffer<uint>	deadParticleIndices	: 	register(u1);
#endif
#if (defined SIMULATION) || (defined INITIALIZE)
AppendStructuredBuffer<uint>	deadParticleIndices	: 	register(u1);
#endif

RWStructuredBuffer<float2>		sortParticleBuffer	: 	register(u2);

/*-----------------------------------------------------------------------------
	Simulation :
-----------------------------------------------------------------------------*/
#if (defined INJECTION) || (defined SIMULATION) || (defined INITIALIZE) || (defined ALLOC_LIGHTMAP)
[numthreads( BLOCK_SIZE, 1, 1 )]
void CSMain( 
	uint3 groupID			: SV_GroupID,
	uint3 groupThreadID 	: SV_GroupThreadID, 
	uint3 dispatchThreadID 	: SV_DispatchThreadID,
	uint  groupIndex 		: SV_GroupIndex
)
{
	int id = dispatchThreadID.x;
	
#ifdef INITIALIZE
	deadParticleIndices.Append(id);
#endif	

#ifdef INJECTION
	//	id must be less than max injected particles.
	//	dead list must contain at leas MAX_INJECTED indices to prevent underflow.
	if (id < Params.MaxParticles && Params.DeadListSize > MAX_INJECTED ) {
		Particle p = injectionBuffer[ id ];
		
		uint newIndex = deadParticleIndices.Consume();
		
		particleBuffer[ newIndex ] = p;
	}
#endif

#ifdef SIMULATION
	if (id < Params.MaxParticles) {
		Particle p = particleBuffer[ id ];
		
		if (p.LifeTime>0) {
			if (p.TimeLag < p.LifeTime) {
				p.TimeLag += Params.DeltaTime;
			} else {
				p.LifeTime = -1;
				deadParticleIndices.Append( id );
			}
		}

		particleBuffer[ id ] = p;

		//	Measure distance :
		float  time		=	p.TimeLag;
		
		particleBuffer[ id ].Velocity	=	p.Velocity + p.Acceleration * Params.DeltaTime;	
		particleBuffer[ id ].Position	=	p.Position + p.Velocity     * Params.DeltaTime;	
		
		float4 ppPos	=	mul( mul( float4(particleBuffer[ id ].Position,1), Params.View ), Params.Projection );

		sortParticleBuffer[ id ] = float2( -abs(ppPos.z / ppPos.w), id );
	}
#endif

#ifdef ALLOC_LIGHTMAP
	if (id < Params.MaxParticles) {
		Particle p = particleBuffer[ id ];

		//	Measure distance :
		float  time		=	p.TimeLag;
		
		particleBuffer[ id ].Velocity	=	p.Velocity + p.Acceleration * Params.DeltaTime;	
		particleBuffer[ id ].Position	=	p.Position + p.Velocity     * Params.DeltaTime;	
		
		particleBuffer[ id ] = p;
	}
#endif
}
#endif


/*-----------------------------------------------------------------------------
	Rendering :
-----------------------------------------------------------------------------*/

struct VSOutput {
	int vertexID : TEXCOORD0;
};

struct GSOutput {
	float4	Position  : SV_Position;
	float2	TexCoord  : TEXCOORD0;
	float4  ViewPosSZ : TEXCOORD1;
	float4	Color     : COLOR0;
};


#if (defined DRAW) || (defined DRAW_SHADOW) || (defined RENDER_LIGHTMAP)
VSOutput VSMain( uint vertexID : SV_VertexID )
{
	VSOutput output;
	output.vertexID = vertexID;
	return output;
}


float Ramp(float f_in, float f_out, float t) 
{
	float y = 1;
	t = saturate(t);
	
	float k_in	=	1 / f_in;
	float k_out	=	-1 / (1-f_out);
	float b_out =	-k_out;	
	
	if (t<f_in)  y = t * k_in;
	if (t>f_out) y = t * k_out + b_out;
	
	return y;
}



[maxvertexcount(6)]
void GSMain( point VSOutput inputPoint[1], inout TriangleStream<GSOutput> outputStream )
{
	GSOutput p0, p1, p2, p3;
	
	uint prtId = (uint)( sortParticleBufferGS[ inputPoint[0].vertexID ].y );
	//uint prtId = inputPoint[0].vertexID;
	
	Particle prt = particleBufferGS[ prtId ];
	
	if (prt.TimeLag<0) {
		return;
	}
	
	if (prt.TimeLag >= prt.LifeTime ) {
		return;
	}
	
	float time		=	prt.TimeLag;
	float factor	=	saturate(prt.TimeLag / prt.LifeTime);
	
	float  sz 		=   lerp( prt.Size0, prt.Size1, factor )/2;
	float4 color	=	lerp( prt.Color0, prt.Color1, Ramp( prt.FadeIn, prt.FadeOut, factor ) );
	float3 position	=	prt.Position    ;// + prt.Velocity * time + accel * time * time / 2;
	float3 tailpos	=	prt.TailPosition;// + prt.Velocity * time + accel * time * time / 2;
	float  a		=	lerp( prt.Rotation0, prt.Rotation1, factor );	

	float2x2	m	=	float2x2( cos(a), sin(a), -sin(a), cos(a) );
	
	float3		rt	=	(Params.CameraRight.xyz * cos(a) + Params.CameraUp.xyz * sin(a)) * sz;
	float3		up	=	(Params.CameraUp.xyz * cos(a) - Params.CameraRight.xyz * sin(a)) * sz;
	
	float4		image	=	Images[prt.ImageIndex ];
	
	float4 pos0	=	mul( float4( position + rt + up, 1 ), Params.View );
	float4 pos1	=	mul( float4( position - rt + up, 1 ), Params.View );
	float4 pos2	=	mul( float4( position - rt - up, 1 ), Params.View );
	float4 pos3	=	mul( float4( position + rt - up, 1 ), Params.View );
	
	if (prt.Effects==ParticleFX_Beam) {
		float3 dir	=	normalize(position - tailpos);
		float3 eye	=	normalize(Params.CameraPosition.xyz - tailpos);
		float3 side	=	normalize(cross( eye, dir ));
		pos0		=	mul( float4( tailpos  + side * sz, 1 ), Params.View );
        pos1		=	mul( float4( position + side * sz, 1 ), Params.View );
        pos2		=	mul( float4( position - side * sz, 1 ), Params.View );
	    pos3		=	mul( float4( tailpos  - side * sz, 1 ), Params.View );
	}
	
	p0.Position	 = mul( pos0, Params.Projection );
	p0.TexCoord	 = float2(image.z, image.y);
	p0.ViewPosSZ = float4( pos0.xyz, 1/sz );
	p0.Color 	 = color;
	
	p1.Position	 = mul( pos1, Params.Projection );
	p1.TexCoord	 = float2(image.x, image.y);
	p1.ViewPosSZ = float4( pos1.xyz, 1/sz );
	p1.Color 	 = color;
	
	p2.Position	 = mul( pos2, Params.Projection );
	p2.TexCoord	 = float2(image.x, image.w);
	p2.ViewPosSZ = float4( pos2.xyz, 1/sz );
	p2.Color 	 = color;
	
	p3.Position	 = mul( pos3, Params.Projection );
	p3.TexCoord	 = float2(image.z, image.w);
	p3.ViewPosSZ = float4( pos3.xyz, 1/sz );
	p3.Color 	 = color;
	
	#ifdef DRAW
	if (prt.Effects==ParticleFX_Lit || prt.Effects==ParticleFX_LitShadow) {
		p0.Color.rgba	*= ( particleLighting[ prtId ].rgba );
		p1.Color.rgba	*= ( particleLighting[ prtId ].rgba );
		p2.Color.rgba	*= ( particleLighting[ prtId ].rgba );
		p3.Color.rgba	*= ( particleLighting[ prtId ].rgba );
		p0.Color.rgb 	*= ( particleLighting[ prtId ].a );
		p1.Color.rgb 	*= ( particleLighting[ prtId ].a );
		p2.Color.rgb 	*= ( particleLighting[ prtId ].a );
		p3.Color.rgb 	*= ( particleLighting[ prtId ].a );
	}//*/
	#endif
	
	#ifdef DRAW_SHADOW
	if (prt.Effects!=ParticleFX_LitShadow && prt.Effects!=ParticleFX_Shadow) {
		return;
	}
	#endif
	
	outputStream.Append(p0);
	outputStream.Append(p1);
	outputStream.Append(p3);
	outputStream.Append(p2);
}


//	Soft particles :
//	http://developer.download.nvidia.com/SDK/10/direct3d/Source/SoftParticles/doc/SoftParticles_hi.pdf

float4 PSMain( GSOutput input, float4 vpos : SV_POSITION ) : SV_Target
{
	#ifdef DRAW
		float4 color	=	Texture.Sample( Sampler, input.TexCoord ) * input.Color;
		//	saves about 5%-10% of rasterizer time:
		clip( color.a < 0.001f ? -1:1 );
		
		float  depth 	= DepthValues.Load( int3(vpos.xy,0) ).r;
		float  a 		= Params.LinearizeDepthA;
		float  b        = Params.LinearizeDepthB;
		float  sceneZ   = 1 / (depth * a + b);
		
		float  prtZ		= abs(input.ViewPosSZ.z);

		// TODO : profile soft particles clipping!
		// if (depth < vpos.z) {
		// clip(-1);
		// }
		
		float softFactor	=	saturate( (sceneZ - prtZ) * input.ViewPosSZ.w );

		color.rgba *= softFactor;
		
		return color;
	#endif
	
	#ifdef DRAW_SHADOW
		float4 textureColor	=	Texture.Sample( Sampler, input.TexCoord );
		float4 vertexColor  =  	input.Color;
		float4 color		=	1 - vertexColor.a * textureColor.a;
		
		return color;
	#endif
	
	#ifdef RENDER_LIGHTMAP
		float4 textureColor	=	Texture.Sample( Sampler, input.TexCoord );
		float4 vertexColor  =  	input.Color;
		float4 color		=	1 - vertexColor.a * textureColor.a;
		
		return color;
	#endif
	
}
#endif

