using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal class NamedColorSetLoader : ColorSetLoaderBase
    {
        public NamedColorSetLoader([NotNull] string name, [NotNull] string filename, [NotNull] string defaultContent)
            : base(name, filename, defaultContent)
        {
        }

        private static readonly Regex NamedColorLineRegex = new Regex(@"^\s*(?<color>\S+)(?:\s*(?<name>.+))?$");

        protected override IColorSet ParseColorSet(string unparsedColors)
        {
            // split on new lines, commas and semi-colons
            var colorHexValues = unparsedColors.Split(new[] { "\n", "\r", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
            var colorDict = new Dictionary<Color32, string>(colorHexValues.Length);
            foreach (var colorHexValue in colorHexValues)
            {
                var match = NamedColorLineRegex.Match(colorHexValue);

                if (match.Success && TryHexToColor(match.Groups["color"].Value, out var color))
                {
                    colorDict[color] = match.Groups["name"].Value;
                }
                else if (TryHexToColor(colorHexValue, out color))
                {
                    colorDict.Add(color, match.Groups["color"].Value);
                }
            }

            return new NamedColorSet(colorDict);
        }
    }
}
