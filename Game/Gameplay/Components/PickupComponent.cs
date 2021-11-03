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


		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( FXName );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			FXName	=	reader.ReadString();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}
	}
}
