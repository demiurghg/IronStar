using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class TriggerComponent : IComponent
	{
		public string	Name = "";
		private Entity	Activator;
		private bool	External;

		public void Set( Entity activator, bool external )
		{
			Activator	=	activator;
			External	=	external;
		}


		public void Reset()
		{
			Activator	=	null;
			External	=	false;
		}


		public bool IsSet( out Entity activator )
		{
			activator = Activator;
			return activator!=null;
		}

		public bool IsSet( out Entity activator, out bool external )
		{
			activator	=	Activator;
			external	=	External;
			return activator!=null;
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Name );
			writer.WriteEntity( gs, Activator );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Name		=	reader.ReadString();
			Activator	=	reader.ReadEntity(gs);
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}
	}
}
