
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

#define PS_INPUT VS_OUTPUT

#define PI 3.141592f

#define HalfPI (3.141592f / 2.0f)

/*-------------------------------------------------------------------------------------------------
	Atmospheric Scattering Model :
-------------------------------------------------------------------------------------------------*/

bool raySphereIntersect(float3 origin, float3 dir, float radius, out float t0, out float t1 )
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
	
	if (D<0)
	{
        return false;
    }
	
    t0	=	(-b - sqrt(D))/(2.0*a);
    t1	=	(-b + sqrt(D))/(2.0*a);
	return true;
}

bool RayIntersectsPlanet( float3 origin, float3 dir )
{
	float t0, t1;
	return raySphereIntersect( origin, dir, Sky.PlanetRadius, t0, t1 ) && t1 > 0;
}

bool RayIntersectsAtmosphere( float3 origin, float3 dir )
{
	float t0, t1;
	return raySphereIntersect( origin, dir, Sky.AtmosphereRadius, t0, t1 ) && t1 > 0;
}

float3 computeIncidentLight(float3 orig, float3 dir, float3 sunDir, float tmin, float tmax)
{ 
	float 	atmosphereRadius	=	Sky.AtmosphereRadius;
	float 	earthRadius			=	Sky.PlanetRadius;
	float3	sunDirection		=	sunDir;
	float	M_PI				=	3.141592f;
	float	Hr					=	Sky.RayleighHeight;
	float	Hm					=	Sky.MieHeight;
    float 	g 					= 	Sky.MieExcentricity; 
	float3	betaR				=	Sky.BetaRayleigh.xyz;
	float3	betaM				=	Sky.BetaMie.xyz;

    float t0, t1; 
    if (!raySphereIntersect(orig, dir, atmosphereRadius, t0, t1) || t1 < 0) return float3(0,0,0); 
    if (t0 > tmin && t0 > 0) tmin = t0; 
    if (t1 < tmax) tmax = t1; 
    uint numSamples = 16; 
    uint numSamplesLight = 8; 
    float segmentLength = (tmax - tmin) / numSamples; 
    float tCurrent = tmin; 
    float3 sumR = 0; // rayleigh contribution 
	float3 sumM = 0; // mie contribution 
    float opticalDepthR = 0, opticalDepthM = 0; 
    float mu = dot(dir, sunDirection); // mu in the paper which is the cosine of the angle between the sun direction and the ray direction 
    float phaseR = 3.f / (16.f * M_PI) * (1 + mu * mu); 
    float phaseM = 3.f / (8.f * M_PI) * ((1.f - g * g) * (1.f + mu * mu)) / ((2.f + g * g) * pow(1.f + g * g - 2.f * g * mu, 1.5f)); 

    for (uint i = 0; i < numSamples; ++i) 
	{ 
        float3 samplePosition = orig + (tCurrent + segmentLength * 0.5f) * dir; 
        float height = length(samplePosition) - earthRadius; 
        // compute optical depth for light
        float hr = exp(-height / Hr) * segmentLength; 
        float hm = exp(-height / Hm) * segmentLength; 
        opticalDepthR += hr; 
        opticalDepthM += hm; 
        // light optical depth
        float t0Light, t1Light; 
        raySphereIntersect(samplePosition, sunDirection, atmosphereRadius, t0Light, t1Light); 
        float segmentLengthLight = t1Light / numSamplesLight, tCurrentLight = 0; 
        float opticalDepthLightR = 0, opticalDepthLightM = 0; 
		
		float planetShadow	=	RayIntersectsPlanet( samplePosition, sunDirection ) ? 0 : 1;

        for (uint j = 0; j < numSamplesLight; ++j) 
		{ 
            float3 samplePositionLight = samplePosition + (tCurrentLight + segmentLengthLight * 0.5f) * sunDirection; 
            float heightLight = length(samplePositionLight) - earthRadius; 
            if (heightLight < 0) break; 
            opticalDepthLightR += exp(-heightLight / Hr) * segmentLengthLight; 
            opticalDepthLightM += exp(-heightLight / Hm) * segmentLengthLight; 
            tCurrentLight += segmentLengthLight; 
        } 
        if (j == numSamplesLight) 
		{ 
            float3 tau = betaR * (opticalDepthR + opticalDepthLightR) + betaM * 1.1f * (opticalDepthM + opticalDepthLightM); 
            float3 attenuation	=	float3(exp(-tau.x), exp(-tau.y), exp(-tau.z)); 
            sumR += attenuation * hr * planetShadow; 
            sumM += attenuation * hm * planetShadow; 
        } 
        tCurrent += segmentLength; 
    } 
 
    return (sumR * betaR * phaseR + sumM * betaM * phaseM) * DirectLight.DirectLightIntensity.rgb; 
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


float3 ComputeSkyColor( float3 rayDir, float sunAzimuth, float sunAltitude )
{
	float3 	origin	=	float3( 0, Sky.PlanetRadius + Sky.ViewHeight, 0 );
	float3	sunDir	=	RayFromAngles( sunAzimuth, sunAltitude );

	float	tmin, tmax;

	if ( raySphereIntersect(origin, rayDir, Sky.PlanetRadius, tmin, tmax) && tmax > 0 )
	{
		tmax	=	tmin;
		tmin	=	0;
	}
	else
	{
		tmin	=	0;
		tmax	=	1000000;
	}
	
	return computeIncidentLight( origin, rayDir, sunDir, tmin, tmax ) * Sky.SkyExposure;
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

[numthreads(BLOCK_SIZE,BLOCK_SIZE,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY		=	dispatchThreadId.xy;
	int2	storeXY		=	dispatchThreadId.xy;
	
	float2	signedUV	=	2 * loadXY / float2( LUT_WIDTH-1, LUT_HEIGHT-1 ) - float2(1, 1);

	//float	
	float	horizon		=	HorizonAngle();
	float	azimuth		=	PI 	  * signedUV.x;
	float	altitude	=	LutToAltitude( signedUV.y, horizon / HalfPI ) * HalfPI;
	
	float3	rayDir		=	RayFromAngles( azimuth, altitude );
	
	float3	skyColor	=	ComputeSkyColor( rayDir, 0, Sky.SunAltitude );
	LutUav[ storeXY ]	=	float4( skyColor, 1 );
	
	
	// plot map function :
	//LutUav[ storeXY ]	=	MapLut( signedUV.x, horizon/HalfPI ) > signedUV.y ? 0 : 1;

	//	plot altitude :
	//LutUav[ storeXY ]	=	float4( altitude, 0, -altitude, 1 );
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
	
	float2 	signedUV;
	
	signedUV.x			=	azimuth / PI;
	signedUV.y			=	AltitudeToLut( altitude / HalfPI, horizon / HalfPI );

	float2	normUV		=	signedUV * 0.5 + 0.5f;
	
	float3 	skyColor	= 	Lut.SampleLevel( LinearClamp, normUV, 0 );
	
	return 	float4( skyColor, 1 );
}

#endif




























