using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal static class ColorDistanceMetric
    {
        public static IColorDistanceMetric RGB { get; } = new RGBMetric();
        public static IColorDistanceMetric HSV { get; } = new HSVMetric();

        private class RGBMetric : IColorDistanceMetric
        {
            public float MeasureDistance(Color32 a, Color32 b)
            {
                Vector3 v1 = (Vector4)(Color)a, v2 = (Vector4)(Color)b;
                return Vector3.Distance(v1, v2);
            }
        }

        private class HSVMetric : IColorDistanceMetric
        {
            public float MeasureDistance(Color32 a, Color32 b)
            {
                Color.RGBToHSV(a, out var h1, out var s1, out var v1);
                Color.RGBToHSV(b, out var h2, out var s2, out var v2);

                return Vector3.Distance(
                    new Vector3(h1, s1, v1),
                    new Vector3(h2, s2, v2));
            }
        }
    }
}
