using Fusion.Core.Mathematics;

namespace IronStar.Core
{
    public class GridVertex
    {
        public Vector3 Vector { get; set; }
        public int Value { get; set; }
        public Color Color {
            get { return Grid.FromIntToColor(Value); }
        }
    }

    public class GridEdge
    {
        public GridVertex Start { get; set; }
        public GridVertex End { get; set; }
    }

    public static class Grid
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
