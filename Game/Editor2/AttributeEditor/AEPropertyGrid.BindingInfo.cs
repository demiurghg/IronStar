using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		class BindingInfo {

			public readonly string		Category;
			public readonly string		Name;
			public readonly object		TargetObject;
			public readonly MemberInfo	TargetMember;

			public BindingInfo ( string category, string name, object obj, MemberInfo mi )
			{
				this.Category		=	category;
				this.Name			=	name;
				this.TargetObject	=	obj;
				this.TargetMember	=	mi;
			}
		}
	}
}
