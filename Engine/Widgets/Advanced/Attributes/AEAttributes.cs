using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Widgets.Binding;

namespace Fusion.Core.Shell {

	[Obsolete]
	[AttributeUsage(AttributeTargets.Property)]
	public class AEValueRangeAttribute : Attribute {

		public readonly float Min;
		public readonly float Max;
		public readonly float RoughStep;
		public readonly float PreciseStep;

		public AEValueRangeAttribute( float min, float max, float roughStep, float preciseStep ) 
		{
			this.Min			=	min;
			this.Max			=	max;
			this.RoughStep		=	roughStep;
			this.PreciseStep	=	preciseStep;
		}
	}


	[Obsolete]
	public class AEClassnameAttribute : Attribute {
		public readonly string Directory;
		public AEClassnameAttribute( string dir )
		{
			Directory = dir;
		}
	}



	public enum AEFileNameMode {
		NoExtension = 0x0001,
		FileNameOnly = 0x0002,
	}



	[Obsolete]
	public class AEFileNameAttribute : Attribute {
		public readonly string Directory;
		public readonly string Extension;
		public readonly bool FileNameOnly;
		public readonly bool NoExtension;

		public AEFileNameAttribute( string dir, string ext, AEFileNameMode fileNameMode )
		{
			Directory = dir;
			Extension = ext;
			FileNameOnly = fileNameMode.HasFlag( AEFileNameMode.FileNameOnly );
			NoExtension  = fileNameMode.HasFlag( AEFileNameMode.NoExtension );
		}
	}


	[Obsolete]
	public class AEAtlasImageAttribute : Attribute {
		public readonly string AtlasName;

		public AEAtlasImageAttribute( string atlasName )
		{
			AtlasName = atlasName;
		}
	}
}
