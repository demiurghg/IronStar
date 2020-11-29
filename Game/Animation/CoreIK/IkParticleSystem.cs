using System;
using System.Collections.Generic;
using Fusion.Core.Mathematics;



namespace CoreIK {
	public class IkParticleSystem {
		
		struct Link {
			public int		id0, id1;
			public float	min, max;
		}

		struct ExtLink {
			public int		id;
			public Vector3	point;
			public float	min;
			public float	max;
		}

		List<Vector3>	particles	=	new List<Vector3>();
		List<Link>		links		=	new List<Link>();
		List<ExtLink>	xlinks		=	new List<ExtLink>();


		public IkParticleSystem ( int particlesCount ) 
		{
			for (int i=0; i<particlesCount; i++) {
				AddParticle( Vector3.Zero );
			}
		}
		

		public int AddParticle	( Vector3 point ) 
		{
			particles.Add(point);
			return particles.Count-1;
		}


		public void AddLink ( int id0, int id1, float distance )
		{
			Link link;
			link.id0	=	id0;
			link.id1	=	id1;
			link.min	=	distance - 0.0001f;
			link.max	=	distance + 0.0001f;
			links.Add( link );
		}


		public void AddLink ( int id0, int id1, float min, float max )
		{
			Link link;
			link.id0	=	id0;
			link.id1	=	id1;
			link.min	=	min;
			link.max	=	max;
			links.Add( link );
		}


		public void AddExtLink ( int id, Vector3 point, float min, float max )
		{
			ExtLink link;
			link.id			=	id;
			link.point		=	point;
			link.min		=	min;
			link.max		=	max;
			xlinks.Add( link );
		}


		public void SetParticle ( int id, Vector3 point )
		{
			particles[id] = point;
		}


		public Vector3 GetParticle ( int id ) 
		{
			return particles[id];
		}


		float Tension ( float dist, float min, float max )
		{
			dist = MathUtil.Clamp( dist, min, max );
			dist -= min;
			dist /= (max-min);
			return MathUtil.SmoothStep( -1, 1, dist );
		}


		public void Solve ( int iterationCount, Vector3 shiftVector ) 
		{
			for (int k=0; k<iterationCount; k++) {

				for (int i=0; i<xlinks.Count; i++) {

					var id			= xlinks[i].id;
					var min			= xlinks[i].min;
					var max			= xlinks[i].max;
					var point		= xlinks[i].point;
								
					var dir			= particles[id] - point;
					var dl			= dir.Length();

					if ( dl > max ) {
						particles[id] = point + dir / dl * max;
					}
					if ( dl < min ) {
						particles[id] = point + dir / dl * min;
					}
				}

				for (int i=0; i<links.Count; i++) {

					var i0			= links[i].id0;
					var i1			= links[i].id1;
					var min			= links[i].min;
					var max			= links[i].max;
								
					var dir			= particles[i1] - particles[i0];
					var dl			= dir.Length();
					var tension		= 0.5f;//Tension( dl, min, max ) / 2;


					if ( dl > max ) {
						var diff	  = ( dl - max ) / dl;
						particles[i1] = particles[i1] - tension * dir * diff;
						particles[i0] = particles[i0] + tension * dir * diff;
					} else
					if ( dl < min ) {
						var diff	  = ( dl - min ) / dl;
						particles[i1] = particles[i1] - tension * dir * diff;
						particles[i0] = particles[i0] + tension * dir * diff;
					} else {
						
					}

					//var diff		= ( dl - L ) / dl;
					//particles[i0]	= particles[i0] + dir * ( 0.3f * diff );
					//particles[i1]	= particles[i1] - dir * ( 0.3f * diff );					
					//var dir			= particles[i1] - particles[i0];
					//var dl			= dir.Length();
					//var diff		= ( dl - L ) / dl;
					//particles[i0]	= particles[i0] + dir * ( 0.3f * diff );
					//particles[i1]	= particles[i1] - dir * ( 0.3f * diff );
				}
			}

			xlinks.Clear();
		}


		//public void AddParticleAndLinkFromSkeleton ( IkSkeleton skel, string config )
		//{
		//    var bonePairs = config.Split( new char[]{';'} );
			
		//}


		public void Draw ( DebugRender dr ) 
		{
			foreach (var p in particles) {
				dr.DrawPoint( p, 0.1f, Color.Cyan );
			}

			foreach (var link in links) {
				var x0	= particles[ link.id0 ];
				var x1	= particles[ link.id1 ];
				dr.DrawLine( x0, x1, Color.Cyan );
			}
		}
	}
}
