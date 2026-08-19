// netstandard2.1 non conosce IsExternalInit, che il compilatore pretende per
// `init` e per i `record`. Dichiararlo qui costa tre righe e ci lascia scrivere
// C# moderno in una libreria che Unity puo' caricare senza adattatori.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
