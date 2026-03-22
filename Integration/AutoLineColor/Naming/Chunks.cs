using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Debug = System.Diagnostics.Debug;

namespace AutoLineColor.Naming
{
    /// <summary>
    /// Part of a line name that can be reformatted for uniqueness and length.
    /// </summary>
    /// <remarks>
    /// <para>Use <see cref="System.Object.ToString"/> to format the chunk as-is.</para>
    /// <para>Use <see cref="GetVariants"/> to reformat the chunk for uniqueness, then
    /// <see cref="GetShortenings"/> to reformat it for length.</para>
    /// <para>Variants are meaningfully different names, while shortenings are
    /// different ways to write the same name. If we need to change the name
    /// "Green Street" because it doesn't fit, we can use the shortening
    /// "Green St". But if there's already a Green Street Line, we shouldn't
    /// create a separate Green St Line; we need to use a variant, maybe a different
    /// street name.</para>
    /// </remarks>
    internal interface IChunk
    {
        IEnumerable<IChunk> GetVariants();

        IEnumerable<string> GetShortenings();
    }

    internal static class ChunkExtensions
    {
        private static IEnumerable<IChunk> GetThisAndVariants(this IChunk chunk)
        {
            return Enumerable.Repeat(chunk, 1).Concat(chunk.GetVariants());
        }

        private static IEnumerable<string> GetThisAndShortenings(this IChunk chunk)
        {
            return Enumerable.Repeat(chunk.ToString(), 1).Concat(chunk.GetShortenings());
        }

        public static string VaryAndShortenToFit(this IChunk chunk)
        {
            return VaryAndShortenToFit(chunk, null, s => s.Length <= 32) ?? chunk.ToString();
        }

        private static string VaryAndShortenToFit(
            this IChunk chunk,
            Predicate<string> variantPredicate,
            Predicate<string> shorteningPredicate)
        {
            //var logger = Console.Instance;

            foreach (var v in chunk.GetThisAndVariants())
            {
                if (variantPredicate != null)
                {
                    var vs = v.ToString();

                    //logger.Message($"VaryAndShortenToFit: check variant '{vs}'");

                    if (!variantPredicate(vs))
                        continue;
                }

                foreach (var s in v.GetThisAndShortenings())
                {
                    //logger.Message($"VaryAndShortenToFit: check shortening '{s}'");

                    if (shorteningPredicate == null || shorteningPredicate(s))
                        return s;
                }
            }

            //logger.Message("VaryAndShortenToFit: gave up");

            return null;
        }
    }

    internal abstract class CompositeChunk : IChunk
    {
        protected readonly IChunk[] Parts;

        protected CompositeChunk(IChunk[] parts)
        {
            Debug.Assert(parts != null && parts.Length > 0);
            this.Parts = parts;
        }

        public abstract override string ToString();
        public abstract IEnumerable<string> GetShortenings();
        public abstract IEnumerable<IChunk> GetVariants();

        protected static IEnumerable<string> ShortenAllPartsEachIteration(IEnumerable<IChunk> parts)
        {
            return ShortenAllPartsEachIteration(parts, string.Concat);
        }

        protected static IEnumerable<T> ShortenAllPartsEachIteration<T>(IEnumerable<IChunk> parts, Func<string[], T> combiner)
        {
            // on each iteration, try shortening every part, until all parts are exhausted:
            //   Abc Wxyz 12 -> Ab Wxy 1 -> A Wx 1 -> A W 1

            var partsArray = parts.ToArray();
            var shortenedParts = partsArray.Select(p => p.ToString()).ToArray();
            var enumerators = partsArray.Select(p => p.GetShortenings().GetEnumerator()).ToArray();

            try
            {
                while (true)
                {
                    var shortenedAnyPart = false;

                    for (var i = 0; i < enumerators.Length; i++)
                    {
                        var e = enumerators[i];

                        if (!e.MoveNext())
                            continue;

                        shortenedParts[i] = e.Current;
                        shortenedAnyPart = true;
                    }

                    if (!shortenedAnyPart)
                        break;

                    yield return combiner(shortenedParts);
                }
            }
            finally
            {
                foreach (var e in enumerators)
                    e.Dispose();
            }
        }

