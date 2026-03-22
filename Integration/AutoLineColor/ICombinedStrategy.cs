using AutoLineColor.Coloring;
using AutoLineColor.Naming;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor
{
    /// <summary>
    /// Assigns a color and name at the same time.
    /// </summary>
    public interface ICombinedStrategy : IColorStrategy, INamingStrategy
    {
        void GetColorAndName(in TransportLine line, [NotNull] IUsedColors usedColors, out Color32 color, [CanBeNull] out string name);
    }
}
