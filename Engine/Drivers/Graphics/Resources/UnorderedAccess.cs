using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace Fusion.Drivers.Graphics 
{
	public class UnorderedAccess : GraphicsObject
	{
		internal UnorderedAccessView Uav;
		
		internal UnorderedAccess( GraphicsDevice device, UnorderedAccessView uav ) : base( device )
		{
			Uav = uav;
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref Uav );
			}

			base.Dispose( disposing );
		}
	}
}
