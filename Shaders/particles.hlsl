
#if 0
$ubershader INITIALIZE|INJECTION|SIMULATION|DRAW|DRAW_SHADOW|DRAW_LIGHT|ALLOC_LIGHTMAP
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
SamplerState			Sampler			: 	register(s0);
SamplerComparisonState	ShadowSampler	: 	register(s1);

//-----------------------------------------------
//	SRVs :
//-----------------------------------------------
Texture2D					Texture 			: 	register(t0);
StructuredBuffer<Particle>	injectionBuffer		:	register(t1);
StructuredBuffer<Particle>	particleBufferGS	:	register(t2);
StructuredBuffer<float2>	sortParticleBufferGS:	register(t3);
StructuredBuffer<float4>	particleLighting	:	register(t4);
Texture2D					DepthValues			: 	register(t5);

Texture3D<uint2>			ClusterTable		: 	register(t7);
Buffer<uint>				LightIndexTable		: 	register(t8);
StructuredBuffer<LIGHT>		LightDataTable		:	register(t9);
Texture2D					ShadowMap			:	register(t10);
Texture2D					LightMap			:	register(t11);

#include "particles.lighting.hlsl"

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
#if (defined INJECTION) || (defined SIMULATION) || (defined INITIALIZE)
[numthreads( BLOCK_SIZE, 1, 1 )]
void CSMain( 
	uint3 groupID			: SV_GroupID,
	uint3 groupThreadID 	: SV_GroupThreadID, 
	uint3 dispatchThreadID 	: SV_DispatchThreadID,
	uint  groupIndex 		: SV_GroupIndex
)
{
	uint id = dispatchThreadID.x;
	
#ifdef INITIALIZE
	deadParticleIndices.Append(id);
#endif	

#ifdef INJECTION
	//	id must be less than max injected particles.
	//	dead list must contain at leas MAX_INJECTED indices to prevent underflow.
	if (id < (uint)Params.MaxParticles && Params.DeadListSize > (uint)MAX_INJECTED ) {
		Particle p = injectionBuffer[ id ];
		
		uint newIndex = deadParticleIndices.Consume();
		
		particleBuffer[ newIndex ] = p;
	}
#endif

#ifdef SIMULATION
	if (id < (uint)Params.MaxParticles) {
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
		
		float3 velocity		=	p.Velocity;
		float3 acceleration	=	p.Acceleration - velocity * length(velocity) * p.Damping;
		
		particleBuffer[ id ].Velocity	=	p.Velocity + acceleration * Params.DeltaTime;	
		particleBuffer[ id ].Position	=	p.Position + velocity     * Params.DeltaTime;	
		
		float4 ppPos	=	mul( mul( float4(particleBuffer[ id ].Position,1), Params.View ), Params.Projection );

		sortParticleBuffer[ id ] = float2( -abs(ppPos.z / ppPos.w), id );
	}
#endif
}
#endif



#ifdef ALLOC_LIGHTMAP
groupshared uint lmIndices[8] = {0,0,0,0, 0,0,0,0}; 

[numthreads( 1024, 1, 1 )]
void CSMain( 
	uint3 groupID			: SV_GroupID,
	uint3 groupThreadID 	: SV_GroupThreadID, 
	uint3 dispatchThreadID 	: SV_DispatchThreadID,
	uint  groupIndex 		: SV_GroupIndex
)
{
	for (uint base=0; base<64; base++) {
		int id = dispatchThreadID.x + base*1024;

		if (id < Params.MaxParticles) {
			Particle p = particleBuffer[ id ];

			p.LightmapRegion = 	0;
			
			float 	factor	=	saturate(p.TimeLag / p.LifeTime);
			float4 	projPos	=	mul( float4(p.Position.xyz,1), Params.ViewProjection );
			float  	size	=   lerp( p.Size0, p.Size1, factor ) / abs(projPos.w);
			uint	offset;
			uint 	bank;

			particleBuffer[ id ] = p;
			
			if ( p.TimeLag < 0 )			{ continue; }
			if ( p.TimeLag >= p.LifeTime ) 	{ continue; }
	
			for (int i=0; i<8; i++) {
				float minSize = (i==0)?     0 : 4*exp2(i-8);
				float maxSize = (i==7)? 99999 : 4*exp2(i-8+1);
				
				if ( size > minSize && size <= maxSize ) {
					InterlockedAdd( lmIndices[i], 1, offset );
					bank = i;
				}
			}
			
			uint 	regSize = min(32,1 << bank);
			uint 	count	= LightmapRegionSize / regSize;
			
			uint	baseX	= (bank % 4)*LightmapRegionSize;
			uint	baseY	= (bank / 4)*LightmapRegionSize;
			
			float x0 = ( baseX + regSize*(offset % count) + 0 )  * Params.LightMapSize.z;
			float y0 = ( baseY + regSize*(offset / count) + 0 )  * Params.LightMapSize.w;
			float x1 = ( baseX + regSize*(offset % count) + regSize )  * Params.LightMapSize.z;
			float y1 = ( baseY + regSize*(offset / count) + regSize )  * Params.LightMapSize.w;

			p.LightmapRegion = float4( x0,y0,x1,y1 );
			
			particleBuffer[ id ] = p;
		}
	}
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
	float2	LMCoord	  : TEXCOORD1;
	float4  ViewPosSZ : TEXCOORD2;
	float4	Color     : COLOR0;
	float	LMFactor  : TEXCOORD3;
};


