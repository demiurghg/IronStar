using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.SFX2;
using Native.NRecast;
using System.ComponentModel;
using Fusion.Core.Extensions;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Build;
using IronStar.Gameplay;

namespace IronStar.AI
{
	class NavSystem : ISystem
	{
		readonly IGameState	gs;
		readonly string		navMeshPath;
		NavMesh				navMesh			=	null;

		readonly Aspect	navigatorAspect	=	new Aspect().Include<AIComponent,Transform,CharacterController,UserCommandComponent>();


		public NavSystem(IGameState gs, string mapName)
		{
			this.gs				=	gs;
			this.navMeshPath	=	Path.Combine("maps", "navmesh", mapName);

			LoadContent();

			gs.Game.Reloading += (s,e) => LoadContent();
		}


		void LoadContent()
		{
			if (!gs.Content.TryLoad(navMeshPath, out navMesh))
			{
				Log.Warning("Missing navmesh data: {0}", navMeshPath);
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	System stuff
		-----------------------------------------------------------------------------------------*/

		public Aspect GetAspect()
		{
			return navigatorAspect;
		}

		
		public void Add( IGameState gs, Entity e )
		{
		}

		
		public void Remove( IGameState gs, Entity e )
		{
		}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			//	#TODO #AI #NAVIGATION -- control doors here
		}

		/*-----------------------------------------------------------------------------------------
		 *	Navmesh queries :
		-----------------------------------------------------------------------------------------*/

		public Vector3[] FindRoute( Vector3 startPoint, Vector3 endPoint )
		{
			return navMesh?.FindRoute( startPoint, endPoint );
		}

		public Vector3 GetReachablePointInRadius( Vector3 startPoint, float maxRadius )
		{
			if (navMesh==null) return startPoint;

			Vector3 result = startPoint;

			navMesh.GetRandomReachablePoint( startPoint, maxRadius, ref result );

			return result;
		}
	}
}
