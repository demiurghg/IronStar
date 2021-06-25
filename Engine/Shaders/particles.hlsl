
#if 0
$ubershader COMPUTE INITIALIZE|INJECTION|SIMULATION|ALLOC_LIGHTMAP
$ubershader DRAW SOFT|HARD|DUDV|VELOCITY|HARD_SHADOW|SOFT_SHADOW|LIGHTMAP
#endif

#include "auto/particles.fxi"
#include "gamma.fxi"

#include "ls_fog.fxi"

//-----------------------------------------------
//	UAVs :
//-----------------------------------------------

void TransformVector ( float4x4 transform, inout float3 v )
{
	v.xyz = mul( float4(v,0), transform ).xyz;
}

void TransformPoint ( float4x4 transform, inout float3 p )
{
	p.xyz = mul( float4(p,1), transform ).xyz;
}

bool IsWeaponFX		( Particle p ) { return (p.FXData & 0xFF000000) >> 24; } 
uint GetParticleFX	( Particle p ) { return (p.FXData & 0x000000FF) >>  0; } 

uint 	GetImageCount( Particle p ) { return ((p.FadingImageIndexCount & 0xFF000000) >> 24); }
uint 	GetImageIndex( Particle p ) { return ((p.FadingImageIndexCount & 0x00FF0000) >> 16); }
float 	GetFadeIn	 ( Particle p ) { return ((p.FadingImageIndexCount & 0x000000FF) >>  0) / 255.0f; }
float 	GetFadeOut	 ( Particle p ) { return ((p.FadingImageIndexCount & 0x0000FF00) >>  8) / 255.0f; }


float4 GetColor4( Particle p )
{
	uint  c = p.ColorPacked;
	float b = ((c >> 0) & 0xFF) / 255.0f;
	float g = ((c >> 8) & 0xFF) / 255.0f;
	float r = ((c >>16) & 0xFF) / 255.0f;
	float a = ((c >>24) & 0xFF) / 255.0f;
	return SRGBToLinear(float4(r,g,b,a));
}

float GetExposure	( Particle p ) { return ((p.MaterialERMS >>  0) & 0xFF) / 255.0f; }
float GetRoughness	( Particle p ) { return ((p.MaterialERMS >>  8) & 0xFF) / 255.0f; }
float GetMetallic	( Particle p ) { return ((p.MaterialERMS >> 16) & 0xFF) / 255.0f; }
float GetScattering	( Particle p ) { return ((p.MaterialERMS >> 24) & 0xFF) / 255.0f; }

float GetBeamFactor	( Particle p ) { return f16tof32( p.IntensityBeamFactor >> 16 ); }
float GetIntensity 	( Particle p ) { return f16tof32( p.IntensityBeamFactor >>  0 ); }
float GetDamping 	( Particle p ) { return f16tof32( p.GravityDamping >> 16 ); }
float GetGravity	( Particle p ) { return f16tof32( p.GravityDamping >>  0 ); }

float GetSize0 		( Particle p ) { return f16tof32( p.Size01 >> 0 ); }
float GetSize1 		( Particle p ) { return f16tof32( p.Size01 >> 16 ); }
float GetRotation0 	( Particle p ) { return f16tof32( p.Rotation01 >> 0 ); }
float GetRotation1 	( Particle p ) { return f16tof32( p.Rotation01 >> 16 ); }

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
	deadParticleIndicesPush.Append(id);
#endif	

#ifdef INJECTION
	//	id must be less than max injected particles.
	//	dead list must contain at leas MAX_INJECTED indices to prevent underflow.
	if (id < (uint)Params.MaxParticles && Params.DeadListSize > (uint)MAX_INJECTED ) 
	{
		Particle p = injectionBuffer[ id ];
		
		if (IsWeaponFX(p)) 
		{
			TransformVector( Camera.View, p.Velocity );
			TransformPoint ( Camera.View, p.Position );//*/
		}
		
		uint newIndex = deadParticleIndicesPull.Consume();
		
		particleBuffer[ newIndex ] = p;
	}
#endif

