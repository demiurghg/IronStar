
#if 0
$ubershader SKY_VIEW
$ubershader SKY_CUBE
$ubershader	LUT_SKY
$ubershader	LUT_AP
#endif

#include "auto/sky2.fxi"

struct VS_INPUT {
	float3 position		: POSITION;
};
	

struct VS_OUTPUT {
	float4 position		: SV_POSITION;
	float3 worldPos		: TEXCOORD1;
	float3 cirrusUV		: TEXCOORD2; // UV + Linear distance
	float3 skyColor		: COLOR0;
	float3 rayDir		: COLOR1;
};

struct SKY_STC
{
	float4	scattering		: SV_TARGET0;		//	sky emission in given point
	float4	transmittance	: SV_TARGET1;		//	sky transparency in given point
	float4	cirrusClouds	: SV_TARGET2;		//	cirrus lighting and extiction
};

struct SKY_SST
{
	float4	scattering0		;		//	direct light scattering
	float4	scattering1		;		//	indirect light scattering
	float4	transmittance	;		//	transmittance
};


#define PS_INPUT VS_OUTPUT

#define PI 					3.141592f
#define HalfPI 				(3.141592f / 2.0f)


/*-------------------------------------------------------------------------------------------------
	Atmospheric Scattering Model :
-------------------------------------------------------------------------------------------------*/

float3 Rayleigh( float mu )
{
    return 3.f / (16.f * PI) * (1 + mu * mu); 
}

float3 Mie( float mu, float g )
{
    return 3.f / (8.f * PI) * ((1.f - g * g) * (1.f + mu * mu)) / ((2.f + g * g) * pow(abs(1.f + g * g - 2.f * g * mu), 1.5f)); 
}

bool RaySphereIntersect(float3 origin, float3 dir, float radius, out float t0, out float t1 )
{
	t0 = t1 = 0;
	
	float3	r0	=	origin;			// - r0: ray origin
	float3	rd	=	dir;			// - rd: normalized ray direction
	float3	s0	=	float3(0,0,0);	// - s0: sphere center
	float	sr	=	radius;			// - sr: sphere radius

    float 	a 		= dot(rd, rd);
    float3 	s0_r0 	= r0 - s0;
    float 	b 		= 2.0 * dot(rd, s0_r0);
    float 	c 		= dot(s0_r0, s0_r0) - (sr * sr);
	
	float	D		=	b*b - 4.0*a*c;

	//	no intersection at all
	if (D<0) return false;
	
    t0	=	(-b - sqrt(D))/(2.0*a);
    t1	=	(-b + sqrt(D))/(2.0*a);
	
	//	sphere is behind the ray
	if (t0<0 && t1<0) return false;
	
	//	clamp t0
	t0	=	max(0, t0);
	
	return true;
}

bool RayIntersectsPlanet( float3 origin, float3 dir )
{
	float t0, t1;
	return RaySphereIntersect( origin, dir, Sky.PlanetRadius, t0, t1 );
}

bool RayIntersectsAtmosphere( float3 origin, float3 dir )
{
	float t0, t1;
	return RaySphereIntersect( origin, dir, Sky.AtmosphereRadius, t0, t1 );
}


