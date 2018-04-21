using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using Fusion.Core.Shell;

namespace IronStar.Mapping {


	public class MapOmniLight : MapNode {

		//[Category("Decal Image")]
		//[Editor( typeof( SpotFileLocationEditor ), typeof( UITypeEditor ) )]
		//public string SpotMaskName { get; set; } = "";
		
		
		[AECategory("Omni-light")]
		[AEValueRange(0, 1000, 10, 0.1f)]
		public float Intensity { get; set; } = 500;
		
		[AECategory("Omni-light")]
		[AEValueRange(0, 50, 1, 0.1f)]
		public float OuterRadius { get; set; } = 5;
		
		[AECategory("Omni-light")]
		[AEValueRange(0, 50, 1, 0.1f)]
		public float InnerRadius { get; set; } = 0.1f;

		[AECategory("Omni-light")]
		public LightPreset LightPreset { get; set; } = LightPreset.IncandescentStandard;

		[AECategory("Omni-light")]
		public LightStyle LightStyle { get; set; } = LightStyle.Default;

		[AECategory("Omni-light")]
		public bool Ambient { get; set; } = false;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; } = Color.White;

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AEValueRange(0, 10000, 100, 1)]
		public float LightIntensity { get; set; } = 100;

		[AEFileName("scenes", "*.fbx", AEFileNameMode.NoExtension)]
		public string FileName { get; set; } = "/scenes/model.fbx";

		OmniLight	light;

		[AECommand]
		[AECategory("Omni-light")]
		[AEDisplayName("Do Blah!")]
		public void ComputeLighting ()
		{
			Log.Warning("BLAH!!");
		}


		[AECommand]
		[AECategory("Commands")]
		[AEDisplayName("Do Blah!")]
		public void FlushShaderCache ()
		{
			Log.Warning("BLAH!!");
		}


		[AECommand]
		[AECategory("Commands")]
		[AEDisplayName("Do Blah!")]
		public void Bake ()
		{
			Log.Warning("BLAH!!");
		}


		/// <summary>
		/// 
		/// </summary>
		public MapOmniLight ()
		{
		}



		public override void SpawnNode( GameWorld world )
		{
			if (!world.IsPresentationEnabled) {
				return;
			}

			light		=	new OmniLight();

			light.Intensity		=	LightPresetColor.GetColor( LightPreset, Intensity );;
			light.Position		=	WorldMatrix.TranslationVector;
			light.RadiusOuter	=	OuterRadius;
			light.RadiusInner	=	InnerRadius;
			light.LightStyle	=	LightStyle;
			light.Ambient		=	Ambient;

			world.Game.RenderSystem.RenderWorld.LightSet.OmniLights.Add( light );
		}



		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			var transform	=	WorldMatrix;

			var lightColor	=	LightPresetColor.GetColor( LightPreset, Intensity );

			var max			=	Math.Max( Math.Max( lightColor.Red, lightColor.Green ), Math.Max( lightColor.Blue, 1 ) );

			var dispColor   =	new Color( (byte)(lightColor.Red / max * 255), (byte)(lightColor.Green / max * 255), (byte)(lightColor.Blue / max * 255), (byte)255 ); 

			dr.DrawPoint( transform.TranslationVector, 1, color, 1 );

			if (selected) {
				dr.DrawSphere( transform.TranslationVector, InnerRadius, dispColor );
				dr.DrawSphere( transform.TranslationVector, OuterRadius, dispColor );
			} else {
				dr.DrawSphere( transform.TranslationVector, InnerRadius, dispColor );
			}
		}



		public override void ResetNode( GameWorld world )
		{
			light.Position		=	WorldMatrix.TranslationVector;
		}



		public override void HardResetNode( GameWorld world )
		{
			KillNode( world );
			SpawnNode( world );
		}



		public override void KillNode( GameWorld world )
		{
			world.Game.RenderSystem.RenderWorld.LightSet.OmniLights.Remove( light );
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapOmniLight)MemberwiseClone();
			newNode.light = null;
			return newNode;
		}
	}
}
