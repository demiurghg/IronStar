using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Animation;

namespace IronStar.Gameplay.Components
{
	public class CameraComponent : IComponent
	{
		public Matrix	AnimTransform;

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

		public void Load( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}

		public void Save( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}
	}
}