#ifdef SIMULATION
	sortParticleBuffer[ id ] = float2(0,0);

	if (id < (uint)Params.MaxParticles) {
		Particle p = particleBuffer[ id ];

		if (p.LifeTime>0) {
			if (p.TimeLag < p.LifeTime) {
				p.TimeLag += Params.DeltaTime * Params.IntegrationSteps;
			} else {
				p.LifeTime = -1;
				deadParticleIndicesPush.Append( id );
			}
		}

		particleBuffer[ id ] = p;

		//	Integrate kinematics :
		float  time		=	p.TimeLag;
		
		float3 gravity		=	-Params.Gravity.xyz * GetGravity(p);
		float3 position		=	p.Position;
		float3 velocity		=	p.Velocity;
		float3 acceleration	=	0;
		
		for (uint i=0; i<Params.IntegrationSteps; i++) 
		{
			acceleration	=	(-1.0f) * velocity * length(velocity) * GetDamping(p) + gravity;
			velocity		=	velocity + acceleration * Params.DeltaTime;	
			position		=	position + velocity     * Params.DeltaTime;	
		}

		if (GetBeamFactor(p)>=0) {
			particleBuffer[ id ].Velocity	=	velocity;	
			particleBuffer[ id ].Position	=	position;	
		}
		
		//	Measure distance :
		float  pDist	=	distance( position.xyz, Camera.CameraPosition.xyz );
		
		float   pKey	=	(p.TimeLag > p.LifeTime) ? 0 : -abs(pDist);
		//float 	pKey	=	wang_hash( id + (int)(Params.CameraPosition.x) );
		float	pValue	=	id;
		
		sortParticleBuffer[ id ] = float2( pKey, pValue );
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
			
			lightMapRegions[id] = 	float4(0,0,0,0);
			
			float 	factor	=	saturate(p.TimeLag / p.LifeTime);
			float4 	projPos	=	mul( float4(p.Position.xyz,1), Camera.ViewProjection );
			float  	size	=   lerp( GetSize0(p), GetSize1(p), factor ) / abs(projPos.w);
			uint	offset;
			uint 	bank;

			if ( p.TimeLag < 0 )			{ continue; }
			if ( p.TimeLag >= p.LifeTime ) 	{ continue; }
	
			for (int i=0; i<8; i++) {
				float minSize = (i==0)?     0 : 1*exp2(i-8);
				float maxSize = (i==7)? 99999 : 1*exp2(i-8+1);
				
				if ( size > minSize && size <= maxSize ) {
					InterlockedAdd( lmIndices[i], 1, offset );
					bank = i;
				}
			}
			
			uint 	regSize = min(16,1 << bank);
			uint 	count	= LightmapRegionSize / regSize;
			
			uint	baseX	= (bank % 4)*LightmapRegionSize;
			uint	baseY	= (bank / 4)*LightmapRegionSize;
			
			float x0 = ( baseX + regSize*(offset % count) + 0 )  * Params.LightMapSize.z;
			float y0 = ( baseY + regSize*(offset / count) + 0 )  * Params.LightMapSize.w;
			float x1 = ( baseX + regSize*(offset % count) + regSize )  * Params.LightMapSize.z;
			float y1 = ( baseY + regSize*(offset / count) + regSize )  * Params.LightMapSize.w;

			lightMapRegions[id]  = float4( x0,y0,x1,y1 );
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
	float3	Normal	  : NORMAL;
	float2	TexCoord  : TEXCOORD0;
	float2	LMCoord	  : TEXCOORD1;
	float4  ViewPosSZ : TEXCOORD2;
	float4	Color     : COLOR0;
	float4	Fog	      : COLOR1;
	float	LMFactor  : TEXCOORD3;
	float4	FogSRM	  : TEXCOORD4;
	float3	WorldPos  : TEXCOORD5;
	float3	Tangent	  : TEXCOORD6;
	float3	Binormal  : TEXCOORD7;
};


#ifdef DRAW
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
	
	float k_in	=	 1 / f_in;
	float k_out	=	-1 / f_out;
	
	if (t <   f_in ) y = t * k_in;
	if (t > 1-f_out) y = t * k_out - k_out;
	
	return y;
}


#include "temperature.fxi"

float3 ComputeVelocity ( float3 position, float3 velocity )
{
	float4 pp0	=	mul( float4( position,            1 ), Camera.ViewProjection );
	float4 pp1	=	mul( float4( position + velocity, 1 ), Camera.ViewProjection );
	
	pp0 /= pp0.w;
	pp1 /= pp1.w;
	
	return pp1.xyz - pp0.xyz;
}



