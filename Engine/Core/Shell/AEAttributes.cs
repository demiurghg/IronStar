using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Binding;

namespace Fusion.Core.Shell {

	[AttributeUsage(AttributeTargets.Property|AttributeTargets.Method)]
	public class AECategoryAttribute : Attribute {

		public readonly string Category;

		public AECategoryAttribute( string category ) 
		{
			this.Category = category;
		}
	}


	[AttributeUsage(AttributeTargets.Property|AttributeTargets.Method)]
	public class AEDisplayNameAttribute : Attribute {

		public readonly string Name;

		public AEDisplayNameAttribute( string name ) 
		{
			this.Name = name;
		}
	}


	[AttributeUsage(AttributeTargets.Method)]
	public class AECommandAttribute : Attribute {
	}


	[AttributeUsage(AttributeTargets.Property)]
	public class AEExpandableAttribute : Attribute {
	}


	[AttributeUsage(AttributeTargets.Property)]
	public class AEIgnoreAttribute : Attribute {
	}


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


	public class AEClassnameAttribute : Attribute {
		public readonly string Directory;
		public AEClassnameAttribute( string dir )
		{
			Directory = dir;
		}
	}



	public abstract class AEExternalEditorAttribute : Attribute {
		public abstract void RunEditor ( IValueBinding binding );
	}


	public abstract class AEValueProviderAttribute : Attribute {
		public abstract string[] GetValues ();
	}



	public enum AEFileNameMode {
		NoExtension = 0x0001,
		FileNameOnly = 0x0002,
	}



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


	public class AEAtlasImageAttribute : Attribute {
		public readonly string AtlasName;

		public AEAtlasImageAttribute( string atlasName )
		{
			AtlasName = atlasName;
		}
	}
}
