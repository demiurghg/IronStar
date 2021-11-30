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
	public class SoundComponent : IComponent
	{
		public string	SoundName;
		public bool		Looped;
		public float	Timeout;

		public SoundComponent() : this("", false) {}

		public SoundComponent( string soundName, bool looped )
		{
			SoundName	=	soundName;
			Looped		=	looped;
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( SoundName );
			writer.Write( Looped );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			SoundName	=	reader.ReadString();
			Looped		=	reader.ReadBoolean();
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