float3 ComputeLightExtincation( float3 p0, float3 p1 )
{
	int numSamples = 3; // is enough for half numerical solution
						// fully numerical solution requires about 64 samples
	
	//	computes amount of attenuated sun light 
	//	coming at given point p0.
	float 	opticalLength	=	 distance( p0, p1 ) / numSamples;
	float 	opticalDepthR	=	0;
	float 	opticalDepthM	=	0;
	float3	betaR			=	Sky.BetaRayleigh.xyz;
	float3	betaM			=	Sky.BetaMie.xyz;
	
	#if 0
	for (int i=0; i<numSamples; i++)
	{
		float3 	pos = 	lerp( p0, p1, (i+0.5)/numSamples );
		float  	h	=	length( pos ) - Sky.PlanetRadius;
		float	hR	=	exp(-h / Sky.RayleighHeight);
		float	hM	=	exp(-h / Sky.MieHeight);
		
		opticalDepthR	+=	hR * opticalLength;
		opticalDepthM	+=	hM * opticalLength;
	}
	#else
	//	integrate transmittance numerically using
	//	analytically calculated integral for each segment
	for (int i=0; i<numSamples; i++)
	{
		float3 	pos0	= 	lerp( p0, p1, (i+0)/(float)numSamples );
		float3 	pos1	= 	lerp( p0, p1, (i+1)/(float)numSamples );
		float  	h0		=	max( 0, length( pos0 ) - Sky.PlanetRadius );
		float  	h1		=	max( 0, length( pos1 ) - Sky.PlanetRadius );
		float	k		=	( h1 - h0 ) / opticalLength;
		float	hR0		=	exp(-h0 / Sky.RayleighHeight);
		float	hM0		=	exp(-h0 / Sky.MieHeight);
		float	hR1		=	exp(-h1 / Sky.RayleighHeight);
		float	hM1		=	exp(-h1 / Sky.MieHeight);
		
		if (abs(k)>0.01f)
		{
			//	integral exp(ax+b) = 1/a * exp(ax+b)
			//	a = k / H
			//	k = dh / dS
			opticalDepthR	+=	(-1) * (hR1 - hR0) * Sky.RayleighHeight / k;
			opticalDepthM	+=	(-1) * (hM1 - hM0) * Sky.MieHeight	    / k;
		}
		else
		{
			//	prevent division by zero, when height difference is small :
			opticalDepthR	+=	hR0 * opticalLength;
			opticalDepthM	+=	hM0 * opticalLength;
		}
	}
	#endif
	
    float3 tau	=	betaR * (opticalDepthR) + betaM * 1.1f * (opticalDepthM); 
	
	return exp( -tau );
}


float3 ComputeIndcidentSunLight( float3 samplePosition, float3 sunDirection )
{
	float t0, t1;
	//	no intersection, sun light comes without attenuation :
	if (!RaySphereIntersect(samplePosition, sunDirection, Sky.AtmosphereRadius, t0, t1))
	{
		return Sky.SunIntensity.rgb;
	}
	
	float3	p0	=	samplePosition + sunDirection * t0;
	float3	p1	=	samplePosition + sunDirection * t1;

	return ComputeLightExtincation( p0, p1 ) * Sky.SunIntensity.rgb;
}


#if 1
	#ifdef LUT_AP 
	static const uint numIntegrationSamlpes = 64;
	#else
	static const uint numIntegrationSamlpes = 64;
	#endif
	float t(float i) { return pow((i+0.5f) / numIntegrationSamlpes, 3); }
	float q(float i) { return (t(i+0.5f) - t(i-0.5f)); }
#else
	static const uint numIntegrationSamlpes = 2048;
	float t(float i) { return (i+0.5f) / numIntegrationSamlpes; }
	float q(float i) { return 1.0f / numIntegrationSamlpes; }
#endif

float3 SampleAmbient( float3 dir )
{
	return Sky.AmbientLevel.rgb;
}

SKY_SST computeIncidentLight(float3 orig, float3 dir, float3 sunDir, float tmin, float tmax)
{ 
	float	M_PI		=	3.141592f;
    float 	g 			= 	Sky.MieExcentricity; 
	float3	betaR		=	Sky.BetaRayleigh.xyz;
	float3	betaM		=	Sky.BetaMie.xyz;

	SKY_SST st;
	st.scattering0		=	0;
	st.scattering1		=	0;
	st.transmittance	=	1;
	
    float t0, t1; 
    if (!RaySphereIntersect(orig, dir, Sky.AtmosphereRadius, t0, t1)) return st; 
	
	t0	=	clamp( t0, tmin, tmax );
	t1	=	clamp( t1, tmin, tmax );
	
	float3 	p0			=	orig + dir * t0;
	float3 	p1			=	orig + dir * t1;
	 
    float 	segmentLength 	= distance(p0, p1); 
    float 	opticalDepthR 	= 0;
	float	opticalDepthM 	= 0; 
    float 	mu 				= dot(dir, sunDir); // cosine of the angle between the sun direction and the ray direction 
    float 	phaseR 			= 3.f / (16.f * M_PI) * (1 + mu * mu); 
    float 	phaseM 			= 3.f / (8.f * M_PI) * ((1.f - g * g) * (1.f + mu * mu)) / ((2.f + g * g) * pow(abs(1.f + g * g - 2.f * g * mu), 1.5f)); 
	
	float3 ambient		=	SampleAmbient(dir);

    for (uint i = 0; i < numIntegrationSamlpes; ++i) 
	{ 
        float3 	pos 	=	lerp( p0, p1, t(i) ); 
        float 	height 	=	max(0, length(pos) - Sky.PlanetRadius); 

        float 	hr 		= 	exp(-height / Sky.RayleighHeight) * segmentLength * q(i); 
        float 	hm 		= 	exp(-height / Sky.MieHeight		) * segmentLength * q(i); 
		
		float3	extinction		=	hr * betaR + hm * betaM * 1.1f;
		float3	extinctionClamp	=	clamp( extinction, 0.0000001, 1 );
		float3	transmittance	=	exp( - extinction );
		
		float3	luminance		=	ComputeIndcidentSunLight( pos, sunDir );
		float3	scattering0		=	luminance * (hr * phaseR * betaR + hm * phaseM * betaM);
		float3	scattering1		=	SampleAmbient(dir) * (hr * betaR + hm * betaM);

		float3	integScatt0		=	( scattering0 - scattering0 * transmittance ) / extinctionClamp;
		float3	integScatt1		=	( scattering1 - scattering1 * transmittance ) / extinctionClamp;
		
		st.scattering0.rgb		+=	st.transmittance.rgb * integScatt0;
		st.scattering1.rgb		+=	st.transmittance.rgb * integScatt1;
		st.transmittance.rgb	*=	transmittance;
	} 
 
    return st; 
}


