
#if 0
$ubershader  SKY|FOG
#endif

#include "auto/sky2.fxi"

struct VS_INPUT {
	float3 position		: POSITION;
};
	

struct VS_OUTPUT {
	float4 position		: SV_POSITION;
	float3 worldPos		: TEXCOORD1;
	float3 skyColor		: COLOR0;
};

#define PS_INPUT VS_OUTPUT

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

float3 computeIncidentLight(float3 orig, float3 dir, float tmin, float tmax)
{ 
	float 	atmosphereRadius	=	Sky.AtmosphereRadius;
	float 	earthRadius			=	Sky.PlanetRadius;
	float3	sunDirection		=	-DirectLight.DirectLightDirection.xyz;
	float	M_PI				=	3.141592f;
	float	Hr					=	Sky.RayleighHeight;
	float	Hm					=	Sky.MieHeight;
    float 	g 					= 	Sky.MieExcentricity; 
	float3	betaR				=	Sky.BetaRayleigh.xyz;
	float3	betaM				=	Sky.BetaMie.xyz;

    float t0, t1; 
    if (!raySphereIntersect(orig, dir, atmosphereRadius, t0, t1) || t1 < 0) return float3(1,0,1); 
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
            sumR += attenuation * hr; 
            sumM += attenuation * hm; 
        } 
        tCurrent += segmentLength; 
    } 
 
    // We use a magic number here for the intensity of the sun (20). We will make it more
    // scientific in a future revision of this lesson/code
    return (sumR * betaR * phaseR + sumM * betaM * phaseM) * 500;//DirectLight.DirectLightIntensity; 
}

/*-------------------------------------------------------------------------------------------------
	Vertex Shader
-------------------------------------------------------------------------------------------------*/

VS_OUTPUT VSMain( VS_INPUT input )
{
	VS_OUTPUT output;

	output.position = mul( float4( input.position * Sky.SkySphereSize, 1.0f ), Camera.ViewProjection );
	output.worldPos = input.position;

	float3 	origin	=	float3( 0, Sky.PlanetRadius + 1, 0 );
	float3 	dir		=	input.position;
			dir.y	=	max( 0, dir.y );
	
	output.skyColor	= 	computeIncidentLight( origin, dir, 0, 10000000 );
	
	output.skyColor	=	max( float3(0,0,0), output.skyColor );
	
	/*if (output.skyColor.r>=0 || output.skyColor.r<=0)
	{
	}
	else
	{
		output.skyColor = 100;
	}*/
	/*float3 v = normalize(output.worldPos);
	float3 l = normalize(SunPosition); 
	output.skyColor		= perezSky( Turbidity, max ( v.y, 0.0 ) + 0.05, dot ( l, v ), l.y );*/
	
	return output;
}

/*-------------------------------------------------------------------------------------------------
	Pixel Shader
-------------------------------------------------------------------------------------------------*/

float4 PSMain( PS_INPUT input ) : SV_TARGET0
{
	return float4( input.skyColor, 1 );
}





























