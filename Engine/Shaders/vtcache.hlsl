
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

uint Encode(uint4 value)
{
	//	[y/n][res][x:13][y:13][mip:4]
	return 
		(( value.w & 0x0001 ) << 31 ) |
		(( value.x & 0x1FFF ) << 17 ) |
		(( value.y & 0x1FFF ) <<  4 ) |
		(( value.z & 0x000F ) <<  0 ) ;
}

void WriteValue( uint2 xy, uint mip, uint4 value )
{
	switch (mip)
	{
		case 0: pageTable [xy] = Encode(value); break;
		case 1: pageTable1[xy] = Encode(value); break;
		case 2: pageTable2[xy] = Encode(value); break;
		case 3: pageTable3[xy] = Encode(value); break;
		case 4: pageTable4[xy] = Encode(value); break;
		case 5: pageTable5[xy] = Encode(value); break;
		case 6: pageTable6[xy] = Encode(value); break;
	}
}


void WriteValue( uint2 xy, uint mip, uint value )
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
		
		uint2 	tileMin		= 	uint2( tileLocation.x * 16,    		tileLocation.y * 16      );
		uint2 	tileMax		= 	uint2( tileLocation.x * 16 + 16,	tileLocation.y * 16 + 16 );
		
		if ( pageIndex < totalPageCount) 
		{
			PageGpu	page = pageData[ pageIndex ];
			
			uint pageMip = page.PAddr & 0xF;
			
			if ( pageMip >= targetMipLevel ) 
			{
				uint	size	=	exp2(pageMip - targetMipLevel);
				uint2 	pageMin	=	uint2( page.VX * size, 			page.VY * size 		  );
				uint2 	pageMax	=	uint2( page.VX * size + size, 	page.VY * size + size );
			
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
	
	uint 	physicalAddress	=	0xF;

	for (uint i = 0; i < visiblePageCount; i++) 
	{
		uint 	pageIndex	=	visiblePages[ i ];
		PageGpu page 		= 	pageData[ pageIndex ];
		uint	pageMip 	=	page.PAddr & 0xF;
		uint	size		=	exp2(pageMip - targetMipLevel);
		
		[flatten]
		if ( (physicalAddress & 0xF) > pageMip ) 
		{
			[flatten]
			if ( ( ( page.VX*size       ) <= targetLocation.x )
			  && ( ( page.VX*size + size) >  targetLocation.x )
			  && ( ( page.VY*size       ) <= targetLocation.y )
			  && ( ( page.VY*size + size) >  targetLocation.y ) 
			 )
			{
				physicalAddress 	=	page.PAddr;
			}
		}
	}

	GroupMemoryBarrierWithGroupSync();

	WriteValue( targetLocation, targetMipLevel, physicalAddress );
	
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

	WriteValue( targetLocation, targetMipLevel, physicalAddress );
#endif
}
