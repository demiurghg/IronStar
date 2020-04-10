// Native.Embree.h

#pragma once

using namespace System;

namespace Native {
	namespace Embree {

		public ref class Rtc {
		internal:

			RTCDevice device;

		public:

			Rtc();
			~Rtc();

		};
	}
}
