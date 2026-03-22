using Random = UnityEngine.Random;

namespace AutoLineColor.Naming
{
    internal class DistrictNamingStrategy : NamingStrategyBase
    {
        // todo could this be localized?

        protected override string GetTrainLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            var analysis = AnalyzeLine(transportLine);
            var lineNumber = transportLine.m_lineNumber;
            var districts = analysis.Districts;

            switch (districts.Count)
            {
                case 1 when analysis.HasNonDistrictStop:
                    return $"#{lineNumber} {districts[0]} Shuttle";

                case 1:
                    switch (Random.value)
                    {
                        case float f when f <= .33f:
                            return $"#{lineNumber} {districts[0]} Limited";

                        case float f when f <= .66f:
                            return $"#{lineNumber} {districts[0]} Service";

                        default:
                            return $"#{lineNumber} {districts[0]} Shuttle";
                    }

                case 2:
                    switch (Random.value)
                    {
                        case float f when f <= .33f:
                            return $"#{lineNumber} {districts[0].Substring(0, 1)}&{districts[1].Substring(0, 1)}";

                        case float f when f <= .5f:
                            return $"#{lineNumber} {districts[0].Substring(0, 1)} Zephr";

                        case float f when f <= .7f:
                            return $"#{lineNumber} {districts[0].Substring(0, 1)} Flyer";

                        default:
                            return $"#{lineNumber} {districts[0]} & {districts[1]}";
                    }

                default:
                    return $"#{lineNumber} Unlimited";
            }
        }

        protected override string GetBusLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            var analysis = AnalyzeLine(transportLine);
            var lineNumber = transportLine.m_lineNumber;
            var districts = analysis.Districts;
            var stopCount = analysis.StopCount;

            switch (districts.Count)
            {
                case 1:
                    return $"#{lineNumber} {districts[0]} Local";

                case 2 when stopCount <= 4:
                    return $"#{lineNumber} {districts[0]} / {districts[1]} Express";

                case 2:
                    return $"#{lineNumber} {districts[0]} / {districts[1]} Line";

                default:
                    return $"#{lineNumber} Line";
            }
        }

        protected override string GetMetroLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            return GetBusLineName(transportLine, lineId);
        }

        protected override string GetGenericLineName(in TransportLine transportLine)
        {
            return $"#{transportLine.m_lineNumber} Line";
        }
    }
}