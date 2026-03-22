using System.Diagnostics.CodeAnalysis;

namespace AutoLineColor.Coloring
{
    [SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
    internal static class ColorSetProvider
    {
        public static IColorSetProvider Categorised { get; } = new CategorisedProvider();
        public static IColorSetProvider Random { get; } = new RandomProvider();
        public static IColorSetProvider RandomHue { get; } = new RandomHueProvider();

        public static IColorSetProvider Named { get; } = new NamedProvider();

        private class RandomProvider : IColorSetProvider
        {
            public IColorSet GetColorSet(in TransportLine transportLine)
            {
                return KnownColorSet.Any.LoadColorSet();
            }
        }

        private class CategorisedProvider : IColorSetProvider
        {
            public IColorSet GetColorSet(in TransportLine transportLine)
            {
                // TODO: support more line types
                switch (transportLine.Info.m_transportType)
                {
                    case TransportInfo.TransportType.Bus:
                        return KnownColorSet.Pale.LoadColorSet();
                    case TransportInfo.TransportType.Metro:
                        return KnownColorSet.Bright.LoadColorSet();
                    default:
                        return KnownColorSet.Dark.LoadColorSet();
                }
            }
        }

        private class RandomHueProvider : IColorSetProvider
        {
            public IColorSet GetColorSet(in TransportLine transportLine)
            {
                // TODO: support more line types
                switch (transportLine.Info.m_transportType)
                {
                    case TransportInfo.TransportType.Bus:
                        return KnownColorSet.Blue.LoadColorSet();
                    case TransportInfo.TransportType.Metro:
                        return KnownColorSet.Green.LoadColorSet();
                    case TransportInfo.TransportType.Train:
                        return KnownColorSet.Orange.LoadColorSet();
                    default:
                        return KnownColorSet.Any.LoadColorSet();
                }
            }
        }

        private class NamedProvider : IColorSetProvider
        {
            public IColorSet GetColorSet(in TransportLine transportLine)
            {
                return KnownColorSet.Named.LoadColorSet();
            }
        }
    }
}
