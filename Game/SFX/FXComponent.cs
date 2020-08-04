using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.IO;
using IronStar.ECS;

namespace IronStar.SFX {


	public class FXComponent : ECS.IComponent
	{
		public string	FXName;
		public bool		Looped;

		public FXComponent( string fxName, bool looped )
		{
			FXName	=	fxName;
			Looped	=	looped;
		}


		public void Added( GameState gs, Entity entity ) {}
		public void Removed( GameState gs ) {}
		public void Save( GameState gs, Stream stream ) {}
		public void Load( GameState gs, Stream stream ) {}
	}
}