/*-------------------------------------------------------------------------------------------------
	CLOUDS :
-------------------------------------------------------------------------------------------------*/

float3 ComputeCirrusCloudsCoords( float3 rayDir )
{
	// add 2 meters to prevent raycast against planet surface
	float tmin, tmax;
	float3 	origin	=	Sky.ViewOrigin.xyz;

	if ( RaySphereIntersect(origin, rayDir, Sky.PlanetRadius + Sky.CirrusHeight, tmin, tmax) )
	{
		if (tmin>0) tmax = tmin;
		float3	hitPos	=	origin + rayDir * tmax;
		float2	duv 	=	float2( Sky.CirrusScrollU, Sky.CirrusScrollV );
		float2	uv		=	hitPos.xz * Sky.CirrusScale + duv;
		float	dist	=	tmax;
		return float3( uv, dist );
	}
	else
	{
		return float3(0,0,-1);
	}
}


float CirrusPhaseFunction(float3 rayDir, float3 sunDir)
{
	float c	=	dot(rayDir, sunDir);
	float s = 	sqrt( 1 - c * c );

	//float e = 	2.65f * exp(-10 * s) + 0.91f * c - 0.583f;*/

	c = mad(c, 0.5f, 0.5f);
	float e = 1.5 * exp(-75*(1-c)) + 0.5*exp(-25*c) + 1.2f*c - 1.2f;
	
	return pow(10,e);
}


float4 ComputeCirrusClouds(float3 rayDir, float3 sunDir )
{
	float tmin, tmax;
	float3 	origin	=	Sky.ViewOrigin.xyz;
	
	if ( RayIntersectsPlanet( origin, rayDir ) )
	{
		return float4(0,0,0,0);
	}

	if ( RaySphereIntersect(origin, rayDir, Sky.PlanetRadius + Sky.CirrusHeight, tmin, tmax) )
	{
		float3	hitPos	=	origin + rayDir * tmax;
		float	dist	=	tmax;
		
		float	phase	=	CirrusPhaseFunction( rayDir, sunDir );
		
		float3	light	=	ComputeIndcidentSunLight( hitPos, sunDir ) * phase;
				light	+=	SampleAmbient( rayDir );
				
		float3	extinct	=	ComputeLightExtincation( origin, hitPos );
				light	*=	extinct;
				
		return float4( light, extinct.b ); // assume green is average extinction coefficient
	}
	else
	{
		return float4(0,0,0,0);
	}
}

/*-------------------------------------------------------------------------------------------------
	Sky :
-------------------------------------------------------------------------------------------------*/

float HorizonAngle( float3 rayDir )
{
	float cosA 	=	Sky.PlanetRadius / (Sky.PlanetRadius + Sky.ViewHeight);
	float sinB  = 	normalize(rayDir).y;
	//	horizon angle is always negative:
	return asin(sinB) - (-1) * acos(cosA);
}

float HorizonAngle()
{
	float cosA 	=	Sky.PlanetRadius / (Sky.PlanetRadius + Sky.ViewHeight);
	//	horizon angle is always negative:
	return -acos(cosA);
}

