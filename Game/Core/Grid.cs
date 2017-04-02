using Fusion.Core.Mathematics;

namespace IronStar.Core
{
    public class GridVertex
    {
        public Vector3 Vector { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }
        public Color Color {
            get { return GridHelper.FromIntToColor(OldValue); }
        }
        public void SetOldValue()
        {
            OldValue = NewValue;
            NewValue = 0;
        }
    }

    public class GridEdge
    {
        public VertexIndexes Start { get; set; }
        public VertexIndexes End { get; set; }
    }

    public class VertexIndexes
    {
        public VertexIndexes(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public static class GridHelper
    {
        public static Color FromIntToColor(int value)
        {
            if (value <= 10)
                return Color.Blue;
            else if (value > 10 && value <= 20)
                return Color.Green;
            else if (value > 20 && value <= 30)
                return Color.Yellow;
            else if (value > 30 && value <= 40)
                return Color.Orange;
            return Color.Red;
        }
    }
}
