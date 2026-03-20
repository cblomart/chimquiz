using ChimQuiz.Services;

namespace ChimQuiz.Tests.Services
{
    public class ElementServiceTests
    {
        private readonly ElementService _svc = new();

        // ── GetAll ────────────────────────────────────────────────────────────────

        [Fact]
        public void GetAll_Returns118Elements()
        {
            Assert.Equal(118, _svc.GetAll().Count);
        }

        // ── GetById ───────────────────────────────────────────────────────────────

        [Fact]
        public void GetById_Hydrogen_ReturnsCorrectElement()
        {
            Models.Element? el = _svc.GetById(1);
            Assert.NotNull(el);
            Assert.Equal("H", el.Symbol);
            Assert.Equal("Hydrogène", el.Name);
        }

        [Fact]
        public void GetById_Carbon_ReturnsCorrectElement()
        {
            Models.Element? el = _svc.GetById(6);
            Assert.NotNull(el);
            Assert.Equal("C", el.Symbol);
            Assert.Equal("Carbone", el.Name);
        }

        [Fact]
        public void GetById_OutOfRange_ReturnsNull()
        {
            Assert.Null(_svc.GetById(0));
            Assert.Null(_svc.GetById(119));
        }

        // ── GetBySymbol ───────────────────────────────────────────────────────────

        [Fact]
        public void GetBySymbol_CaseInsensitive_ReturnsElement()
        {
            Assert.NotNull(_svc.GetBySymbol("he"));
            Assert.NotNull(_svc.GetBySymbol("HE"));
            Assert.NotNull(_svc.GetBySymbol("He"));
        }

        [Fact]
        public void GetBySymbol_Unknown_ReturnsNull()
        {
            Assert.Null(_svc.GetBySymbol("Xx"));
        }

        // ── GetConfusableSymbols ──────────────────────────────────────────────────

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        public void GetConfusableSymbols_ReturnsRequestedCount(int count)
        {
            List<string> symbols = _svc.GetConfusableSymbols(count, "H");
            Assert.Equal(count, symbols.Count);
        }

        [Fact]
        public void GetConfusableSymbols_ExcludesReferenceSymbol()
        {
            List<string> symbols = _svc.GetConfusableSymbols(10, "He");
            Assert.DoesNotContain("He", symbols, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetConfusableSymbols_RespectsMaxAtomicNumber()
        {
            List<string> symbols = _svc.GetConfusableSymbols(5, "H", maxAtomicNumber: 10);
            foreach (string sym in symbols)
            {
                Models.Element? el = _svc.GetBySymbol(sym);
                Assert.NotNull(el);
                Assert.True(el.AtomicNumber <= 10, $"Symbol {sym} (Z={el.AtomicNumber}) exceeds maxAtomicNumber=10");
            }
        }

        // ── GetConfusableNames ────────────────────────────────────────────────────

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        public void GetConfusableNames_ReturnsRequestedCount(int count)
        {
            List<string> names = _svc.GetConfusableNames(count, "Carbone");
            Assert.Equal(count, names.Count);
        }

        [Fact]
        public void GetConfusableNames_ExcludesReferenceName()
        {
            List<string> names = _svc.GetConfusableNames(10, "Hélium");
            Assert.DoesNotContain("Hélium", names, StringComparer.OrdinalIgnoreCase);
        }

        // ── GetWeightedRandom ─────────────────────────────────────────────────────

        [Fact]
        public void GetWeightedRandom_ReturnsElementWithinMaxAtomicNumber()
        {
            HashSet<int> excluded = new HashSet<int>();
            for (int i = 0; i < 50; i++)
            {
                Models.Element el = _svc.GetWeightedRandom(excluded, maxAtomicNumber: 20);
                Assert.True(el.AtomicNumber <= 20, $"Z={el.AtomicNumber} exceeds maxAtomicNumber=20");
            }
        }

        [Fact]
        public void GetWeightedRandom_ExcludesSpecifiedElements()
        {
            HashSet<int> excluded = Enumerable.Range(1, 117).ToHashSet();  // exclude all but Z=118
            Models.Element el = _svc.GetWeightedRandom(excluded, maxAtomicNumber: 118);
            Assert.Equal(118, el.AtomicNumber);
        }

        [Fact]
        public void GetWeightedRandom_NeverReturnsExcluded()
        {
            HashSet<int> excluded = new HashSet<int> { 1, 2, 3 };
            for (int i = 0; i < 30; i++)
            {
                Models.Element el = _svc.GetWeightedRandom(excluded, maxAtomicNumber: 20);
                Assert.DoesNotContain(el.AtomicNumber, excluded);
            }
        }
    }
}
