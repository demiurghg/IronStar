using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Animation;
using Fusion.Core.Extensions;

namespace IronStar.Gameplay.Components
{
	public class CameraComponent : IComponent
	{
		public Matrix	AnimTransform;

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( AnimTransform );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			AnimTransform	=	reader.Read<Matrix>();
		}

		public IComponent Clone()
		{
			return new CameraComponent { AnimTransform = AnimTransform };
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			var prev	=	((CameraComponent)previous).AnimTransform;
			var lerp	=	AnimationUtils.Lerp( prev, AnimTransform, factor );

			return new CameraComponent { AnimTransform = lerp };
		}
	}
}
