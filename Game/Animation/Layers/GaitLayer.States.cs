using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Scripting;
using KopiLua;
using IronStar.Animation;
using IronStar.Gameplay.Components;

namespace IronStar.Animation 
{
	public partial class GaitLayer : BaseLayer 
	{
		abstract class LocoState
		{
			readonly protected GaitLayer fb;

			public LocoState( GaitLayer fb )
			{
				this.fb	=	fb;
			}

			public virtual void Timeout() {}
			public virtual void StartMovement() {}
			public virtual void StopMovement() {}

			public void SetTimeout( float seconds )
			{
				timer = 0;
				period = seconds;
			}

			public void UpdateInternal( float dt, Vector3 groundVelocity )
			{
				if (timer>=period) Timeout();
				timer += dt;
			}

			public abstract void Enter();
			public abstract void Update(float dt, Vector3 groundVelocity);
			public abstract void Leave();
		}


		class StanceState
		{
			StanceState
		}
	}
}
