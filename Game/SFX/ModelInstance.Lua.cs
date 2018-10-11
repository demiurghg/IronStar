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

		[LuaApi("sleep")]
		int Sleep ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {
				sleepTime	=	Lua.LuaToInteger( L, 1 );
			}
			return 0;
		}


		/// <summary>
		/// Load scene with sepcified path
		/// model.load("path");
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



		/// <summary>
		/// Sets model glow color 
		/// setColor(255,128,64)
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("set_color")]
		int SetColor ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {
				var r	=	(byte)(int)Lua.LuaToNumber( L, 1 );
				var g	=	(byte)(int)Lua.LuaToNumber( L, 2 );
				var b	=	(byte)(int)Lua.LuaToNumber( L, 3 );
				color	=	new Color( r,g,b,(byte)255 );
			}
			return 0;
		}



		/// <summary>
		/// Sets model glow intensity
		/// setIntensity(1000)
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("set_intensity")]
		int SetIntensity ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {
				intensity	=	(float)Lua.LuaToNumber( L, 1 );
			}
			return 0;
		}



		/// <summary>
		/// Sets model scale
		/// 
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("set_scale")]
		int SetScale ( LuaState L )
		{
			using ( new LuaStackGuard(L) ) {
				var scale		=	Lua.LuaToNumber( L, 1 );
				preTransform	=	Matrix.Scaling( (float)scale );
			}
			return 0;
		}


		/// <summary>
		/// Sets model FPV parameters
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("set_fpv")]
		int SetFPV( LuaState L )
		{
			using ( new LuaStackGuard( L ) ) {

				fpvEnabled		=	Lua.LuaToBoolean(L, 1)!=0;
				var scale		=	(float)Lua.LuaToNumber(L, 2);
				var camera	=	Lua.LuaToString(L, 3).ToString();

				if (fpvEnabled) {
					var fpvCameraIndex		=	scene.GetNodeIndex( camera );

					if (fpvCameraIndex<0) {	
						Log.Warning("Camera node {0} does not exist", camera);
					} else {
						var fpvCameraMatrix	=	Scene.FixGlobalCameraMatrix( globalTransforms[ fpvCameraIndex ] );
						var fpvViewMatrix	=	Matrix.Invert( fpvCameraMatrix );
						preTransform		=	fpvViewMatrix * Matrix.Scaling( scale );
					}
				} else {
					preTransform	=	Matrix.Scaling( scale );	
				}
			}
			return 0;
		}


		/// <summary>
		/// Gets instance of animation composer
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("get_composer")]
		int GetComposer( LuaState L )
		{
			using ( new LuaStackGuard(L,1) ) {
				
				if (composer==null) {
					composer = new AnimationComposer("", this, scene, world);
				}

				LuaObjectTranslator.Instance(L).PushObject( L, composer );
				return 1;
			}
		}


		/// <summary>
		/// Gets instance of animation composer
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("get_entity")]
		int GetEntity( LuaState L )
		{
			using ( new LuaStackGuard(L,1) ) {
				LuaObjectTranslator.Instance(L).PushObject( L, Entity );
				return 1;
			}
		}



		/// <summary>
		/// Gets instance of animation composer
		/// </summary>
		/// <param name="L"></param>
		/// <returns></returns>
		[LuaApi("get_dt")]
		int GetDTime( LuaState L )
		{
			Lua.LuaPushNumber( L, dtime );
			return 1;
		}

	}
}
