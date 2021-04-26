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
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Engine.Audio;
using KopiLua;
using Fusion.Scripting;
using IronStar.ECS;

namespace IronStar.SFX2 
{
	[Flags]
	public enum RMFlags
	{	
		None			=	0x0000,
		FirstPointView	=	0x0001,
	}

	public partial class RenderModel : Component
	{
		//	pure component data :
		public string	scenePath;
		public Matrix	transform;
		public Color	color;
		public float	intensity;
		public RMFlags	rmFlags;

		public Size2	lightmapSize;
		public string	lightmapName;

		public string	cmPrefix;

		public bool		NoShadow { get; set; }
		public bool		UseLightMap { get { return lightmapSize.Width>0 && lightmapSize.Height>0; } }


		public RenderModel ()
		{
		}


		public RenderModel ( string scenePath, Matrix transform, Color color, float intensity, RMFlags flags )
		{
			this.scenePath	=	scenePath	;
			this.transform	=	transform	;
			this.color		=	color		;
			this.intensity	=	intensity	;
			this.rmFlags	=	flags		;
		}


		public RenderModel ( string scenePath, float scale, Color color, float intensity, RMFlags flags )
		:this( scenePath, Matrix.Scaling(scale), color, intensity, flags )
		{
		}


		public void SetupLightmap( int width, int height, string regionId )
		{
			lightmapSize	=	new Size2( width, height );
			lightmapName	=	regionId;
		}


		public bool AcceptCollisionNode ( Node node )
		{	  
			return (string.IsNullOrWhiteSpace(cmPrefix)) ? true : node.Name.StartsWith(cmPrefix);
		}

		public bool AcceptVisibleNode ( Node node )
		{
			return (string.IsNullOrWhiteSpace(cmPrefix)) ? true : !node.Name.StartsWith(cmPrefix);
		}


		public override void Added( GameState gs, Entity entity ) {}
		public override void Removed( GameState gs ) {}
		public override void Load( GameState gs, Stream stream ) {}
		public override void Save( GameState gs, Stream stream ) {}

	}
}
