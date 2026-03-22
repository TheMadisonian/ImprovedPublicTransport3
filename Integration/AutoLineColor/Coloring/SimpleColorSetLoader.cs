using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal class SimpleColorSetLoader : ColorSetLoaderBase
    {
        public SimpleColorSetLoader([NotNull] string name, [NotNull] string filename, [NotNull] string defaultContent)
            : base(name, filename, defaultContent)
        {
        }

        protected override IColorSet ParseColorSet(string unparsedColors)
        {
            // split on new lines, commas and semi-colons
            var colorHexValues = unparsedColors.Split(new[] { "\n", "\r", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
            var colorList = new List<Color32>(colorHexValues.Length);
            foreach (var colorHexValue in colorHexValues)
            {
                if (TryHexToColor(colorHexValue, out var color))
                {
                    colorList.Add(color);
                }
            }
            return new ColorSet(colorList);
        }
    }
}
