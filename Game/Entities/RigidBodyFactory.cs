using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using Fusion.Core.Shell;
using Fusion.Widgets.Advanced;


namespace IronStar 
{
	public class RigidBodyFactory : EntityFactoryContent 
	{
		[AECategory("Physics")]
		public float  Width  { get; set; } = 1;
		[AECategory("Physics")]
		public float  Height { get; set; } = 1;
		[AECategory("Physics")]
		public float  Depth  { get; set; } = 1;
		[AECategory("Physics")]
		public float  Mass   { get; set; } = 1;

		
		[AECategory("Appearance")]
		public string Model  { get; set; } = "";


		[AECategory("Explosiveness")]
		public bool Explosive  { get; set; } = false;

		[AECategory("Explosiveness")]
		public bool ExplodeOnTrigger  { get; set; } = false;

		[AECategory("Explosiveness")]
		public bool ExplodeOnDamage  { get; set; } = false;

		[AECategory("Explosiveness")]
		public string ExplosionFX  { get; set; } = "";

		[AECategory("Explosiveness")]
		public string BurningFX  { get; set; } = "";

		[AECategory("Explosiveness")]
		public int BurningMaxTime  { get; set; } = 100;

		[AECategory("Explosiveness")]
		public int BurningMinTime  { get; set; } = 500;

		[AECategory("Explosiveness")]
		public int Health  { get; set; } = 100;

		[AECategory("Explosiveness")]
		public int ExplosionDamage  { get; set; } = 100;

		[AECategory("Explosiveness")]
		public float ExplosionImpulse  { get; set; } = 10;

		[AECategory("Explosiveness")]
		public float ExplosionRadius  { get; set; } = 3;



		public override ECS.Entity SpawnECS( ECS.IGameState gs, Vector3 p, Quaternion r )
		{
			var e = gs.Spawn();

			e.AddComponent( new ECS.Transform(p,r) );
			e.AddComponent( new ECSPhysics.DynamicBox( Width, Height, Depth, Mass ) );
			e.AddComponent( new SFX2.RenderModel("scenes\\boxes\\box_low.fbx", Matrix.Scaling(3), new Color(48, 96, 255), 7, SFX2.RMFlags.None ) );

			return e;
		}


		public override void Draw( DebugRender dr, Matrix transform, Color color, bool selected )
		{
			var w = Width/2;
			var h = Height/2;
			var d = Depth/2;

			dr.DrawBox( new BoundingBox( new Vector3(-w, -h, -d), new Vector3(w, h, d) ), transform, color );
			dr.DrawPoint( transform.TranslationVector, (w+h+d)/3/2, color );
		}
	}
}
