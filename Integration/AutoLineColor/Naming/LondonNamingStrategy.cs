using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework;
using JetBrains.Annotations;
using Random = UnityEngine.Random;

namespace AutoLineColor.Naming
{
    internal class LondonNamingStrategy : NamingStrategyBase
    {
        private static readonly string[] Trains =
        {
            "{0}",
            "{0} Service",
            "{0} Rail",
            "{0} Railway",
            "{0} Flyer",
            "{0} Zephyr",
            "{0} Rocket",
            "{0} Arrow",
            "{0} Special",
            "Spirit of {0}",
            "Pride of {0}",
        };

        // TODO: return HashSet<string>, since this is only used to check uniqueness
        private static List<string> GetNumbers([NotNull] IEnumerable<string> names)
        {
            return names.Select(name => name.FirstWord()).ToList();
        }

        private static string TryBakerlooify(string word1, string word2)
        {
            var offset1 = Math.Min(word1.Length - 1, Math.Max(word1.Length / 2, 4));
            var offset2 = word2.Length / 4;
            var length2 = Math.Max(word2.Length / 2, 3);

            var substring2 = word2.Substring(offset2, length2);

            for (var offset = offset1; offset < word1.Length; offset++)
            {
                if (substring2.IndexOf(word1[offset]) >= 0)
                {
                    return word1.Substring(0, offset) + word2.Substring(offset2 + substring2.IndexOf(word1[offset]));
                }
            }
            return null;
        }

        /*
         * Bus line numbers are based on district:
         *
         * Given districts "Hamilton Park", "Ivy Square", "King District" in a city called "Springwood", bus line names look like:
         *
         * HP43 Local
         * 22 Hamilton Park
         * 345 Ivy to King Express
         * 9 Hamilton, Ivy and King
         * 6 Springwood Express
         */

        protected override string GetBusLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            var analysis = AnalyzeLine(transportLine);
            string prefix = null;
            int number;
            string name;
            string suffix = null;
            var existingNames = GetExistingNames(lineId);
            var existingNumbers = GetNumbers(existingNames);

            // Work out the bus number (and prefix)
            if (!analysis.HasNonDistrictStop && analysis.Districts.Count == 1)
            {
                /* District Initials */
                prefix = analysis.Districts[0].GetInitials();
                number = 0;
                string prefixedNumber;
                do
                {
                    number++;
                    prefixedNumber = $"{prefix}{number}";
                } while (existingNumbers.Contains(prefixedNumber));
            }
            else
            {
                int step;
                if (analysis.StopCount < 15)
                {
                    number = Random.Range(100, 900);
                    step = Random.Range(7, 20);
                }
                else if (analysis.StopCount < 30)
                {
                    number = Random.Range(20, 100);
                    step = Random.Range(2, 10);
                }
                else
                {
                    number = Random.Range(1, 20);
                    step = Random.Range(1, 4);
                }
                while (existingNumbers.Contains(number.ToString()))
                {
                    number += step;
                }
            }

            // Work out the bus name
            switch (analysis.Districts.Count)
            {
                case 1:
                    name = analysis.HasNonDistrictStop ? analysis.Districts[0] : "Local";
                    break;

                case 2:
                    name = $"{analysis.Districts[0].FirstWord()} to {analysis.Districts[1].FirstWord()}";
                    break;

                case 3:
                    name =
                        $"{analysis.Districts[0].FirstWord()}, {analysis.Districts[1].FirstWord()} and {analysis.Districts[2].FirstWord()}";
                    break;

                default:
                    var theSimulationManager = Singleton<SimulationManager>.instance;
                    name = theSimulationManager.m_metaData.m_CityName;
                    break;
            }

            if (analysis.StopCount <= 4)
            {
                suffix = "Express";
            }

            var lineName = $"{prefix ?? ""}{number}";
            if (!string.IsNullOrEmpty(name))
            {
                lineName += " " + name;
            }
            if (!string.IsNullOrEmpty(suffix))
            {
                lineName += " " + suffix;
            }
            return lineName;
        }

        /*
         * Metro line names are based on district, with generic names from a list thrown in.
         *
         * Given districts "Manor Park", "Ivy Square", "Hickory District", metro line names look like:
         *
         * Manor Line
         * Ivy Loop Line
         * Hickory & Ivy Line
         * Hickory, Manor & Ivy Line
         * Foxtrot Line
         *
         * There's also some attempt to "Bakerlooify" line names.  No idea how well that will work.
         */

