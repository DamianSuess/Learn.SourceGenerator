using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Learn.SourceGenerator.Mvvm.Extensions
{
  /// <summary>Extension methods for the <see cref="ISymbol"/> type.</summary>
  /// <remarks>REF: https://github.com/CommunityToolkit/dotnet/blob/main/CommunityToolkit.Mvvm.SourceGenerators/Extensions/ISymbolExtensions.cs</remarks>
  public static class ISymbolExtensions
  {
    /// <summary>
    /// Checks whether or not a given type symbol has a specified full name.
    /// </summary>
    /// <param name="symbol">The input <see cref="ISymbol"/> instance to check.</param>
    /// <param name="name">The full name to check.</param>
    /// <returns>Whether <paramref name="symbol"/> has a full name equals to <paramref name="name"/>.</returns>
    public static bool HasFullyQualifiedName(this ISymbol symbol, string name)
    {
      return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == name;
    }

    /// <summary>
    /// Checks whether or not a given symbol has an attribute with the specified full name.
    /// </summary>
    /// <param name="symbol">The input <see cref="ISymbol"/> instance to check.</param>
    /// <param name="name">The attribute name to look for.</param>
    /// <returns>Whether or not <paramref name="symbol"/> has an attribute with the specified name.</returns>
    public static bool HasAttributeWithFullyQualifiedName(this ISymbol symbol, string name)
    {
      ImmutableArray<AttributeData> attributes = symbol.GetAttributes();

      foreach (AttributeData attribute in attributes)
      {
        if (attribute.AttributeClass?.HasFullyQualifiedName(name) == true)
        {
          return true;
        }
      }

      return false;
    }
  }
}
