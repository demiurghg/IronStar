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
using IronStar.Core;
using Fusion.Engine.Audio;
using IronStar.Views;
using KopiLua;
using Fusion.Scripting;

namespace IronStar.SFX {

	public partial class ModelInstance {


		/// <summary>
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("load")]
		int Load ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {
				var path	=	Lua.LuaToString( L, 1 ).ToString();

				LoadScene( path );
			}
			return 0;
		}



		[LuaApi("setColor")]
		int SetColor ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {

				var r		=	(byte)(int)Lua.LuaToNumber( L, 1 );
				var g		=	(byte)(int)Lua.LuaToNumber( L, 2 );
				var b		=	(byte)(int)Lua.LuaToNumber( L, 3 );

				color		=	new Color( r,g,b,(byte)255 );

				glowColor	=	color.ToColor4() * intensity;
					
			}
			return 0;
		}



		[LuaApi("setIntensity")]
		int SetIntensity ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {

				intensity	=	(float)Lua.LuaToNumber( L, 1 );
					
				glowColor	=	color.ToColor4() * intensity;

			}
			return 0;
		}



		[LuaApi("setScale")]
		int SetScale ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {

				var scale		=	Lua.LuaToNumber( L, 1 );
				PreTransform	=	Matrix.Scaling( (float)scale );
					
			}
			return 0;
		}


		[LuaApi("setFpv")]
		int SetFPV( LuaState L )
		{
			using ( new LuaStackGuard( L ) ) {

				float scale;

				fpvEnabled	=	Lua.LuaToBoolean(L, 1)!=0;
				scale		=	(float)Lua.LuaToNumber(L, 2);
				fpvCamera	=	Lua.LuaToString(L, 3).ToString();

				if (fpvEnabled) {
					fpvCameraIndex		=	scene.GetNodeIndex( fpvCamera );

					if (fpvCameraIndex<0) {	
						Log.Warning("Camera node {0} does not exist", fpvCamera);
					} else {
						fpvCameraMatrix	=	Scene.FixGlobalCameraMatrix( globalTransforms[ fpvCameraIndex ] );
						fpvViewMatrix	=	Matrix.Invert( fpvCameraMatrix );
						PreTransform	=	fpvViewMatrix * Matrix.Scaling( scale );
					}
				} else {
					PreTransform	=	Matrix.Scaling( scale );	
				}
			}
			return 0;
		}

	}
}
