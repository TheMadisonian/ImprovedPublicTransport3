using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    public interface IColorStrategy
    {
        Color32 GetColor(in TransportLine transportLine, [NotNull] IUsedColors usedColors);
    }

    public interface IColorSet
    {
        [NotNull] IReadOnlyList<Color32> GetColors();
    }

    public interface INamedColorSet : IColorSet
    {
        [CanBeNull]
        string GetColorName(Color32 color);
    }

    public interface IColorSetLoader
    {
        [NotNull] IColorSet LoadColorSet();
    }

    public interface IColorSetProvider
    {
        [NotNull] IColorSet GetColorSet(in TransportLine transportLine);
    }

    public interface IColorSelector
    {
        Color32 SelectColor(in TransportLine transportLine,
            [NotNull] IColorSet colorSet,
            [NotNull] IUsedColors usedColors,
            [NotNull] IColorDistanceMetric metric);
    }

    public interface IUsedColors
    {
        int CountPreviousUses(Color32 color);
        float MeasureNovelty(Color32 color, [NotNull] IColorDistanceMetric metric);
    }

    public interface IColorDistanceMetric
    {
        /// <summary>
        /// Measures the absolute distance between two colors.
        /// </summary>
        /// <param name="a">One color.</param>
        /// <param name="b">The other color.</param>
        /// <returns>A number between 0f and 1f indicating how different the colors are,
        /// where 0f means they're identical and 1f means they're as different as possible.</returns>
        float MeasureDistance(Color32 a, Color32 b);
    }
}