        protected static IEnumerable<IChunk> VaryOnePartAtATime(IEnumerable<IChunk> parts, Func<IChunk[], IChunk> combiner)
        {
            // on each iteration, try varying a different part in sequence, until all parts are exhausted:
            //   Red Foo -> Green Foo -> Green Bar -> Blue Bar -> Yellow Bar
            // TODO: try every combination

            var variedParts = parts.ToArray();
            var enumerators = Array.ConvertAll(variedParts, p => p.GetVariants().GetEnumerator());

            if (variedParts.Length == 0)
            {
                Console.Instance.Error("VaryOnePartAtATime: nothing to vary!");
                yield break;
            }

            try
            {
                var lastVaried = -1;

                while (true)
                {
                    lastVaried = (lastVaried + 1) % enumerators.Length;

                    var sequence =
                        Enumerable.Range(lastVaried, enumerators.Length - lastVaried)
                            .Concat(Enumerable.Range(0, lastVaried));

                    var variedAnyPart = false;

                    foreach (var i in sequence)
                    {
                        var e = enumerators[i];

                        if (e == null)
                            continue;

                        if (e.MoveNext())
                        {
                            variedParts[i] = e.Current;
                            variedAnyPart = true;
                            lastVaried = i;
                            break;
                        }

                        enumerators[i] = null;
                        e.Dispose();
                    }

                    if (!variedAnyPart)
                        break;

                    yield return combiner(variedParts);
                }
            }
            finally
            {
                foreach (var e in enumerators)
                {
                    e?.Dispose();
                }
            }
        }
    }

    internal class ConcatChunk : CompositeChunk
    {
        public ConcatChunk(params IChunk[] parts) : base(parts) { }

        public override string ToString()
        {
            // ReSharper disable once CoVariantArrayConversion
            return string.Concat(Parts);
        }

        public override IEnumerable<string> GetShortenings()
        {
            return ShortenAllPartsEachIteration(Parts);
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            return VaryOnePartAtATime(Parts, parts => new ConcatChunk(parts));
        }
    }

/*
    internal class AlternativesChunk : CompositeChunk
    {
        public AlternativesChunk(params IChunk[] parts) : base(parts) { }

        public override string ToString()
        {
            return parts[0].ToString();
        }

        public override IEnumerable<string> GetShortenings()
        {
            return parts[0].GetShortenings();
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            return parts.Skip(1);
        }
    }
*/

    internal enum DecayMode
    {
        RespectEndpoints,
        RespectPriority,
    }

    internal class DecayingListChunk : CompositeChunk
    {
        private readonly string _delimiter;
        private readonly DecayMode _mode;

        public DecayingListChunk(DecayMode mode, params IChunk[] parts) : this(mode, "/", parts) { }

        private DecayingListChunk(DecayMode mode, string delimiter, params IChunk[] parts) : base(parts)
        {
            this._delimiter = delimiter;
            this._mode = mode;
        }

