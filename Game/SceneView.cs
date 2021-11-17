using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Core;

namespace IronStar
{
	/// <summary>
	/// Represent particular scene representation, like physical, rendering etc.
	/// </summary>
	/// <typeparam name="TMesh"></typeparam>
	public class SceneView<TMesh> where TMesh: ITransformable
	{
		public readonly Scene		scene;
		public readonly Matrix[]	transforms;
		public readonly Matrix[]	skinning;
		public readonly TMesh[]		meshes;


		public SceneView( Scene scene, Func<Node,Mesh,TMesh> meshSelector, Func<Node,bool> nodeFilter )
		{
			this.scene	=	scene;
			transforms	=	new Matrix[ scene.Nodes.Count ];
			skinning	=	new Matrix[ scene.Nodes.Count ];
			meshes		=	new TMesh[ scene.Nodes.Count ];

			scene.ComputeAbsoluteTransforms( transforms );
			scene.ComputeBoneTransforms( transforms, skinning );

			for ( int i=0; i<scene.Nodes.Count; i++ ) 
			{
				if (nodeFilter(scene.Nodes[i]) && scene.Nodes[i].MeshIndex>=0)
				{
					var node		=	scene.Nodes[i];
					var meshIndex	=	node.MeshIndex;
					meshes[i]		=	meshSelector( node, scene.Meshes[ meshIndex ] );
				}
				else
				{
					meshes[i]		=	default(TMesh);
				}
			}
		}

		public SceneView( Scene scene, Func<Mesh,TMesh> meshSelector, Func<Node,bool> nodeFilter )
		:this( scene, (node,mesh) => meshSelector(mesh), nodeFilter )
		{
			
		}

		public Matrix GetAbsoluteTransform(int nodeIndex)
		{
			return transforms[nodeIndex];
		}


		/*public void ApplyTransform( Action<TMesh,Matrix> transformAction, Matrix worldMatrix )
		{
			for ( int i=0; i<meshes.Length; i++ )
			{
				if (meshes[i]!=null)
				{
					transformAction( meshes[i], transforms[i] * worldMatrix );
				}
			}
		}	 */


		public void SetTransforms( Matrix world, Matrix[] bones, bool flatten = true )
		{
			if (bones!=null)
			{
				if (bones.Length<transforms.Length) throw new ArgumentOutOfRangeException(nameof(bones) + " has less element than requred");

				if (flatten)
				{
					Array.Copy( bones, transforms, transforms.Length );
				}
				else
				{
					scene.ComputeAbsoluteTransforms( bones, transforms );
				}

				scene.ComputeBoneTransforms( transforms, skinning );
			}

			for ( int i=0; i<meshes.Length; i++ )
			{
				if (meshes[i]!=null)
				{
					meshes[i].World = transforms[i] * world;

					var dstBones = (meshes[i] as ISkinnable)?.Bones;

					if (dstBones!=null)
					{
						int count = Math.Min( dstBones.Length, skinning.Length );
						Array.Copy( skinning, dstBones, count );
					}
				}
			}
		}


		public void ForEachMesh( Action<TMesh> action )
		{
			foreach ( var mesh in meshes )
			{
				if (mesh!=null)
				{
					action( mesh );
				}
			}
		}

		public void ForEachMesh( Action<int,TMesh> action )
		{
			for (int idx=0; idx<meshes.Length; idx++)
			{
				var mesh = meshes[idx];

				if (mesh!=null)
				{
					action( idx, mesh );
				}
			}
		}
	}
}