#if (defined DRAW) || (defined DRAW_SHADOW) || (defined DRAW_LIGHT)
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
	
	float4 wpos0	=	float4( position + rt + up, 1 );
	float4 wpos1	=	float4( position - rt + up, 1 );
	float4 wpos2	=	float4( position - rt - up, 1 );
	float4 wpos3	=	float4( position + rt - up, 1 );
	
	float4 pos0		=	mul( wpos0, Params.View );
	float4 pos1		=	mul( wpos1, Params.View );
	float4 pos2		=	mul( wpos2, Params.View );
	float4 pos3		=	mul( wpos3, Params.View );
	
	if (prt.Effects==ParticleFX_Beam) {
		float3 dir	=	normalize(position - tailpos);
		float3 eye	=	normalize(Params.CameraPosition.xyz - tailpos);
		float3 side	=	normalize(cross( eye, dir ));
		pos0		=	mul( float4( tailpos  + side * sz, 1 ), Params.View );
        pos1		=	mul( float4( position + side * sz, 1 ), Params.View );
        pos2		=	mul( float4( position - side * sz, 1 ), Params.View );
	    pos3		=	mul( float4( tailpos  - side * sz, 1 ), Params.View );
	}
	
	float2 lmszA	 = Params.LightMapSize.zw * 0.0f;
	float2 lmszB	 = Params.LightMapSize.zw * float2(0.5f,-0.5f);
	
	p0.Position	 = mul( pos0, Params.Projection );
	p0.TexCoord	 = image.zy;
	p0.LMCoord	 = prt.LightmapRegion.zy + lmszA;
	p0.ViewPosSZ = float4( pos0.xyz, 1/sz );
	p0.Color 	 = color;
	p0.LMFactor	 = 0;
	
	p1.Position	 = mul( pos1, Params.Projection );
	p1.TexCoord	 = image.xy;
	p1.LMCoord	 = prt.LightmapRegion.xy + lmszA;
	p1.ViewPosSZ = float4( pos1.xyz, 1/sz );
	p1.Color 	 = color;
	p1.LMFactor	 = 0;
	
	p2.Position	 = mul( pos2, Params.Projection );
	p2.TexCoord	 = image.xw;
	p2.LMCoord	 = prt.LightmapRegion.xw + lmszA;
	p2.ViewPosSZ = float4( pos2.xyz, 1/sz );
	p2.Color 	 = color;
	p2.LMFactor	 = 0;
	
	p3.Position	 = mul( pos3, Params.Projection );
	p3.TexCoord	 = image.zw;
	p3.LMCoord	 = prt.LightmapRegion.zw + lmszA;
	p3.ViewPosSZ = float4( pos3.xyz, 1/sz );
	p3.Color 	 = color;
	p3.LMFactor	 = 0;
	
	#ifdef DRAW
	if (prt.Effects==ParticleFX_Lit || prt.Effects==ParticleFX_LitShadow) {
		p0.LMFactor	 = 1;
		p1.LMFactor	 = 1;
		p2.LMFactor	 = 1;
		p3.LMFactor	 = 1;
	}//*/
	#endif
	
	#ifdef DRAW_SHADOW
	if (prt.Effects!=ParticleFX_LitShadow && prt.Effects!=ParticleFX_Shadow) {
		return;
	}
	#endif
	
	#ifdef DRAW_LIGHT
		p0.Position	 = float4( float2(1,-1) * (p0.LMCoord.xy * 2 - 1), 0, 1 );
		p1.Position	 = float4( float2(1,-1) * (p1.LMCoord.xy * 2 - 1), 0, 1 );
		p2.Position	 = float4( float2(1,-1) * (p2.LMCoord.xy * 2 - 1), 0, 1 );
		p3.Position	 = float4( float2(1,-1) * (p3.LMCoord.xy * 2 - 1), 0, 1 );
		p0.ViewPosSZ = wpos0;
		p1.ViewPosSZ = wpos1;
		p2.ViewPosSZ = wpos2;
		p3.ViewPosSZ = wpos3;
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
		//clip( color.a < 0.001f ? -1:1 );
		
		float  depth 	= 	DepthValues.Load( int3(vpos.xy,0) ).r;
		float  a 		= 	Params.LinearizeDepthA;
		float  b        = 	Params.LinearizeDepthB;
		float  sceneZ   = 	1 / (depth * a + b);
		
		float  prtZ		= 	abs(input.ViewPosSZ.z);

		// TODO : profile soft particles clipping!
		//	May be using depth buffer instead? (copy required)
		// if (depth < vpos.z) {
		// clip(-1);
		// }
		float3 light		=	(input.LMFactor > 0.5f) ? LightMap.Sample( Sampler, input.LMCoord ).rgb : 1;
		
		float softFactor	=	saturate( (sceneZ - prtZ) * input.ViewPosSZ.w );

		color.rgba *= softFactor;
		color.rgb  *= light.rgb;
		color.rgb  *= color.a;
		
		return color;
	#endif
	
	#ifdef DRAW_SHADOW
		float4 textureColor	=	Texture.Sample( Sampler, input.TexCoord );
		float4 vertexColor  =  	input.Color;
		float4 color		=	1 - vertexColor.a * textureColor.a;
		
		return color;
	#endif
	
	#ifdef DRAW_LIGHT
		float3 lighting = ComputeClusteredLighting( input.ViewPosSZ.xyz );
		return float4(lighting,1);
	#endif
	
}
#endif

