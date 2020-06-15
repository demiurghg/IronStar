
#if 0
$ubershader SKY|FOG
$ubershader	LUT
#endif

#include "auto/sky2.fxi"

struct VS_INPUT {
	float3 position		: POSITION;
};
	

struct VS_OUTPUT {
	float4 position		: SV_POSITION;
	float3 worldPos		: TEXCOORD1;
	float3 skyColor		: COLOR0;
	float3 rayDir		: COLOR1;
};

struct SKY_ST
{
	float4	scattering		: SV_TARGET0;		//	sky emission in given point
	float4	transmittance	: SV_TARGET1;		//	sky transparency in given point
};


#define PS_INPUT VS_OUTPUT

#define PI 					3.141592f
#define HalfPI 				(3.141592f / 2.0f)


/*-------------------------------------------------------------------------------------------------
	Atmospheric Scattering Model :
-------------------------------------------------------------------------------------------------*/

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


float3 ComputeIndcidentSunLight( float3 samplePosition, float3 sunDirection )
{
	float t0, t1;
	int numSamples = 3; // is enough for half numerical solution
						// fully numerical solution requires about 64 samples
	
	//	no intersection, sun light comes without attenuation :
	if (!RaySphereIntersect(samplePosition, sunDirection, Sky.AtmosphereRadius, t0, t1))
	{
		return Sky.SunIntensity.rgb;
	}
	
	float3	p0	=	samplePosition + sunDirection * t0;
	float3	p1	=	samplePosition + sunDirection * t1;
	
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
		
		if (abs(k)>0.001f)
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
	
	return exp( -tau ) * Sky.SunIntensity.rgb;
}


#if 1
static const uint numIntegrationSamlpes = 48;
float t(float i) { return pow((i+0.5f) / numIntegrationSamlpes, 2); }
float q(float i) { return t(i+0.5f) - t(i-0.5f); }
#else
float t(float i) { return (i+0.5f) / numIntegrationSamlpes; }
float q(float i) { return 1.0f / numIntegrationSamlpes; }
#endif


