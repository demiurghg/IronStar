using System.Collections.Generic;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;

namespace IronStar.Core
{
    public partial class GameWorld
    {

        void DrawDebugGrid()
        {
            if (Game.Keyboard.IsKeyDown(Fusion.Engine.Input.Keys.N))
                SetGridValue(new Vector3(7, 2, 5), 1000);

            if (Game.Keyboard.IsKeyDown(Fusion.Engine.Input.Keys.K))
                GridUpdate();

            if (Game.Keyboard.IsKeyDown(GridKey) && debugGridOff)
            {
                debugGridOn = false;
                Log.Warning($"Grid status was changed {debugGridOn}");
            }
            if (Game.Keyboard.IsKeyDown(GridKey) && !debugGridOff)
            {
                debugGridOn = true;
                Log.Warning($"Grid status was changed {debugGridOn}");
            }

            if (Game.Keyboard.IsKeyUp(GridKey))
            {
                if (debugGridOn)
                {
                    for (var index0 = 0; index0 < large; index0++)
                    {
                        for (var index1 = 0; index1 < height; index1++)
                        {
                            for (var index2 = 0; index2 < width; index2++)
                            {
                                var vertex = gridArray[index0, index1, index2];
                                Game.RenderSystem.RenderWorld.Debug.DrawPoint(vertex.Vector, 1, vertex.Color, 5);
                            }
                        }
                    }

                    foreach (var edge in edges)
                    {
                        var startPoint = gridArray[edge.Start.X, edge.Start.Y, edge.Start.Z];
                        var endPoint = gridArray[edge.End.X, edge.End.Y, edge.End.Z];
                        Game.RenderSystem.RenderWorld.Debug.DrawLine(startPoint.Vector, endPoint.Vector, startPoint.Color, endPoint.Color, 5, 5);
                    }

                    debugGridOff = true;
                }
                else debugGridOff = false;
            }
        }

        void GridGenerate()
        {
            //large, height, width
            vertices = new List<GridVertex>();
            var coordinateX = offsetX;
            var coordinateY = offsetY;
            var coordinateZ = offsetZ;
            for (var index0 = 0; index0 < large; index0++)
            {
                coordinateY = offsetY;
                for (var index1 = 0; index1 < height; index1++)
                {
                    coordinateX = offsetX;
                    for (var index2 = 0; index2 < width; index2++)
                    {
                        gridArray[index0, index1, index2]
                            = new GridVertex() { Vector = new Vector3(coordinateX, coordinateY, coordinateZ), OldValue = 0 };
                        coordinateX += gridStep;
                    }
                    coordinateY += gridStep;
                }
                coordinateZ += gridStep;
            }

            GenerateGritEdges();
        }

        void GenerateGritEdges()
        {
            //large, height, width
            edges = new List<GridEdge>();
            for (var index0 = 0; index0 < large; index0++)
            {
                for (var index1 = 0; index1 < height; index1++)
                {
                    for (var index2 = 0; index2 < width; index2++)
                    {
                        Vector3 normal;
                        var startPoint = gridArray[index0, index1, index2];
                        if (index0 + 1 < large && !GridEdgeRayCast(startPoint.Vector, gridArray[index0 + 1, index1, index2].Vector, out normal))
                        {
                            edges.Add(new GridEdge() { Start = new VertexIndexes(index0, index1, index2), End = new VertexIndexes(index0 + 1, index1, index2) });
                        }
                        if (index1 + 1 < height && !GridEdgeRayCast(startPoint.Vector, gridArray[index0, index1 + 1, index2].Vector, out normal))
                        {
                            edges.Add(new GridEdge() { Start = new VertexIndexes(index0, index1, index2), End = new VertexIndexes(index0, index1 + 1, index2) });
                        }
                        if (index2 + 1 < width && !GridEdgeRayCast(startPoint.Vector, gridArray[index0, index1, index2 + 1].Vector, out normal))
                        {
                            edges.Add(new GridEdge() { Start = new VertexIndexes(index0, index1, index2), End = new VertexIndexes(index0, index1, index2 + 1) });
                        }
                    }
                }
            }
        }

        bool GridEdgeRayCast(Vector3 from, Vector3 to, out Vector3 normal)
        {
            var raycast0 = RayCastAgainstStatic(from, to, out normal);
            var raycast1 = RayCastAgainstStatic(to, from, out normal);
            return raycast0 || raycast1;
        }

        int iterations = 10;
        void SetGridValue(Vector3 point, int value)
        {
            gridArray[(int)point.X, (int)point.Y, (int)point.Z].OldValue = value;
            iterations = 20;
        }

        void GridUpdate()
        {
            Log.Warning($"Grid update");
            if (iterations > 0)
            {
                Log.Warning($"SoundPropogationIterate {iterations}");
                SoundPropogationIterate();
                SetSoundPropogation();
                GenerateGritEdges();
                iterations--;
            }
            else
            {
                SetSoundPropogation();
                GenerateGritEdges();
            }
        }

        int attenuation = 12;
        void SoundPropogationIterate()
        {
            for (var index0 = 0; index0 < large; index0++)
            {
                for (var index1 = 0; index1 < height; index1++)
                {
                    for (var index2 = 0; index2 < width; index2++)
                    {
                        var vertex = gridArray[index0, index1, index2];
                        if (index0 + 1 < large)
                            vertex.NewValue += gridArray[index0 + 1, index1, index2].OldValue / attenuation;
                        if (index1 + 1 < height)
                            vertex.NewValue += gridArray[index0, index1 + 1, index2].OldValue / attenuation;
                        if (index2 + 1 < width)
                            vertex.NewValue += gridArray[index0, index1, index2 + 1].OldValue / attenuation;
                        if (index0 - 1 > 0)
                            vertex.NewValue += gridArray[index0 - 1, index1, index2].OldValue / attenuation;
                        if (index1 - 1 > 0)
                            vertex.NewValue += gridArray[index0, index1 - 1, index2].OldValue / attenuation;
                        if (index2 - 1 > 0)
                            vertex.NewValue += gridArray[index0, index1, index2 - 1].OldValue / attenuation;
                        vertex.NewValue += vertex.OldValue / 2;
                    }
                }
            }
        }

        void SetSoundPropogation()
        {
            for (var index0 = 0; index0 < large; index0++)
            {
                for (var index1 = 0; index1 < height; index1++)
                {
                    for (var index2 = 0; index2 < width; index2++)
                    {
                        gridArray[index0, index1, index2].SetOldValue();
                    }
                }
            }
        }
    }
}
