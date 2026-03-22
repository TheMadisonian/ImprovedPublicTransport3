using UnityEngine;

namespace AutoLineColor.Coloring
{
    public abstract class ColorStrategyBase : IColorStrategy
    {
        protected abstract IColorSetProvider GetColorSetProvider();
        protected abstract IColorSelector GetColorSelector();

        private static IColorDistanceMetric GetColorDistanceMetric()
        {
            // TODO: option to use HSV
            return ColorDistanceMetric.RGB;
        }

        public Color32 GetColor(in TransportLine transportLine, IUsedColors usedColors)
        {
            var provider = GetColorSetProvider();
            var selector = GetColorSelector();
            var metric = GetColorDistanceMetric();

            var colorSet = provider.GetColorSet(transportLine);
            var color = selector.SelectColor(transportLine, colorSet, usedColors, metric);
            return color;
        }
    }
}
