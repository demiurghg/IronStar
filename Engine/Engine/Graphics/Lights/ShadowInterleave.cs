using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics 
{
	public class ShadowInterleave
	{
		public static bool CascadeInterleaveNone( int frame, int index )
		{
			return true;
		}

		public static bool CascadeInterleave1122( int frame, int index )
		{
			if (index<2) return true;
			
			if (((index+frame)&1)==1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool CascadeInterleave1244( int frame, int index )
		{
			frame %= 4;

			if (frame==0) return (index==0) || (index==1);
			if (frame==1) return (index==0) || (index==2);
			if (frame==2) return (index==0) || (index==1);
			if (frame==3) return (index==0) || (index==3);

			return true;
		}
	}
}