        private string JoinWithDelimiter(IEnumerable<object> chunks)
        {
            var sb = new StringBuilder();

            foreach (var c in chunks)
            {
                if (sb.Length > 0)
                    sb.Append(_delimiter);

                sb.Append(c);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return JoinWithDelimiter(Parts);
        }

        public override IEnumerable<string> GetShortenings()
        {
            return ShortenAllPartsEachIteration(Parts, JoinWithDelimiter);
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            return GetVariantsInner().SelectMany(chunks => chunks);
        }

        private DecayingListChunk CloneWithOtherParts(IChunk[] otherParts)
        {
            return new DecayingListChunk(_mode, _delimiter, otherParts);
        }

        private IEnumerable<IEnumerable<IChunk>> GetVariantsInner()
        {
            // try varying the items first
            yield return VaryOnePartAtATime(Parts, CloneWithOtherParts);

            switch (_mode)
            {
                case DecayMode.RespectEndpoints:
                    // drop parts one at a time, leaving the middle/first/last parts until the end
                    yield return DecayPartsRespectingEndpoints(Parts)
                        .SelectMany(c =>
                            Enumerable.Repeat<IChunk>(CloneWithOtherParts(c), 1)
                                .Concat(VaryOnePartAtATime(c, CloneWithOtherParts)));
                    break;

                case DecayMode.RespectPriority:
                    // drop parts one at a time, leaving the earliest parts until the end
                    yield return DecayPartsRespectingPriority(Parts)
                        .SelectMany(c =>
                            Enumerable.Repeat<IChunk>(CloneWithOtherParts(c), 1)
                                .Concat(VaryOnePartAtATime(c, CloneWithOtherParts)));
                    break;

                default:
                    yield break;
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static IEnumerable<IChunk[]> DecayPartsRespectingEndpoints(IChunk[] parts)
        {
            var logger = Console.Instance;

            if (parts.Length == 0)
            {
                logger.Error("no parts to decay!");
                yield break;
            }

            var temp = parts.ToArray(); // clone
            var remaining = temp.Length;
            var mid = temp.Length / 2;
            var last = temp.Length - 1;
            int dropEarly = 1, dropLate = mid + 1;
            var early = true;

            //logger.Message($"decaying (endpoints): {remaining} items to start");

            while (true)
            {
                if (remaining <= 1)
                {
                    yield return temp.Where(c => c != null).ToArray();
                    yield break;
                }

                var canDropEarly = dropEarly > 0 && dropEarly < mid;
                var canDropLate = dropLate > mid && dropLate < last;

                if (canDropEarly && (early || !canDropLate))
                {
                    //logger.Message($"early: dropping item {dropEarly}");
                    temp[dropEarly] = null;
                    dropEarly++;
                    early = false;
                }
                else if (canDropLate)
                {
                    //logger.Message($"late: dropping item {dropLate}");
                    temp[dropLate] = null;
                    dropLate++;
                    early = true;
                }
                else if (mid != 0 && mid != last && temp[mid] != null)
                {
                    //logger.Message($"mid: dropping item {mid}");
                    temp[mid] = null;
                }
                else if (temp[0] != null)
                {
                    //logger.Message("first: dropping item 0");
                    temp[0] = null;
                }
                else
                {
                    // shouldn't get here
                    logger.Error("unexpected case!");
                    Trace.Fail("dropped parts too fast");
                    yield break;
                }

                remaining--;

                yield return temp.Where(c => c != null).ToArray();
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static IEnumerable<IChunk[]> DecayPartsRespectingPriority(IChunk[] parts)
        {
            var logger = Console.Instance;

            if (parts.Length == 0)
            {
                logger.Error("no parts to decay!");
                yield break;
            }

            var temp = parts.ToArray(); // clone

            var remaining = temp.Length;

            //logger.Message($"decaying (priority): {remaining} items to start");

            while (true)
            {
                remaining--;

                if (remaining <= 1)
                {
                    yield return new[] { temp[0] };
                    yield break;
                }

                //logger.Message($"decaying: returning first {remaining} items");

                var result = new IChunk[remaining];
                Array.Copy(temp, result, remaining);
                yield return result;
            }
        }
    }

    internal sealed class OptionalCosmeticChunk : IChunk
    {
        public static readonly OptionalCosmeticChunk Line = new OptionalCosmeticChunk(new StaticChunk(" Line"));

        private readonly IChunk _part;

        private OptionalCosmeticChunk(IChunk part)
        {
            Debug.Assert(part != null);
            this._part = part;
        }

        public override string ToString()
        {
            return _part.ToString();
        }

        public IEnumerable<string> GetShortenings()
        {
            yield return "";
        }

        public IEnumerable<IChunk> GetVariants()
        {
            yield break;
        }
    }

    internal abstract class StringChunk : IChunk
    {
        protected readonly string Text;

        protected StringChunk(string text)
        {
            this.Text = text;
        }

        public override string ToString()
        {
            return Text;
        }

        public abstract IEnumerable<string> GetShortenings();

        public abstract IEnumerable<IChunk> GetVariants();
    }

    internal sealed class StaticChunk : StringChunk
    {
        public static readonly StaticChunk Via = new StaticChunk(" via ");

        public StaticChunk(string text) : base(text) { }

        public override IEnumerable<string> GetShortenings()
        {
            yield break;
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            yield break;
        }
    }

    internal enum AbbreviationMode
    {
        Original,
        AbbreviateSuffix,
        StripSuffix,
        AutoShortenWords,
    }

    internal abstract class NameChunkBase : StringChunk
    {
        private readonly AbbreviationMode _mode;

        protected NameChunkBase(string text, AbbreviationMode mode = AbbreviationMode.Original) : base(text)
        {
            this._mode = mode;
        }

        public override string ToString()
        {
            switch (_mode)
            {
                case AbbreviationMode.AbbreviateSuffix:
                    return AbbreviateSuffix();

                case AbbreviationMode.StripSuffix:
                    return StripSuffix();

                case AbbreviationMode.AutoShortenWords:
                    return StripSuffixAndShortenWords();

                case AbbreviationMode.Original:
                    return Text;

                default:
                    goto case AbbreviationMode.Original;
            }
        }

        protected abstract string StripSuffixAndShortenWords();
        protected abstract string StripSuffix();
        protected abstract string AbbreviateSuffix();

        public override IEnumerable<string> GetShortenings()
        {
            if (_mode < AbbreviationMode.AbbreviateSuffix)
                yield return AbbreviateSuffix();

            if (_mode < AbbreviationMode.StripSuffix)
                yield return StripSuffix();
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            yield break;
        }
    }

    internal class DistrictNameChunk : NameChunkBase
    {
        public DistrictNameChunk(string text, AbbreviationMode mode = AbbreviationMode.Original) : base(text, mode)
        {
        }

        protected override string StripSuffixAndShortenWords()
        {
            return Text.StripDistrictSuffix().AutoShortenWords();
        }

        protected override string StripSuffix()
        {
            return Text.StripDistrictSuffix();
        }

        protected override string AbbreviateSuffix()
        {
            return Text.AbbreviateDistrictSuffix();
        }
    }

    internal class RoadNameChunk : NameChunkBase
    {
        public RoadNameChunk(string text, AbbreviationMode mode = AbbreviationMode.Original) : base(text, mode)
        {
        }

        protected override string StripSuffixAndShortenWords()
        {
            return Text.StripRoadSuffix().AutoShortenWords();
        }

        protected override string StripSuffix()
        {
            return Text.StripRoadSuffix();
        }

        protected override string AbbreviateSuffix()
        {
            return Text.AbbreviateRoadSuffix();
        }
    }
}
