using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal class UsedColors : IUsedColors
    {
        private readonly Dictionary<Color32, int> _dict;

        public static IUsedColors FromLines(TransportLine[] lines)
        {
            var result = new UsedColors(lines.Length);
            var dict = result._dict;

            foreach (var l in lines)
            {
                if (!l.IsActive())
                    continue;

                var color = l.m_color;
                if (dict.TryGetValue(color, out var count))
                {
                    dict[color] = count + 1;
                }
                else
                {
                    dict.Add(color, 1);
                }
            }

            return result;
        }

        private UsedColors(int capacity)
        {
            _dict = new Dictionary<Color32, int>(capacity);
        }

        public int CountPreviousUses(Color32 color)
        {
            return _dict.TryGetValue(color, out var count) ? count : 0;
        }

        public float MeasureNovelty(Color32 color, IColorDistanceMetric metric)
        {
            return _dict.Keys.Min(usedColor => metric.MeasureDistance(color, usedColor));
        }
    }

    internal sealed class NullUsedColors : IUsedColors
    {
        public int CountPreviousUses(Color32 color)
        {
            return 0;
        }

        public float MeasureNovelty(Color32 color, IColorDistanceMetric metric)
        {
            return 1f;
        }
    }
}
