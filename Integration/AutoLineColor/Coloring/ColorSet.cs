using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutoLineColor.Util;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal sealed class ColorSet : IColorSet
    {
        private readonly IReadOnlyList<Color32> _colors;

        public ColorSet(IList<Color32> colors)
        {
            _colors = new ReadOnlyList<Color32>(colors);
        }

        public IReadOnlyList<Color32> GetColors()
        {
            return _colors;
        }
    }

    internal sealed class NamedColorSet : INamedColorSet
    {
        private readonly IReadOnlyDictionary<Color32, string> _colorsAndNames;

        public NamedColorSet(IDictionary<Color32, string> colorsAndNames)
        {
            _colorsAndNames = new ReadOnlyDictionary<Color32, string>(colorsAndNames);
        }

        public IReadOnlyList<Color32> GetColors()
        {
            return new ReadOnlyList<Color32>(_colorsAndNames.Keys);
        }

        public string GetColorName(Color32 color)
        {
            return _colorsAndNames.TryGetValue(color, out var name) ? name : null;
        }
    }
}
