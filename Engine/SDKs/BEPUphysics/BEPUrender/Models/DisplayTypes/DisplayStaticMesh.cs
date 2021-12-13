using System.Collections.Generic;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Graphics.Scenes;
using BEPUphysics.BroadPhaseEntries;
using VertexPositionNormalTexture = Fusion.Engine.Graphics.DebugVertex;
using BEPUutilities.DataStructures;

namespace BEPUrender.Models
{
    /// <summary>
    /// Simple display object for triangles.
    /// </summary>
    public class DisplayStaticMesh : ModelDisplayObject<StaticMesh>
    {
        /// <summary>
        /// Creates the display object for the object.
        /// </summary>
        /// <param name="drawer">Drawer managing this display object.</param>
        /// <param name="displayedObject">Object to draw.</param>
        public DisplayStaticMesh(ModelDrawer drawer, StaticMesh displayedObject)
            : base(drawer, displayedObject)
        {
        }

        public override int GetTriangleCountEstimate()
        {
            return DisplayedObject.Mesh.Data.Indices.Length / 3;
        }

        public override void GetMeshData(RawList<VertexPositionNormalTexture> vertices, RawList<int> indices)
        {
            DisplayTriangleMesh.GetMeshData(DisplayedObject.Mesh, vertices, indices);
        }

        public override void Update()
        {
            WorldTransform = Matrix.Identity; //Transform baked into the vertices.
        }
    }
}