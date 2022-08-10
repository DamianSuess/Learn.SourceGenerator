using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Learn.SourceGenerator.Mvvm
{
  /// <summary>
  ///   Source code generator for <c>ObservablePropertyAttribute</c>.
  /// </summary>
  /// <remarks>
  ///   REF: https://github.com/CommunityToolkit/dotnet/blob/main/CommunityToolkit.Mvvm.SourceGenerators/ComponentModel/ObservablePropertyGenerator.cs
  /// </remarks>
  public sealed partial class ObservablePropertyGenerator : IIncrementalGenerator
  {
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
      IncrementalValuesProvider<IFieldSymbol> fieldSymbols =
          context.SyntaxProvider
          .CreateSyntaxProvider(
              static (node, _) => node is FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or RecordDeclarationSyntax, AttributeLists.Count: > 0 },
              static (context, _) => ((FieldDeclarationSyntax)context.Node).Declaration.Variables.Select(v => (IFieldSymbol)context.SemanticModel.GetDeclaredSymbol(v)!))
          .SelectMany(static (item, _) => item);

    }
  }
}