float Azimuth ( float3 rayDir )
{
	float  rayDirLen	=	length(rayDir.xz);
	
	if (rayDirLen==0) return 0;
	
	return atan2( rayDir.x, -rayDir.z );
}

float3 RayFromAngles( float az, float al )
{
	float	x		=	 sin(az) * cos(al);
	float	y		=	 sin(al);
	float	z		=	-cos(az) * cos(al);
	return float3(x,y,z);
}

SKY_STC ComputeSkyColor( float3 rayDir, float sunAzimuth, float sunAltitude )
{
	float3 	origin	=	Sky.ViewOrigin.xyz;
	float3	sunDir	=	RayFromAngles( sunAzimuth, sunAltitude );

	float	tmin, tmax;
	float 	trans;

	if ( RaySphereIntersect(origin, rayDir, Sky.PlanetRadius, tmin, tmax) )
	{
		tmax	=	tmin * Sky.APScale;
		tmin	=	0;
		trans	=	0;
	}
	else
	{
		tmin	=	0;
		tmax	=	1000000;
		trans	=	1;
	}
	
	tmax = min(tmax, 1000000);
	
	SKY_SST	sst			=	computeIncidentLight( origin, rayDir, sunDir, tmin, tmax );
	SKY_STC	lut			=	(SKY_STC)0;
	
	lut.scattering		=	( sst.scattering0 + sst.scattering1 ) * Sky.SkyExposure;
	lut.transmittance	=	( sst.transmittance * trans );
	lut.cirrusClouds	=	ComputeCirrusClouds( rayDir, sunDir );
	
	return lut;
}


/*-------------------------------------------------------------------------------------------------
	SKY LUT
-------------------------------------------------------------------------------------------------*/

float Quantize( float a )
{
	return abs(a) * a;
}

float Dequantize( float a )
{
	return sign(a) * sqrt(abs(a));
}

float LutToAltitude( float x, float b )
{
	x = Quantize(x);
	if (x>0) return ( (x-1)*(1-b)+1 );
	else	 return ( (x+1)*(1+b)-1 );
}

float AltitudeToLut( float x, float b )
{
	if (x>b) return Dequantize( (x-1)/(1-b)+1 );
	else	 return Dequantize( (x+1)/(1+b)-1 );
}

#if defined(LUT_SKY)

float4 VSMain(uint VertexID : SV_VertexID) : SV_POSITION
{
	return float4((VertexID == 0) ? 3.0f : -1.0f, (VertexID == 2) ? 3.0f : -1.0f, 1.0f, 1.0f);
}


SKY_STC PSMain( float4 vpos : SV_POSITION )
{
	int2	loadXY		=	int2(vpos.xy);
	
	float2	normUV		=	loadXY / float2( LUT_WIDTH-1, LUT_HEIGHT-1 );
	float2	signedUV	=	2 * normUV - float2(1, 1);

	float	horizon		=	HorizonAngle();
	float	azimuth		=	PI 	  * normUV.x;
	float	altitude	=	LutToAltitude( signedUV.y, horizon / HalfPI ) * HalfPI;
	
	float3	rayDir		=	RayFromAngles( azimuth, altitude );
	
	SKY_STC	skyLut		=	ComputeSkyColor( rayDir, 0, Sky.SunAltitude );
	
	return skyLut;
}

#endif


/*-------------------------------------------------------------------------------------------------
	AERIAL Perspective Lut
-------------------------------------------------------------------------------------------------*/

#ifdef LUT_AP

float3 GetAPRayDir( float2 location, out float distScale )
{
	float2	normLocation	=	(location.xy + 0.5f) / float2( AP_WIDTH, AP_HEIGHT );
	
	float	tangentX	=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY	=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float4	vsRay		=	float4( tangentX, tangentY, -1, 0 );
	
	distScale			=	length( vsRay );
	
	float3	wsRay		=	mul( vsRay, Camera.ViewInverted ).xyz;
	
	return normalize(wsRay);
}


