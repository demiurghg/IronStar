using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class AttachmentComponent : IComponent
	{
		public Entity Target { get; set; } = null;
		public Matrix LocalTransform { get; set; } = Matrix.Identity;

		public AttachmentComponent()
		{
		}

		public AttachmentComponent( Entity target )
		{
			Target		=	target;
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.WriteEntity( gs, Target );
			writer.Write( LocalTransform );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Target			=	reader.ReadEntity(gs);
			LocalTransform	=	reader.Read<Matrix>();
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
