using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	class NoiseComponent : IComponent
	{
		//	Noise radius, constantly decaying
		public float Level = 0;

		/// <summary>
		/// Source of the noise. I.e. source of the noise attached to rocket is a attacker.
		/// If NULL the entity is source.
		/// </summary>
		//public Entity Source = null;

		
		public NoiseComponent()
		{
		}

		public void MakeNoise(float level)
		{
			Level	=	Math.Max( Level, level );
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			//Source	=	reader.ReadEntity(gs);
			Level	=	reader.ReadSingle();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			//writer.WriteEntity( gs, Source );
			writer.Write( Level );
		}
	}
}
