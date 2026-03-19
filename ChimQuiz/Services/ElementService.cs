using ChimQuiz.Models;

namespace ChimQuiz.Services;

public class ElementService
{
    private readonly IReadOnlyList<Element> _elements;
    private readonly Random _random = new();

    public ElementService()
    {
        _elements = ElementData.AllElements;
    }

    public IReadOnlyList<Element> GetAll() => _elements;

    public Element? GetById(int atomicNumber)
        => _elements.FirstOrDefault(e => e.AtomicNumber == atomicNumber);

    public Element? GetBySymbol(string symbol)
        => _elements.FirstOrDefault(e => string.Equals(e.Symbol, symbol, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns count confusable symbols:
    /// priority to same first letter or same length as the reference symbol.
    /// </summary>
    public List<string> GetConfusableSymbols(int count, string excludeSymbol, int maxAtomicNumber = 118)
    {
        var pool = _elements
            .Where(e => e.AtomicNumber <= maxAtomicNumber &&
                        !string.Equals(e.Symbol, excludeSymbol, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Prioritise: same first letter OR same length
        char firstChar    = char.ToUpperInvariant(excludeSymbol[0]);
        int  refLen       = excludeSymbol.Length;

        var confusable = pool
            .Where(e => char.ToUpperInvariant(e.Symbol[0]) == firstChar || e.Symbol.Length == refLen)
            .ToList();

        var result = new List<Element>();

        // Take as many confusable as possible, fill remainder with random
        PickInto(confusable, Math.Min(count, confusable.Count), result);
        if (result.Count < count)
        {
            var remaining = pool.Except(result).ToList();
            PickInto(remaining, count - result.Count, result);
        }

        return result.Take(count).Select(e => e.Symbol).ToList();
    }

    /// <summary>
    /// Returns count confusable names:
    /// priority to same first letter or similar length (±3 chars) as the reference name.
    /// </summary>
    public List<string> GetConfusableNames(int count, string excludeName, int maxAtomicNumber = 118)
    {
        var pool = _elements
            .Where(e => e.AtomicNumber <= maxAtomicNumber &&
                        !string.Equals(e.Name, excludeName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        char firstChar = char.ToUpperInvariant(excludeName[0]);
        int  refLen    = excludeName.Length;

        var confusable = pool
            .Where(e => char.ToUpperInvariant(e.Name[0]) == firstChar ||
                        Math.Abs(e.Name.Length - refLen) <= 3)
            .ToList();

        var result = new List<Element>();

        PickInto(confusable, Math.Min(count, confusable.Count), result);
        if (result.Count < count)
        {
            var remaining = pool.Except(result).ToList();
            PickInto(remaining, count - result.Count, result);
        }

        return result.Take(count).Select(e => e.Name).ToList();
    }

    // Legacy: kept for backward compat — now delegates to confusable versions
    public List<string> GetRandomSymbols(int count, string excludeSymbol)
        => GetConfusableSymbols(count, excludeSymbol);

    public List<string> GetRandomNames(int count, string excludeName)
        => GetConfusableNames(count, excludeName);

    /// <summary>
    /// Pick a random element within [1..maxAtomicNumber].
    /// Elements in the lower half of the pool are 2x more likely.
    /// </summary>
    public Element GetWeightedRandom(IEnumerable<int> excludeAtomicNumbers, int maxAtomicNumber = 118)
    {
        var excludeSet = new HashSet<int>(excludeAtomicNumbers);
        int midpoint   = maxAtomicNumber / 2;

        var lowPool  = _elements.Where(e => e.AtomicNumber <= midpoint      && e.AtomicNumber <= maxAtomicNumber && !excludeSet.Contains(e.AtomicNumber)).ToList();
        var highPool = _elements.Where(e => e.AtomicNumber >  midpoint      && e.AtomicNumber <= maxAtomicNumber && !excludeSet.Contains(e.AtomicNumber)).ToList();

        int totalWeight = lowPool.Count * 2 + highPool.Count;
        if (totalWeight == 0)
        {
            var all = _elements.Where(e => e.AtomicNumber <= maxAtomicNumber && !excludeSet.Contains(e.AtomicNumber)).ToList();
            return all[_random.Next(all.Count)];
        }

        int pick = _random.Next(totalWeight);
        if (pick < lowPool.Count * 2)
            return lowPool[pick / 2];

        return highPool[pick - lowPool.Count * 2];
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void PickInto(List<Element> source, int count, List<Element> dest)
    {
        count = Math.Min(count, source.Count);
        for (int i = 0; i < count; i++)
        {
            int j = _random.Next(i, source.Count);
            (source[i], source[j]) = (source[j], source[i]);
        }
        dest.AddRange(source.Take(count));
    }
}
