using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Fusion.Engine.Graphics.Scenes;
using System.IO;

namespace Fusion.Engine.Graphics.Lights {

	partial class LightMapper {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rtc"></param>
		/// <param name="instances"></param>
		/// <returns></returns>
		RtcScene BuildRtcScene ( Rtc rtc, IEnumerable<MeshInstance> instances )
		{
			Log.Message("Generating RTC scene...");

			var sceneFlags	=	SceneFlags.Static|SceneFlags.Coherent;
			var algFlags	=	AlgorithmFlags.Intersect1;

			var scene		=	new RtcScene( rtc, sceneFlags, algFlags );

			foreach ( var instance in instances ) {
				AddMeshInstance( scene, instance );
			}

			scene.Commit();

			return scene;
		}


		/// <summary>
		/// Adds mesh instance to the RTC scene
		/// </summary>
		void AddMeshInstance ( RtcScene scene, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices     =   mesh.GetIndices();
			var vertices    =   mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.Select( v2 => new Vector4( v2.X, v2.Y, v2.Z, 0 ) )
								.ToArray();

			var id		=	scene.NewTriangleMesh( GeometryFlags.Static, indices.Length/3, vertices.Length );

			var pVerts	=	scene.MapBuffer( id, BufferType.VertexBuffer );
			var pInds	=	scene.MapBuffer( id, BufferType.IndexBuffer );

			SharpDX.Utilities.Write( pVerts, vertices, 0, vertices.Length );
			SharpDX.Utilities.Write( pInds,  indices,  0, indices.Length );

			scene.UnmapBuffer( id, BufferType.VertexBuffer );
			scene.UnmapBuffer( id, BufferType.IndexBuffer );
		}

	}
}
