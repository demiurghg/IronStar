#include <windows.h>
#include "Local.h"

using namespace Fusion;


TimeVal getPerfTime()
{
	__int64 count;
	QueryPerformanceCounter((LARGE_INTEGER*)&count);
	return count;
}

int getPerfTimeUsec(const TimeVal duration)
{
	static __int64 freq = 0;
	if (freq == 0)
		QueryPerformanceFrequency((LARGE_INTEGER*)&freq);
	return (int)(duration * 1000000 / freq);
}

//-------------------------------------------------------------	

BuildContext::BuildContext()
{
	resetTimers();
}

// Virtual functions for custom implementations.
void BuildContext::doResetLog()
{
}

void BuildContext::doLog(const rcLogCategory category, const char* msg, const int len)
{
	auto str = gcnew System::String(msg, 0, len);
	str = "RECAST : " + str;

	switch (category)
	{
	case RC_LOG_PROGRESS:	Log::Message(str);	break;
	case RC_LOG_WARNING:	Log::Warning(str);	break;
	case RC_LOG_ERROR:		Log::Error  (str);	break;
	}
}

void BuildContext::doResetTimers()
{
	for (int i = 0; i < RC_MAX_TIMERS; ++i)
		m_accTime[i] = -1;
}

void BuildContext::doStartTimer(const rcTimerLabel label)
{
	m_startTime[label] = getPerfTime();
}

void BuildContext::doStopTimer(const rcTimerLabel label)
{
	const TimeVal endTime = getPerfTime();
	const TimeVal deltaTime = endTime - m_startTime[label];
	if (m_accTime[label] == -1)
		m_accTime[label] = deltaTime;
	else
		m_accTime[label] += deltaTime;
}

int BuildContext::doGetAccumulatedTime(const rcTimerLabel label) const
{
	return getPerfTimeUsec(m_accTime[label]);
}


void WriteData( void *base, int &offset, void *src, int size )
{
	memcpy( ((char*)base + offset), src, size );
	offset += size;
}

void ReadData( char **ptr, void *dst, int size )
{
	memcpy( dst, *ptr, size );
	(*ptr) += size;
}

array<System::Byte> ^Utils::SerializePolyMesh( rcPolyMesh *pmesh )
{
	//	unsigned short* verts;	///< The mesh vertices. [Form: (x, y, z) * #nverts]
	//	unsigned short* polys;	///< Polygon and neighbor data. [Length: #maxpolys * 2 * #nvp]
	//	unsigned short* regs;	///< The region id assigned to each polygon. [Length: #maxpolys]
	//	unsigned short* flags;	///< The user defined flags for each polygon. [Length: #maxpolys]
	//	unsigned char*  areas;	///< The area id assigned to each polygon. [Length: #maxpolys]
	//	int nverts;				///< The number of vertices.
	//	int npolys;				///< The number of polygons.
	//	int maxpolys;			///< The number of allocated polygons.
	//	int nvp;				///< The maximum number of vertices per polygon.
	//	float bmin[3];			///< The minimum bounds in world space. [(x, y, z)]
	//	float bmax[3];			///< The maximum bounds in world space. [(x, y, z)]
	//	float cs;				///< The size of each cell. (On the xz-plane.)
	//	float ch;				///< The height of each cell. (The minimum increment along the y-axis.)
	//	int borderSize;			///< The AABB border size used to generate the source data from which the mesh was derived.
	//	float maxEdgeError;		///< The max error of the polygon edges in the mesh.

	int sizeVerts	=	pmesh->nverts * 3 * sizeof(short);
	int sizePolys	=	pmesh->npolys * 2 * pmesh->nvp * sizeof(short);
	int sizeRegs	=	pmesh->npolys * sizeof(short);
	int sizeFlags	=	pmesh->npolys * sizeof(short);
	int sizeAreas	=	pmesh->npolys * sizeof(char);

	int size	=	4 * (4 + 3 + 3 + 2 + 2)
		+ sizeVerts
		+ sizePolys
		+ sizeRegs
		+ sizeFlags
		+ sizeAreas
		+ 20 + 4;

	auto data	=	gcnew array<System::Byte>(size);

	for (int i=0; i<data->Length; i++) data[i] = 0xEE;

	pin_ptr<System::Byte> pin = &data[0];
	int offset = 0;

	WriteData( pin, offset, &pmesh->nverts	,		sizeof(int) );
	WriteData( pin, offset, &pmesh->npolys	,		sizeof(int) );
	WriteData( pin, offset, &pmesh->maxpolys,		sizeof(int) );
	WriteData( pin, offset, &pmesh->nvp		,		sizeof(int) );

	WriteData( pin, offset, &pmesh->bmin[0],		sizeof(float) * 3 );
	WriteData( pin, offset, &pmesh->bmax[0],		sizeof(float) * 3 );
	WriteData( pin, offset, &pmesh->cs,				sizeof(float) );
	WriteData( pin, offset, &pmesh->ch,				sizeof(float) );

	WriteData( pin, offset, &pmesh->borderSize,		sizeof(int) );
	WriteData( pin, offset, &pmesh->maxEdgeError,	sizeof(float) );

	int test = 0x002ECA57;

	WriteData( pin, offset, pmesh->verts,	sizeVerts	 ); WriteData( pin, offset, &test, 4 );
	WriteData( pin, offset, pmesh->polys,	sizePolys	 ); WriteData( pin, offset, &test, 4 );
	WriteData( pin, offset, pmesh->regs,	sizeRegs	 ); WriteData( pin, offset, &test, 4 );
	WriteData( pin, offset, pmesh->flags,	sizeFlags	 ); WriteData( pin, offset, &test, 4 );
	WriteData( pin, offset, pmesh->areas,	sizeAreas	 ); WriteData( pin, offset, &test, 4 );

	return data;
}


