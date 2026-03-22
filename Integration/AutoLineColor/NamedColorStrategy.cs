using AutoLineColor.Coloring;
using UnityEngine;

namespace AutoLineColor
{
    public class NamedColorStrategy : ColorStrategyBase, ICombinedStrategy
    {
        protected override IColorSetProvider GetColorSetProvider()
        {
            return ColorSetProvider.Named;
        }

        protected override IColorSelector GetColorSelector()
        {
            return ColorSelector.DifferenceThreshold;
        }

        public string GetName(in TransportLine transportLine, ushort lineId = 0)
        {
            // can't do anything unless we're assigning a color at the same time
            return null;
        }

        public void GetColorAndName(in TransportLine line, IUsedColors usedColors, out Color32 color, out string name)
        {
            color = GetColor(line, usedColors);

            var colorSet = GetColorSetProvider().GetColorSet(line);
            var colorName = (colorSet as INamedColorSet)?.GetColorName(color);

            name = colorName != null ? $"{colorName} Line" : null;
        }
    }
}
