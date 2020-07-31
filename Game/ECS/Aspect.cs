using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public sealed class Aspect
	{
		long	includeSet	=	0;
		long	excludeSet	=	0;
		long	singleSet	=	0;
		long	anySet		=	0;	

		public Aspect()
		{
		}


		void Reset()
		{
			includeSet	= 0;
			excludeSet	= 0;
			singleSet	= 0;
			anySet		= 0;
		}


		public static Aspect Empty()
		{
			return new Aspect();
		}


		public bool IsConsistent()
		{
			if ( ( includeSet & excludeSet ) != 0 ) return false;
			if ( ( excludeSet & singleSet  ) != 0 ) return false;
			if ( ( singleSet  & anySet     ) != 0 ) return false;
			if ( ( anySet     & includeSet ) != 0 ) return false;

			if ( ( includeSet & singleSet  ) != 0 ) return false;
			if ( ( excludeSet & anySet     ) != 0 ) return false;

			return true;
		}



		bool IsPowerOfTwo( long x )
		{
			return ((x & (x - 1)) == 0) && (x!=0);
		}


		public bool Accept( Entity e )
		{
			long mapping = e.ComponentMapping;

			return (
				( ( includeSet == 0 ) || ( includeSet  == (mapping & includeSet ) ) ) &&
				( ( excludeSet == 0 ) || ( 0           == (mapping & excludeSet ) ) ) &&
				( ( anySet     == 0 ) || ( 0           != (mapping & anySet     ) ) ) &&
				( ( singleSet  == 0 ) || ( IsPowerOfTwo   (mapping & singleSet  ) ) )
			);
		}


		public Aspect Include( params Type[] types )
		{
			foreach ( var type in types ) includeSet |= ECSTypeManager.GetComponentBit(type);
			return this;
		}


		public Aspect Exclude( params Type[] types )
		{
			foreach ( var type in types ) excludeSet |= ECSTypeManager.GetComponentBit(type);
			return this;
		}


		public Aspect Single( params Type[] types )
		{
			foreach ( var type in types ) singleSet |= ECSTypeManager.GetComponentBit(type);
			return this;
		}


		public Aspect Any( params Type[] types )
		{
			foreach ( var type in types ) anySet |= ECSTypeManager.GetComponentBit(type);
			return this;
		}


		public Aspect Include<T1>()				{ return Include( typeof(T1) ); }
		public Aspect Include<T1,T2>()			{ return Include( typeof(T1), typeof(T2) ); }
		public Aspect Include<T1,T2,T3>()		{ return Include( typeof(T1), typeof(T2), typeof(T3) ); }
		public Aspect Include<T1,T2,T3,T4>()	{ return Include( typeof(T1), typeof(T2), typeof(T3), typeof(T4) ); }

		public Aspect Exclude<T1>()				{ return Exclude( typeof(T1) ); }
		public Aspect Exclude<T1,T2>()			{ return Exclude( typeof(T1), typeof(T2) ); }
		public Aspect Exclude<T1,T2,T3>()		{ return Exclude( typeof(T1), typeof(T2), typeof(T3) ); }
		public Aspect Exclude<T1,T2,T3,T4>()	{ return Exclude( typeof(T1), typeof(T2), typeof(T3), typeof(T4) ); }

		public Aspect Single<T1>()				{ return Single( typeof(T1) ); }
		public Aspect Single<T1,T2>()			{ return Single( typeof(T1), typeof(T2) ); }
		public Aspect Single<T1,T2,T3>()		{ return Single( typeof(T1), typeof(T2), typeof(T3) ); }
		public Aspect Single<T1,T2,T3,T4>()		{ return Single( typeof(T1), typeof(T2), typeof(T3), typeof(T4) ); }

		public Aspect Any<T1>()					{ return Any( typeof(T1) ); }
		public Aspect Any<T1,T2>()				{ return Any( typeof(T1), typeof(T2) ); }
		public Aspect Any<T1,T2,T3>()			{ return Any( typeof(T1), typeof(T2), typeof(T3) ); }
		public Aspect Any<T1,T2,T3,T4>()		{ return Any( typeof(T1), typeof(T2), typeof(T3), typeof(T4) ); }
	}
}
