#pragma once

namespace Native {
	namespace Embree {

		public value class RtcVector3 {
		public:
			float X;
			float Y;
			float Z;
		};


		public value class RtcRay {
		public:
			RtcVector3	Origin;
			RtcVector3	Direction;
			float		TNear;
			float		TFar;
			float		Time;
			unsigned	Mask;

			RtcVector3	HitNormal;
			float		HitU;
			float		HitV;
			unsigned	GeometryId;
			unsigned	PrimitiveId;
			unsigned	InstanceId;
		};

	}
}
