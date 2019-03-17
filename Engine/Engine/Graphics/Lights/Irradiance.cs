using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Lights {

	public struct Irradiance {

		public static readonly Irradiance Zero = new Irradiance(0);

		public SHL1 Red;
		public SHL1 Green;
		public SHL1 Blue;

		public Irradiance ( Color4 intensity, Vector3 direction )
		{
			var ndir	=	Vector3.Normalize( direction );
			var shl1	=	SHL1.EvaluateDiffuseSimplified( ndir );

			Red		= shl1 * intensity.Red;
			Green	= shl1 * intensity.Green;
			Blue	= shl1 * intensity.Blue;
		}

		private Irradiance ( float zero )
		{
			Red		= SHL1.Zero;
			Green	= SHL1.Zero;
			Blue	= SHL1.Zero;
		}

		public void Add ( Irradiance irradiance )
		{
			Red		+= irradiance.Red;
			Green	+= irradiance.Green;
			Blue	+= irradiance.Blue;
		}

		public void Add ( Color4 intensity, Vector3 direction )
		{
			Add( new Irradiance( intensity, direction ) );
		}
	}
}
