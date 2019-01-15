

//
//	https://seblagarde.wordpress.com/2012/09/29/image-based-lighting-approaches-and-parallax-corrected-cubemap/
//


float3 ParallaxCubeMap( float3 PositionWS, float3 CameraWS, float3 NormalWS, float3 CubemapPositionWS, float4x4 WorldToLocal )
{
	float3 DirectionWS = normalize(PositionWS - CameraWS);
	float3 ReflDirectionWS = reflect(DirectionWS, NormalWS);

	// Intersection with OBB convertto unit box space
	// Transform in local unit parallax cube space (scaled and rotated)
	float3 RayLS 		= mul( float4(ReflDirectionWS,0), WorldToLocal ).xyz;
	float3 PositionLS 	= mul( float4(PositionWS,1), WorldToLocal ).xyz;

	float3 Unitary = float3(1.0f, 1.0f, 1.0f);
	float3 FirstPlaneIntersect  = (Unitary - PositionLS) / RayLS;
	float3 SecondPlaneIntersect = (-Unitary - PositionLS) / RayLS;
	float3 FurthestPlane = max(FirstPlaneIntersect, SecondPlaneIntersect);
	float Distance = min(FurthestPlane.x, min(FurthestPlane.y, FurthestPlane.z));

	// Use Distance in WS directly to recover intersection
	float3 IntersectPositionWS = PositionWS + ReflDirectionWS * Distance;
	
	ReflDirectionWS = IntersectPositionWS - CubemapPositionWS;

	return ReflDirectionWS;
}