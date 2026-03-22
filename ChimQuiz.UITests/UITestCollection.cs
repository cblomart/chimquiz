namespace ChimQuiz.UITests
{
    /// <summary>
    /// Regroupe tous les tests UI dans une même collection xUnit pour les exécuter
    /// séquentiellement et éviter la contention entre serveurs Kestrel / DB InMemory.
    /// </summary>
    [CollectionDefinition("UITests", DisableParallelization = true)]
#pragma warning disable CA1711 // Le suffixe Collection est requis par la convention xUnit
    public sealed class UITestCollection { }
#pragma warning restore CA1711
}