[numthreads(8,8,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 	location		=	dispatchThreadId.xyz;
	float	invDepthSlices	=	Fog.FogSizeInv.z;
	float	distScale		=	0;
	float3	rayDir			=	GetAPRayDir( location.xy, distScale );
	
	float3	accumScattering		=	float3(0,0,0);
	float	accumTransmittance	=	1;
	
	uint 	slice		=	location.z;

	float 	normSlice	=	(slice + 0.0f) / AP_DEPTH;
			//	...apply texel distribution (see fog.hlsl):
			normSlice	=	pow(normSlice, 1.5f);
			
	float	rayTMax		=	Sky.APScale * log( 1 - normSlice ) / Fog.FogGridExpK * distScale * 0.32;
	
	SKY_SST	skySst		=	computeIncidentLight( Sky.ViewOrigin.xyz, rayDir, Sky.SunDirection.xyz, 0, rayTMax );
	
	LutAP0[ location ]		=	float4( skySst.scattering0.rgb, skySst.transmittance.b );
	LutAP1[ location ]		=	float4( skySst.scattering1.rgb, skySst.transmittance.b );
}

#endif

/*-------------------------------------------------------------------------------------------------
	SKY/FOG Pixel/Vertex shaders
-------------------------------------------------------------------------------------------------*/

#if defined(SKY_VIEW) || defined(SKY_CUBE)

VS_OUTPUT VSMain( VS_INPUT input )
{
	VS_OUTPUT output;

	output.position = mul( float4( input.position * Sky.SkySphereSize + Camera.CameraPosition.xyz, 1.0f ), Camera.ViewProjection );
	output.worldPos = input.position;
	
	float3	rayDir	=	input.position.xyz;

	output.rayDir	=	rayDir;
	output.skyColor	= 	0;
	output.cirrusUV	=	ComputeCirrusCloudsCoords( rayDir );
	
	return output;
}

float4 PSMain( PS_INPUT input ) : SV_TARGET0
{
	float	horizon		=	HorizonAngle();
	float 	altitude	=	HorizonAngle( input.rayDir );
	float	azimuth		=	Azimuth( input.rayDir ) - Sky.SunAzimuth;
	
	//-----------------------------------------
	//	sample LUT scattering :
	//-----------------------------------------
	
	float2 	normUV;
	
	normUV.x			=	azimuth / PI;
	normUV.y			=	AltitudeToLut( altitude / HalfPI, horizon / HalfPI ) * 0.5f + 0.5f;
	
	float4 	skyScattering		= 	LutScattering	.SampleLevel( LutSampler, normUV, 0 );
	float4 	skyTransmittance	= 	LutTransmittance.SampleLevel( LutSampler, normUV, 0 );
	float4 	skyCirrusClouds		= 	LutCirrus		.SampleLevel( LutSampler, normUV, 0 );

	//-----------------------------------------
	//	compute sun color (sky view only):
	//-----------------------------------------
	
	#ifdef SKY_VIEW
		float 	cosSun	=	saturate( dot(normalize(input.rayDir), Sky.SunDirection.xyz ) );
		float 	sinSun	=	sqrt( 1 - cosSun * cosSun );
		
		float4 	sun		=	Sky.SunBrightness;
		
		float	factor	=	saturate( 1 - pow(sinSun / sun.a,4) );
		
		skyScattering.rgb	+=	sun.rgb * skyTransmittance.rgb * factor;
	#endif
	
	//-----------------------------------------
	//	apply ground fog (sky view only) :
	//-----------------------------------------
	
	#if 1
	#ifdef SKY_VIEW
		float2 	fogUV	=	float2( input.position.xy * Sky.ViewportSize.zw );
		float4	fogData	=	FogLut.SampleLevel( LinearClamp, fogUV, 0 );
	
		skyScattering.rgb = skyScattering.rgb * fogData.a + fogData.rgb;
	#endif
	
	//-----------------------------------------
	//	compute cirrus clouds :
	//-----------------------------------------
	
	float3 	cirrusCoords	=	ComputeCirrusCloudsCoords( input.rayDir );
	
	//return float4( frac(cirrusCoords.xy) * 10, 0, 1 );
	
	float	cirrusTexture	=	CirrusClouds.Sample( LinearWrap, cirrusCoords.xy ).r;
	float	coverage		=	max(0.01f, Sky.CirrusCoverage);
			cirrusTexture	=	smoothstep(1-coverage, 1, cirrusTexture);
			cirrusTexture	*=	Sky.CirrusDensity;
			
	float3	cloudGlow		=	skyCirrusClouds.rgb * cirrusTexture.r / 1.1f;

	skyScattering.rgb 		= 	lerp( skyScattering.rgb, cloudGlow.rgb, cirrusTexture.r * cirrusTexture.r * skyCirrusClouds.a );
	#endif
	
	// result :
	return skyScattering;
}

#endif



