rcPolyMesh *Utils::DeserializePolyMesh(array<System::Byte> ^data)
{
	rcPolyMesh *pmesh = rcAllocPolyMesh();

	pin_ptr<System::Byte> pin = &data[0];
	char *ptr	=	(char*)(void*)(pin);

	ReadData( &ptr, &pmesh->nverts		,	sizeof(int) );
	ReadData( &ptr, &pmesh->npolys		,	sizeof(int) );
	ReadData( &ptr, &pmesh->maxpolys	,	sizeof(int) );
	ReadData( &ptr, &pmesh->nvp			,	sizeof(int) );

	ReadData( &ptr, &pmesh->bmin[0],		sizeof(float) * 3 );
	ReadData( &ptr, &pmesh->bmax[0],		sizeof(float) * 3 );
	ReadData( &ptr, &pmesh->cs,				sizeof(float) );
	ReadData( &ptr, &pmesh->ch,				sizeof(float) );

	ReadData( &ptr, &pmesh->borderSize,		sizeof(int) );
	ReadData( &ptr, &pmesh->maxEdgeError,	sizeof(float) );

	int sizeVerts	=	pmesh->nverts * 3 * sizeof(short);
	int sizePolys	=	pmesh->npolys * 2 * pmesh->nvp * sizeof(short);
	int sizeRegs	=	pmesh->npolys * sizeof(short);
	int sizeFlags	=	pmesh->npolys * sizeof(short);
	int sizeAreas	=	pmesh->npolys * sizeof(char);

	pmesh->verts	=	(unsigned short*)rcAlloc( sizeVerts	, RC_ALLOC_PERM );
	pmesh->polys	=	(unsigned short*)rcAlloc( sizePolys	, RC_ALLOC_PERM );
	pmesh->regs		=	(unsigned short*)rcAlloc( sizeRegs	, RC_ALLOC_PERM );
	pmesh->flags	=	(unsigned short*)rcAlloc( sizeFlags	, RC_ALLOC_PERM );
	pmesh->areas	=	(unsigned char* )rcAlloc( sizeAreas	, RC_ALLOC_PERM );

	int test = 0;

	ReadData( &ptr, pmesh->verts,	sizeVerts	 ); ReadData( &ptr, &test, 4 ); // Log::Warning("{0:X8}", test);
	ReadData( &ptr, pmesh->polys,	sizePolys	 ); ReadData( &ptr, &test, 4 ); // Log::Warning("{0:X8}", test);
	ReadData( &ptr, pmesh->regs,	sizeRegs	 ); ReadData( &ptr, &test, 4 ); // Log::Warning("{0:X8}", test);
	ReadData( &ptr, pmesh->flags,	sizeFlags	 ); ReadData( &ptr, &test, 4 ); // Log::Warning("{0:X8}", test);
	ReadData( &ptr, pmesh->areas,	sizeAreas	 ); ReadData( &ptr, &test, 4 ); // Log::Warning("{0:X8}", test);

	return pmesh;
}


