using namespace System;


namespace Native {
	namespace Embree {
		ref class RtcException : public Exception {

		internal:
		String^ CodeToString (RTCError error) {
			switch (error)	{
			case RTC_NO_ERROR:
				return gcnew String("No error occurred.");
				break;
			case RTC_UNKNOWN_ERROR:
				return gcnew String("An unknown error has occurred.");
				break;
			case RTC_INVALID_ARGUMENT:
				return gcnew String("An invalid argument was specified.");
				break;
			case RTC_INVALID_OPERATION:
				return gcnew String("The operation is not allowed for the specified object.");
				break;
			case RTC_OUT_OF_MEMORY:
				return gcnew String("There is not enough memory left to complete the operation.");
				break;
			case RTC_UNSUPPORTED_CPU:
				return gcnew String("The CPU is not supported as it does not support SSE2.");
				break;
			case RTC_CANCELLED:
				return gcnew String("The operation got cancelled by an Memory Monitor Callback or Progress Monitor Callback function.");
				break;
			default:
				return gcnew String("Bad error code.");
				break;
			}
		}

		static void CheckError ()
		{
			auto error = rtcGetError();

			throw gcnew RtcException( CodeToString( error ) );
		}

		public:

			RtcException(String^ error) : Exception(error)
			{
			}

			

		};
	}
}