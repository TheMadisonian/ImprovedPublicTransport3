using JetBrains.Annotations;

namespace AutoLineColor.Naming
{
    public interface INamingStrategy
    {
        [CanBeNull]
        string GetName(in TransportLine transportLine, ushort lineId = 0);
    }
}