[maxvertexcount(6)]
void GSMain( point VSOutput inputPoint[1], inout TriangleStream<GSOutput> outputStream )
{
	GSOutput p0, p1, p2, p3;
	
	uint prtId = (uint)( sortParticleBuffer[ inputPoint[0].vertexID ].y );
	//uint prtId = inputPoint[0].vertexID;
	
	Particle prt = particleBuffer[ prtId ];
	
	if (prt.TimeLag<0) {
		return;
	}
	
	if (prt.TimeLag >= prt.LifeTime ) {
		return;
	}

	if (IsWeaponFX(prt)) 
	{
		TransformVector( Camera.ViewInverted, prt.Velocity );
		TransformPoint ( Camera.ViewInverted, prt.Position );
	}//*/
	
	float time		=	prt.TimeLag;
	float factor	=	saturate(prt.TimeLag / prt.LifeTime);
	
	float  sz 		=   lerp( GetSize0(prt), GetSize1(prt), factor )/2;
	float  fade		=	Ramp( GetFadeIn(prt), GetFadeOut(prt), factor );
	float  intensity=	lerp( GetIntensity(prt), GetIntensity(prt) * ExposureBuffer[0].g, GetExposure(prt) );
	float4 color4	=	GetColor4( prt );
	float  alpha	=	color4.a * fade;
	float4 color	=	float4( color4.rgb * intensity, alpha );

	if (GetParticleFX(prt)==ParticleFX_Distortive) {
		color	=	float4( 1,1,1, alpha );
	}
	
	float3 position		=	prt.Position    ;// + prt.Velocity * time + accel * time * time / 2;
	
	float  a		=	lerp( GetRotation0(prt), GetRotation1(prt), factor );	

	float2x2	m	=	float2x2( cos(a), sin(a), -sin(a), cos(a) );
	
	float3	offset	=	normalize( prt.Position - Camera.CameraPosition.xyz );
	
	float3	basisRt		=	Camera.CameraRight.xyz * sz;
	float3	basisUp		=	Camera.CameraUp.xyz    * sz;
	float 	beamFactor	=	GetBeamFactor(prt);

	if (beamFactor>0) 
	{
		float3	viewDir	=	normalize(Camera.CameraPosition.xyz - position);
		float3	velNorm	=	normalize(prt.Velocity);
		basisRt			=	(prt.Velocity * 1/60.0f) * beamFactor;
		basisUp			=	normalize( cross( viewDir, basisRt ) ) * sz;
		
		float beamFadeout	=	saturate( 9 - 10*abs(dot( viewDir, velNorm )) );
		color.a				*=	beamFadeout;
		
		if (length(basisRt)<sz) 
		{
			basisRt = normalize(basisRt)*sz;
		}
	}
	if (beamFactor<0) 
	{
		basisRt	=	prt.Velocity;
		basisUp	=	normalize( cross( Camera.CameraPosition.xyz - position, basisRt ) ) * sz;
		
		if (length(basisRt)<sz) 
		{
			basisRt = normalize(basisRt)*sz;
		}
	}
	
	float3		rt	=	(basisRt * cos(a) + basisUp * sin(a));
	float3		up	=	(basisUp * cos(a) - basisRt * sin(a));
	float3		fwd	=	offset * sz;
	
	float 		imageIndex	=	GetImageIndex( prt );
	float 		imageCount	=	GetImageCount( prt );
	
	int			frame	=	(int)lerp( imageIndex, imageIndex + imageCount, factor );
				frame	=	clamp( frame, imageIndex, imageIndex + imageCount );
	float4		image	=	Images[frame];
	
	float4 wpos0	=	float4( position + rt + up - fwd, 1 );
	float4 wpos1	=	float4( position - rt + up - fwd, 1 );
	float4 wpos2	=	float4( position - rt - up - fwd, 1 );
	float4 wpos3	=	float4( position + rt - up - fwd, 1 );
	
	float3 	normal0 =	0;
	float3 	normal1 =	0;
	float3 	normal2 =	0;
	float3 	normal3 =	0;
	
	#ifdef LIGHTMAP
		normal0	=	normalize(  rt + up - fwd * 0.1 );
		normal1	=	normalize( -rt + up - fwd * 0.1 );
		normal2	=	normalize( -rt - up - fwd * 0.1 );
		normal3	=	normalize(  rt - up - fwd * 0.1 );
	#endif
	#ifdef HARD
		normal0	=	normalize( -fwd );
		normal1	=	normalize( -fwd );
		normal2	=	normalize( -fwd );
		normal3	=	normalize( -fwd );
	#endif
	#ifdef VELOCITY
		normal0	=	ComputeVelocity( prt.Position.xyz, prt.Velocity.xyz );
		normal1	=	ComputeVelocity( prt.Position.xyz, prt.Velocity.xyz );
		normal2	=	ComputeVelocity( prt.Position.xyz, prt.Velocity.xyz );
		normal3	=	ComputeVelocity( prt.Position.xyz, prt.Velocity.xyz );
	#endif
	
	float4 pos0		=	mul( wpos0, Camera.View );
	float4 pos1		=	mul( wpos1, Camera.View );
	float4 pos2		=	mul( wpos2, Camera.View );
	float4 pos3		=	mul( wpos3, Camera.View );
	
	/*if (prt.Effects==ParticleFX_Beam) {
		float3 dir	=	normalize(position - tailpos);
		float3 eye	=	normalize(Camera.CameraPosition.xyz - tailpos);
		float3 side	=	normalize(cross( eye, dir ));
		pos0		=	mul( float4( tailpos  + side * sz, 1 ), Params.View );
        pos1		=	mul( float4( position + side * sz, 1 ), Params.View );
        pos2		=	mul( float4( position - side * sz, 1 ), Params.View );
	    pos3		=	mul( float4( tailpos  - side * sz, 1 ), Params.View );
	}//*/
	
	float2 lmszA	 = Params.LightMapSize.zw * 0.0f;
	float2 lmszB	 = Params.LightMapSize.zw * float2(0.5f,-0.5f);
	
	float4 lightmapRegion	=	lightMapRegionsGS[ prtId ];
	
	float4x4	projection	= (IsWeaponFX(prt)) ? CameraWeapon.Projection : Camera.Projection;
	float 		roughness	= GetRoughness( prt );
	float		scattering	= GetScattering( prt );
	float		metallic	= GetMetallic( prt );
	
	p0.Position	 = mul( pos0, projection );
	p0.Normal	 = normal0;
	p0.TexCoord	 = image.zy;
	p0.LMCoord	 = lightmapRegion.zy + lmszA;
	p0.ViewPosSZ = float4( pos0.xyz, 1/sz );
	p0.Color 	 = color;
	p0.LMFactor	 = 0;
	p0.FogSRM	 = float4( 0, scattering, roughness, metallic );
	p0.WorldPos	 = wpos0.xyz;
	p0.Tangent	 = rt;
	p0.Binormal	 = -up;
	p0.Fog		 = SampleVolumetricFog( Fog, p0.Position, FogSampler, FogVolume );
	
	p1.Position	 = mul( pos1, projection );
	p1.Normal	 = normal1;
	p1.TexCoord	 = image.xy;
	p1.LMCoord	 = lightmapRegion.xy + lmszA;
	p1.ViewPosSZ = float4( pos1.xyz, 1/sz );
	p1.Color 	 = color;
	p1.LMFactor	 = 0;
	p1.FogSRM	 = float4( 0, scattering, roughness, metallic );
	p1.WorldPos	 = wpos1.xyz;
	p1.Tangent	 = rt;
	p1.Binormal	 = -up;
	p1.Fog		 = SampleVolumetricFog( Fog, p1.Position, FogSampler, FogVolume );
	
	p2.Position	 = mul( pos2, projection );
	p2.Normal	 = normal2;
	p2.TexCoord	 = image.xw;
	p2.LMCoord	 = lightmapRegion.xw + lmszA;
	p2.ViewPosSZ = float4( pos2.xyz, 1/sz );
	p2.Color 	 = color;
	p2.LMFactor	 = 0;
	p2.FogSRM	 = float4( 0, scattering, roughness, metallic );
	p2.WorldPos	 = wpos2.xyz;
	p2.Tangent	 = rt;
	p2.Binormal	 = -up;
	p2.Fog		 = SampleVolumetricFog( Fog, p2.Position, FogSampler, FogVolume );
	
	p3.Position	 = mul( pos3, projection );
	p3.Normal	 = normal3;
	p3.TexCoord	 = image.zw;
	p3.LMCoord	 = lightmapRegion.zw + lmszA;
	p3.ViewPosSZ = float4( pos3.xyz, 1/sz );
	p3.Color 	 = color;
	p3.LMFactor	 = 0;
	p3.FogSRM	 = float4( 0, scattering, roughness, metallic );
	p3.WorldPos	 = wpos3.xyz;
	p3.Tangent	 = rt;
	p3.Binormal	 = -up;
	p3.Fog		 = SampleVolumetricFog( Fog, p3.Position, FogSampler, FogVolume );
	
	uint prtEffects = GetParticleFX(prt);
	
	#if defined(SOFT)
	if (prtEffects==ParticleFX_SoftLit || prtEffects==ParticleFX_SoftLitShadow) {
		p0.LMFactor	 = 1;		p1.LMFactor	 = 1;
		p2.LMFactor	 = 1;		p3.LMFactor	 = 1;
	}
	#endif
	
	#if defined(HARD)
	if (prtEffects==ParticleFX_HardLit || prtEffects==ParticleFX_HardLitShadow) {
		p0.LMFactor	 = 1;		p1.LMFactor	 = 1;
		p2.LMFactor	 = 1;		p3.LMFactor	 = 1;
	}
	#endif
	
	#if defined(SOFT_SHADOW) || defined(HARD_SHADOW)
	if (prtEffects!=ParticleFX_SoftLitShadow && prtEffects!=ParticleFX_HardLitShadow) {
		return;
	}
	#endif
	
	#ifdef LIGHTMAP
		p0.Position	 = float4( float2(1,-1) * (p0.LMCoord.xy * 2 - 1), 0, 1 );
		p1.Position	 = float4( float2(1,-1) * (p1.LMCoord.xy * 2 - 1), 0, 1 );
		p2.Position	 = float4( float2(1,-1) * (p2.LMCoord.xy * 2 - 1), 0, 1 );
		p3.Position	 = float4( float2(1,-1) * (p3.LMCoord.xy * 2 - 1), 0, 1 );
		p0.ViewPosSZ = float4( wpos0.xyz, sz );
		p1.ViewPosSZ = float4( wpos1.xyz, sz );
		p2.ViewPosSZ = float4( wpos2.xyz, sz );
		p3.ViewPosSZ = float4( wpos3.xyz, sz );
	#endif
	
	outputStream.Append(p0);
	outputStream.Append(p1);
	outputStream.Append(p3);
	outputStream.Append(p2);
}


