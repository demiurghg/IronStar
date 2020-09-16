#pragma once

namespace Native {
	namespace NRecast {
		public ref class NavigationRoute
		{
		private:
			array<Vector3>^ navPoints;

		public:
			NavigationRoute( array<Vector3>^ points )
			{
				if (points==nullptr)
				{
					throw gcnew System::ArgumentNullException("points");
				}

				navPoints	=	gcnew array<Vector3>( points->Length );
				points->CopyTo( navPoints, 0 );
			}

			NavigationRoute( int length )
			{
				navPoints	=	gcnew array<Vector3>( length );
			}

			void SetPoint( int index, float x, float y, float z )
			{
				navPoints[index] = Vector3(x,y,z);
			}

			property int Count
			{
				int get() { return navPoints->Length; }
			}

			Vector3 Last()
			{
				return navPoints[ navPoints->Length - 1 ];
			}

			property Vector3 default[int]
			{
				Vector3 get(int i) { return navPoints[i]; }
			}
		};
	}
}