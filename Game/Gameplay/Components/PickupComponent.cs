using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class PickupComponent : IComponent
	{
		public string FXName { get; set; } = "";

		public PickupComponent( string fxName )
		{
			FXName	=	fxName;
		}

		public void Load( GameState gs, Stream stream )	{	}
		public void Save( GameState gs, Stream stream )	{	}
	}
}