SKY_ST computeIncidentLight(float3 orig, float3 dir, float3 sunDir, float tmin, float tmax)
{ 
	float	M_PI				=	3.141592f;
    float 	g 					= 	Sky.MieExcentricity; 
	float3	betaR				=	Sky.BetaRayleigh.xyz;
	float3	betaM				=	Sky.BetaMie.xyz;

	SKY_ST st;
	st.scattering		=	0;
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
	
	float3 ambient		=	0;//*SkyCube.SampleLevel( LinearClamp, dir + float3(0,1,0), 0 ).rgb;

    for (uint i = 0; i < numIntegrationSamlpes; ++i) 
	{ 
        float3 	pos 	=	lerp( p0, p1, t(i) ); 
        float 	height 	=	length(pos) - Sky.PlanetRadius; 

        float 	hr 		= 	exp(-height / Sky.RayleighHeight) * segmentLength * q(i); 
        float 	hm 		= 	exp(-height / Sky.MieHeight		) * segmentLength * q(i); 
		
		float3	extinction		=	hr * betaR + hm * betaM * 1.1f;
		float3	extinctionClamp	=	clamp( extinction, 0.0000001, 1 );
		float3	transmittance	=	exp( - extinction );
		
		float3	luminance		=	ComputeIndcidentSunLight( pos, sunDir );
		float3	scattering		=	luminance * (hr * phaseR * betaR + hm * phaseM * betaM);
		
		float3	ambientLuminance	=	ambient * Sky.AmbientLevel;
				scattering			+=	ambientLuminance * (hr * betaR + hm * betaM);

		#if 0
			st.scattering.rgb		+=	scattering * st.transmittance;
			st.transmittance.rgb	*=	transmittance;
		#else
			float3	integScatt		=	( scattering - scattering * transmittance ) / extinctionClamp;
			st.scattering.rgb		+=	st.transmittance.rgb * integScatt;
			st.transmittance.rgb	*=	transmittance;
		#endif
	} 
 
    return st; 
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

SKY_ST ComputeSkyColor( float3 rayDir, float sunAzimuth, float sunAltitude )
{
	// add 2 meters to prevent raycast against planet surface
	float3 	origin	=	float3( 0, Sky.PlanetRadius + Sky.ViewHeight + 2, 0 );
	float3	sunDir	=	RayFromAngles( sunAzimuth, sunAltitude );

	float	tmin, tmax;
	float 	trans;

	if ( RaySphereIntersect(origin, rayDir, Sky.PlanetRadius, tmin, tmax) )
	{
		tmax	=	tmin;
		tmin	=	0;
		trans	=	0;
	}
	else
	{
		tmin	=	0;
		tmax	=	1000000;
		trans	=	1;
	}
	
	SKY_ST	lut			=	computeIncidentLight( origin, rayDir, sunDir, tmin, tmax );
	lut.scattering		*=	Sky.SkyExposure;
	lut.transmittance	*=	trans;
	
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

#if defined(LUT)

float4 VSMain(uint VertexID : SV_VertexID) : SV_POSITION
{
	return float4((VertexID == 0) ? 3.0f : -1.0f, (VertexID == 2) ? 3.0f : -1.0f, 1.0f, 1.0f);
}


SKY_ST PSMain( float4 vpos : SV_POSITION )
{
	int2	loadXY		=	int2(vpos.xy);
	
	float2	normUV		=	loadXY / float2( LUT_WIDTH-1, LUT_HEIGHT-1 );
	float2	signedUV	=	2 * normUV - float2(1, 1);

	float	horizon		=	HorizonAngle();
	float	azimuth		=	PI 	  * normUV.x;
	float	altitude	=	LutToAltitude( signedUV.y, horizon / HalfPI ) * HalfPI;
	
	float3	rayDir		=	RayFromAngles( azimuth, altitude );
	
	return	ComputeSkyColor( rayDir, 0, Sky.SunAltitude );
}

#endif


/*-------------------------------------------------------------------------------------------------
	SKY/FOG Pixel/Vertex shaders
-------------------------------------------------------------------------------------------------*/

#if defined(SKY) || defined(FOG)

VS_OUTPUT VSMain( VS_INPUT input )
{
	VS_OUTPUT output;

	output.position = mul( float4( input.position * Sky.SkySphereSize + Camera.CameraPosition.xyz, 1.0f ), Camera.ViewProjection );
	output.worldPos = input.position;

	output.rayDir	=	input.position.xyz;
	output.skyColor	= 	0;//ComputeSkyColor( input.position.xyz );
	
	return output;
}

float4 PSMain( PS_INPUT input ) : SV_TARGET0
{
	float	horizon		=	HorizonAngle();
	float 	altitude	=	HorizonAngle( input.rayDir );
	float	azimuth		=	Azimuth( input.rayDir ) - Sky.SunAzimuth;
	
	//return ComputeSkyColor( input.rayDir, Sky.SunAzimuth, Sky.SunAltitude ).emission;
	
	float2 	normUV;
	
	normUV.x			=	azimuth / PI;
	normUV.y			=	AltitudeToLut( altitude / HalfPI, horizon / HalfPI ) * 0.5f + 0.5f;
	
	float4 	skyScattering		= 	LutScattering	.SampleLevel( LinearClamp, normUV, 0 );
	float4 	skyTransmittance	= 	LutTransmittance.SampleLevel( LinearClamp, normUV, 0 );
	
	float3	sunLimb			=	0;
	
	#ifdef SKY
		
		float 	cosSun	=	saturate( dot(normalize(input.rayDir), Sky.SunDirection.xyz ) );
		float 	sinSun	=	sqrt( 1 - cosSun * cosSun );
		
		float4 	sun		=	Sky.SunBrightness;
		
		float	factor	=	saturate( 1 - pow(sinSun / sun.a,4) );
		
		sunLimb	=	sun.rgb * skyTransmittance.rgb * factor;
		
	#endif
	
	return 	skyScattering + float4(sunLimb,0);
}

#endif




























