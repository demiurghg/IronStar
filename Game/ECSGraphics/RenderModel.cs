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
		None				=	0x0000,
		Static				=	0x0001,
		FirstPointView		=	0x0002,
		UseLightmap			=	0x0004,
		NoShadow			=	0x0008,
		UseLightmapProxy	=	0x0010,
		UseCollisionProxy	=	0x0020,
	}

	public partial class RenderModel : IComponent
	{
		public const string	CollisionProxyPrefix	=	"collisionProxy";
		public const string	LightmapProxyPrefix		=	"lightmapProxy";

		public string	ScenePath;
		public Matrix	Transform;
		public Color	Color;
		public float	Intensity;
		public RMFlags	rmFlags;

		public Size2	LightmapSize;
		public string	LightmapName	=	"";

		public bool IsStatic			{ get { return rmFlags.HasFlag(RMFlags.Static);				} }
		public bool UseCollisionProxy	{ get { return rmFlags.HasFlag(RMFlags.UseCollisionProxy);	} }
		public bool UseLightmapProxy	{ get { return rmFlags.HasFlag(RMFlags.UseLightmapProxy);	} }
		public bool UseLightmap			{ get { return rmFlags.HasFlag(RMFlags.UseLightmap);		} }
		public bool NoShadow			{ get { return rmFlags.HasFlag(RMFlags.NoShadow);			} }


		public RenderModel ()
		{
		}


		public RenderModel ( string scenePath, Matrix transform, Color color, float intensity, RMFlags flags )
		{
			this.ScenePath	=	scenePath	;
			this.Transform	=	transform	;
			this.Color		=	color		;
			this.Intensity	=	intensity	;
			this.rmFlags	=	flags		;
		}


		public RenderModel ( string scenePath, float scale, Color color, float intensity, RMFlags flags )
		:this( scenePath, Matrix.Scaling(scale), color, intensity, flags )
		{
		}


		public void SetupLightmap( int width, int height, string regionId )
		{
			LightmapSize	=	new Size2( width, height );
			LightmapName	=	regionId;
		}


		public bool AcceptCollisionNode ( Node node )
		{
			return UseCollisionProxy ? node.Name.StartsWith(CollisionProxyPrefix) : true;
		}

		public bool AcceptVisibleNode ( Node node )
		{
			return UseCollisionProxy ? !node.Name.StartsWith(CollisionProxyPrefix) : true;
		}

		public bool AcceptLightmapNode ( Node node )
		{
			return UseLightmap && ( UseLightmapProxy ? !node.Name.StartsWith(LightmapProxyPrefix) : true );
		}

		public bool AcceptLightmapProxyNode ( Node node )
		{
			return UseLightmap && ( UseLightmapProxy ? node.Name.StartsWith(LightmapProxyPrefix) : true );
		}

		public Scene LoadScene( IGameState gs )
		{
			Scene scene;

			if (!gs.Content.TryLoad( ScenePath, out scene ))
			{
				scene	=	Scene.CreateEmptyScene();
			}

			return scene;
		}


		public float ComputeScale()
		{
			float scale;
			Quaternion r;
			Vector3 t;
			this.Transform.DecomposeUniformScale( out scale, out r, out t );
			return scale;
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( ScenePath		);
			writer.Write( Transform		);
			writer.Write( Color			);
			writer.Write( Intensity		);
			writer.Write( (int)rmFlags	);

			writer.Write( LightmapSize	);
			writer.Write( LightmapName	);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			ScenePath		=	reader.ReadString();
			Transform		=	reader.ReadMatrix();
			Color			=	reader.ReadColor();
			Intensity		=	reader.ReadSingle();
			rmFlags			=	(RMFlags)reader.ReadInt32();

			LightmapSize	=	reader.ReadSize2();
			LightmapName	=	reader.ReadString();
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
