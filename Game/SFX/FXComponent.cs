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

namespace IronStar.SFX 
{
	public class FXComponent : IComponent
	{
		public string	FXName;
		public bool		Looped;

		public FXComponent( string fxName, bool looped )
		{
			FXName	=	fxName;
			Looped	=	looped;
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( FXName );
			writer.Write( Looped );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			FXName	=	reader.ReadString();
			Looped	=	reader.ReadBoolean();
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
