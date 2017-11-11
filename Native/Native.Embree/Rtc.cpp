#include "local.h"
#include <xmmintrin.h>
#include <pmmintrin.h>

Native::Embree::Rtc::Rtc()
{
	//_MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);
	//_MM_SET_DENORMALS_ZERO_MODE(_MM_DENORMALS_ZERO_ON);

	device	=	rtcNewDevice();
}


Native::Embree::Rtc::~Rtc()
{
	rtcDeleteDevice(device);
}