//	Soft particles :
//	http://developer.download.nvidia.com/SDK/10/direct3d/Source/SoftParticles/doc/SoftParticles_hi.pdf

#if defined(DRAW) && defined(LIGHTMAP)
#define SOFT_LIGHTING
#include "particles_lighting.hlsl"
#endif


#if defined(DRAW) && defined(HARD)
#define HARD_LIGHTING
#include "particles_lighting.hlsl"
#endif

#include "dither.fxi"

float3 ApplyFog( float3 color, float4 fog )
{
	return color * fog.a + fog.rgb;
}

float4 PSMain( GSOutput input, float4 vpos : SV_POSITION ) : SV_Target
{
	#if defined(SOFT) || defined(DUDV)
		float  depth 	= 	DepthValues.Load( int3(vpos.xy,0) ).r;
		float  a 		= 	Camera.LinearizeDepthScale;
		float  b        = 	Camera.LinearizeDepthBias;
		float  sceneZ   = 	1 / (depth * a + b);
		
		float  prtZ		= 	abs(input.ViewPosSZ.z);

		// TODO : profile soft particles clipping!
		//	May be using depth buffer instead? (copy required)
		// if (depth < vpos.z) {
		// 	clip(-1);
		// }
		float particleThickness =	input.ViewPosSZ.w;
		float softFactor		=	saturate( (sceneZ - prtZ) * particleThickness + 1 ) * saturate(prtZ * particleThickness);
	#endif

	#ifdef SOFT
		float4 	color	=	Texture.Sample( LinearSampler, input.TexCoord );
		float3 	light	=	(input.LMFactor > 0.5f) ? LightMap.Sample( LinearSampler, input.LMCoord ).rgb : 1;

		//color.rgba		=	SRGBToLinear( color.rgba ) * input.Color;	

		color.rgb		=	SRGBToLinear( color.rgba ) * input.Color.rgb;	
		color.a			=	color.a * input.Color.a;	
		//color.a			=	saturate( color.a - 1 + (sqrt(input.Color.a))) * input.Color.a;

		color.rgba 		*= 	softFactor;
		color.rgb  		*= 	light.rgb;
		
		color.rgb		=	ApplyFog( color.rgb, input.Fog );
		//color.a			*	input.Fog.a;
		
		return color;
	#endif
	
	
	#ifdef HARD
		float4 	normalAlpha	=	Texture.Sample( LinearSampler, input.TexCoord );
		float3	normal		=	normalize(normalAlpha.xyz * 2 - 1);
		float	alpha		=	normalAlpha.w;
		float	scatter		=	input.FogSRM.y;
		float	roughness	=	input.FogSRM.z;
		float	metallic	=	input.FogSRM.w;
		float3	baseColor	=	input.Color.rgb;
		float3	worldPos	=	input.WorldPos.xyz;
				
		float3	finalColor	=	0;
		
		float3x3 tbnToWorld	= float3x3(
				input.Tangent.x,	input.Tangent.y,	input.Tangent.z,	
				input.Binormal.x,	input.Binormal.y,	input.Binormal.z,	
				input.Normal.x,		input.Normal.y,		input.Normal.z		
			);
			
		float3 	worldNormal = 	normalize( mul( normal, tbnToWorld ).xyz );
		
		[branch]
		if (input.LMFactor > 0.5f) {
			finalColor	=	ComputeClusteredLighting( worldPos, worldNormal, baseColor, scatter, roughness, metallic );
		} else {
			finalColor	=	baseColor;
		}
		
		finalColor		=	ApplyFog( finalColor, input.Fog );
		
		clip( alpha - ( 1 - input.Color.a ) );
		
		return float4(finalColor, 1);
	#endif
	
	#ifdef DUDV
		float4 	color	=	Texture.Sample( LinearSampler, input.TexCoord );
		float	decay	=	1 / (sceneZ+1);
		float2	dudv	=	color.xy * 2 - 1;
				dudv	*=	color.a;
				dudv	*=	input.Color.a;
				dudv	*=	softFactor;
				
		float2	zero	=	float2(0, 0);

		return float4( max(zero, dudv.xy), max(zero, -dudv.xy) );
	#endif
	
	#ifdef VELOCITY
		float4 	tcolor		=	Texture.Sample( LinearSampler, input.TexCoord );
		float4	vcolor		=	input.Color;
		float	alpha		=	tcolor.a * vcolor.a;
		float3	velocity	=	input.Normal.xyz * tcolor.a;

		return float4( velocity.xyz * 0.5f + 0.5f, alpha*alpha );
	#endif
	
	#ifdef SOFT_SHADOW
		#if 1
		float4 textureColor	=	Texture.Sample( LinearSampler, input.TexCoord );
		float4 vertexColor  =  	input.Color;
		float4 color		=	1 - vertexColor.a * textureColor.a;
		return color;
		#else
		float	alphaT	=	Texture.Sample( LinearSampler, input.TexCoord ).a;
		float	alphaV	=	input.Color.a;
		float	alpha	=	alphaT * alphaV;
		clip( BayerDitherAlpha4x4(sqrt(alpha), vpos.xy)-0.5 );
		return 0;
		#endif
	#endif
	
	#ifdef HARD_SHADOW
		float	alpha	=	Texture.Sample( LinearSampler, input.TexCoord ).a;
		
		clip( alpha - ( 1 - input.Color.a ) );
		
		return 0;
	#endif
	
	
	#ifdef LIGHTMAP
		float3 lighting = 0;
		int count = 8;
		float sz = input.ViewPosSZ.w;
		float scatter = input.FogSRM.y;
		
		for (int i=0; i<count; i++) {
			float 		t	=	(((i / (float)count)*2-1) * sz) * 0.5 + 0.5;
			float3 		pos = 	input.ViewPosSZ.xyz + t * Camera.CameraForward.xyz * input.ViewPosSZ.w;
			lighting 		+= 	ComputeClusteredLighting( pos, normalize(input.Normal), 1, scatter, 0.5, 0 ) / count;
		}
		return float4(lighting,1);
	#endif
	
}
#endif

