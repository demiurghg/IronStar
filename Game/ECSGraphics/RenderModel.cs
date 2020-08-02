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
using IronStar.Views;
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
		public Guid		lightmapGuid;

		public bool		UseLightMap { get { return lightmapSize.Width>0 && lightmapSize.Height>0; } }
		

		//	operational data :
		Scene scene;
		SceneView<RenderInstance> sceneView;


		public RenderModel ( string scenePath, Matrix transform, Color color, float intensity, RMFlags flags )
		{
			this.scenePath	=	scenePath	;
			this.transform	=	transform	;
			this.color		=	color		;
			this.intensity	=	intensity	;
		}


		public void SetupLightmap( int width, int height, Guid regionGuid )
		{
			lightmapSize	=	new Size2( width, height );
			lightmapGuid	=	regionGuid;
		}


		public override void Added( GameState gs, Entity entity ) {}
		public override void Removed( GameState gs ) {}
		public override void Load( GameState gs, Stream stream ) {}
		public override void Save( GameState gs, Stream stream ) {}

	}
}
