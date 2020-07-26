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


	public class FXComponent : ECS.IComponent, ECS.ITransformable
	{
		public string FXName;

		public bool Looped;

		FXEvent		fxEvent;
		FXInstance	fxInstance;
		FXPlayback	fxPlayback;

		public bool IsExhausted { get { return fxInstance.IsExhausted; } }


		public FXComponent( string fxName, bool looped )
		{
			FXName	=	fxName;
			Looped	=	looped;
		}


		public void Added( GameState gs, Entity entity )
		{
			fxPlayback	=	gs.GetService<FXPlayback>();
			fxEvent		=	new FXEvent();
		}


		public void Removed( GameState gs )
		{
			fxInstance.Kill();
		}


		public void SetTransform( Matrix transform )
		{
			Vector3 t, s;
			Quaternion r;
			transform.Decompose( out s, out r, out t );
			fxEvent.Origin		=	t;
			fxEvent.Rotation	=	r;
			fxEvent.Scale		=	0.333f * (s.X + s.Y + s.Z);

			//	run FX instance when we get transform
			//	otherwice sound will be cracky.
			if (fxInstance==null)
			{
				fxInstance	=	fxPlayback.RunFX( FXName, fxEvent, Looped, true );
			}
		}


		public void UpdateFXState ( GameTime gameTime )
		{
			fxInstance.UpdateECS( gameTime.ElapsedSec );
		}


		public void Save( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}

		
		public void Load( GameState gs, Stream stream )
		{
			throw new NotImplementedException();
		}
	}
}