        protected override string GetMetroLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            var analysis = AnalyzeLine(transportLine);
            string name = null;
            var districtFirstNames = analysis.Districts.Select(StringExtensions.FirstWord).ToList();
            var existingNames = GetExistingNames(lineId);
            var count = 0;

            switch (analysis.Districts.Count)
            {
                case 0:
                    // empty
                    break;

                case 1:
                    name = analysis.Districts[0];
                    break;

                case 2:
                    if (districtFirstNames[0].Equals(districtFirstNames[1]))
                    {
                        name = districtFirstNames[0];
                    }
                    else
                    {
                        name = TryBakerlooify(districtFirstNames[0], districtFirstNames[1]) ??
                               TryBakerlooify(districtFirstNames[1], districtFirstNames[0]) ??
                               $"{districtFirstNames[0]} & {districtFirstNames[1]}";
                    }
                    break;

                default:
                    var totalLength = districtFirstNames.Sum(d => d.Length);
                    if (totalLength < 20)
                    {
                        var districtFirstNamesArray = districtFirstNames.ToArray();
                        name =
                            $"{string.Join(", ", districtFirstNamesArray, 0, districtFirstNamesArray.Length - 1)} & {districtFirstNamesArray[districtFirstNamesArray.Length - 1]}";
                    }
                    break;
            }

            var lineName = name == null ? "Metro Line" : $"{name} Line";
            while (name == null || existingNames.Contains(lineName))
            {
                name = GenericNames.GetGenericName(count / 2);
                lineName = $"{name} Line";
                count++;
            }
            return lineName;
        }

        /*
         * Train line names are based on the British designations, with some liberties taken.
         *
         * The format is AXNN Name:
         *
         * A is the number of districts the train stops at.
         * X is the first letter of the last district, or X if the train stops outside of a district.
         * NN are random digits.
         *
         * The name is based on the district names.
         */

        protected override string GetTrainLineName(in TransportLine transportLine, ushort lineId = 0)
        {
            var analysis = AnalyzeLine(transportLine);
            var number = Random.Range(1, 90);
            string name;
            var districtFirstNames = analysis.Districts.Select(StringExtensions.FirstWord).ToList();
            var existingNames = GetExistingNames(lineId);
            var existingNumbers = GetNumbers(existingNames);

            var lastDistrictName = analysis.Districts.LastOrDefault();
            if (string.IsNullOrEmpty(lastDistrictName))
            {
                lastDistrictName = "Z";
            }

            var ident =
                $"{analysis.Districts.Count}{(analysis.HasNonDistrictStop ? "X" : lastDistrictName.Substring(0, 1))}";

            switch (analysis.Districts.Count)
            {
                case 0:
                    var theSimulationManager = Singleton<SimulationManager>.instance;
                    name = string.Format(Trains[Random.Range(0, Trains.Length)], theSimulationManager.m_metaData.m_CityName);
                    break;

                case 1:
                    name = string.Format(Trains[Random.Range(0, Trains.Length)], analysis.Districts[0]);
                    break;

                case 2:
                    if (districtFirstNames[0].Equals(districtFirstNames[1]))
                    {
                        name = districtFirstNames[0];
                    }
                    else if (analysis.StopCount == 2)
                    {
                        name = $"{districtFirstNames[0]} {districtFirstNames[1]} Shuttle";
                    }
                    else if (!analysis.HasNonDistrictStop)
                    {
                        name = $"{districtFirstNames[0]} {districtFirstNames[1]} Express";
                    }
                    else
                    {
                        name = $"{districtFirstNames[0]} via {districtFirstNames[1]}";
                    }
                    break;

                default:
                    var totalLength = districtFirstNames.Sum(d => d.Length);
                    if (totalLength < 15)
                    {
                        var districtFirstNamesArray = districtFirstNames.ToArray();
                        name =
                            $"{string.Join(", ", districtFirstNamesArray, 0, districtFirstNamesArray.Length - 2)} and {districtFirstNamesArray[districtFirstNamesArray.Length - 1]} via {districtFirstNamesArray[districtFirstNamesArray.Length - 2]}";
                    }
                    else
                    {
                        name = string.Format(Trains[Random.Range(0, Trains.Length)], analysis.Districts[0]);
                    }
                    break;
            }

            var lineNumber = $"{ident}{number:00}";
            while (existingNumbers.Contains(lineNumber))
            {
                number++;
                lineNumber = $"{ident}{number:00}";
            }
            return $"{lineNumber} {name}";
        }
    }
}
