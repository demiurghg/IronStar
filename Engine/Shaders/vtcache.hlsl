
#if 0
$ubershader
#endif

#include "auto/vtcache.fxi"

void ComputeTileLocation( uint index, out uint2 xy, out uint mip )
{
	xy = uint2(0,0);
	if ( index < 64*64 )
	{
		mip	= 0;
	}
	else if ( index < ( 64*64 + 32*32 ) )
	{
		mip = 1;
		index -= ( 64*64 );
	}
	else if ( index < ( 64*64 + 32*32 + 16*16 ) )
	{
		mip = 2;
		index -= ( 64*64 + 32*32 );
	}
	else if ( index < ( 64*64 + 32*32 + 16*16 + 8*8 ) )
	{
		mip = 3;
		index -= ( 64*64 + 32*32 + 16*16 );
	}
	else if ( index < ( 64*64 + 32*32 + 16*16 + 8*8 + 4*4 ) )
	{
		mip = 4;
		index -= ( 64*64 + 32*32 + 16*16 + 8*8 );
	}
	else if ( index < ( 64*64 + 32*32 + 16*16 + 8*8 + 4*4 + 2*2 ) )
	{
		mip = 5;
		index -= ( 64*64 + 32*32 + 16*16 + 8*8 + 4*4 );
	}
	else if ( index < ( 64*64 + 32*32 + 16*16 + 8*8 + 4*4 + 2*2 + 1*1 ) )
	{
		mip = 6;
		index -= ( 64*64 + 32*32 + 16*16 + 8*8 + 4*4 + 2*2 );
	}
		
	uint size 	= 	(VTVirtualPageCount / 16) >> mip;
	xy.x		=	index % size;
	xy.y		=	index / size;
}

void WriteValue( uint2 xy, uint mip, float4 value )
{
	switch (mip)
	{
		case 0: pageTable [xy] = value; break;
		case 1: pageTable1[xy] = value; break;
		case 2: pageTable2[xy] = value; break;
		case 3: pageTable3[xy] = value; break;
		case 4: pageTable4[xy] = value; break;
		case 5: pageTable5[xy] = value; break;
		case 6: pageTable6[xy] = value; break;
	}
}


groupshared uint visiblePageCount = 0; 
groupshared uint visiblePages[343];

[numthreads(16,16,1)] 
void CSMain( uint3 dispatchThreadId : SV_DispatchThreadID, uint3 groupIndex : SV_GroupID, uint3 groupThreadIndex : SV_GroupThreadID ) 
{
	uint 	totalPageCount	=	Params.totalPageCount;

	float4 	physicalAddress	=	float4(0,0,999,0);
	uint2 	tileLocation;
	uint2 	targetLocation;
	uint  	targetMipLevel;
	
	ComputeTileLocation( groupIndex.x, tileLocation, targetMipLevel );
	targetLocation	=	tileLocation.xy * 16 + groupThreadIndex.xy;
	
	GroupMemoryBarrierWithGroupSync();

#if 1
	//--------------------------
	// Tiled approach:
	//--------------------------
	
	uint passCount	=	(totalPageCount + BlockSize - 1) / BlockSize;
	
	for (uint passIt=0; passIt < passCount; passIt++) 
	{
		uint 	pageIndex	=	passIt * 16*16 + groupThreadIndex.y * 16 + groupThreadIndex.x;
		
		float2 	tileMin = 	float2( tileLocation.x * 16,    	tileLocation.y * 16      );
		float2 	tileMax = 	float2( tileLocation.x * 16 + 16,	tileLocation.y * 16 + 16 );
		
		if ( pageIndex < totalPageCount) 
		{
			PageGpu	page = pageData[ pageIndex ];
			
			if ( page.Mip >= targetMipLevel ) 
			{
				float	size	=	exp2(page.Mip - targetMipLevel);
				float2 	pageMin	=	float2( page.VX * size, 		page.VY * size 		  );
				float2 	pageMax	=	float2( page.VX * size + size, 	page.VY * size + size );
			
				if ( pageMin.x < tileMax.x && tileMin.x < pageMax.x 
				  && pageMin.y < tileMax.y && tileMin.y < pageMax.y ) 
				{
					uint offset; 
					InterlockedAdd(visiblePageCount, 1, offset); 
					visiblePages[offset] = pageIndex;
				}
			}
		}
	}
	
	GroupMemoryBarrierWithGroupSync();
	
	for (uint i = 0; i < visiblePageCount; i++) 
	{
		uint 	pageIndex	=	visiblePages[ i ];
		PageGpu page 		= 	pageData[ pageIndex ];
		uint	mip			=	(uint)page.Mip;
		float	size		=	exp2(mip - targetMipLevel);
		
		if (physicalAddress.z>page.Mip) 
		{
			{
				if ( ( ( page.VX*size       ) <= targetLocation.x )
				  && ( ( page.VX*size + size) >  targetLocation.x )
				  && ( ( page.VY*size       ) <= targetLocation.y )
				  && ( ( page.VY*size + size) >  targetLocation.y ) 
				 )
				{
					physicalAddress 	=	float4( page.OffsetX, page.OffsetY, page.Mip, 1 );
				}
			}
		}
	}
	
	GroupMemoryBarrierWithGroupSync();
#else 
	//--------------------------
	// Brute-force approach:
	//--------------------------
	for (int mipPass=6; mipPass>=0; mipPass--)
	{
		for (uint i = 0; i < totalPageCount; i++) 
		{
			PageGpu page 		= 	pageData[ i ];
			uint	mip			=	(uint)page.Mip;
			float	size		=	exp2(mip - targetMipLevel);
			
			if (mip<=mipPass)
			{
				if ( ( ( page.VX*size       ) <= targetLocation.x )
				  && ( ( page.VX*size + size) >  targetLocation.x )
				  && ( ( page.VY*size       ) <= targetLocation.y )
				  && ( ( page.VY*size + size) >  targetLocation.y ) 
				 )
				{
					physicalAddress 	=	float4( page.OffsetX, page.OffsetY, page.Mip, 1 );
				}
			}
		}
	}
#endif
	
	WriteValue( targetLocation, targetMipLevel, physicalAddress );
	//WriteValue( targetLocation, targetMipLevel, float4(targetLocation.xy, targetMipLevel,0) );
	//pageTable[dispatchThreadId.xy] = physicalAddress;
}
