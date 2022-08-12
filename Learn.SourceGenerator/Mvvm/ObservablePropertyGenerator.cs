using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Learn.SourceGenerator.Mvvm.Extensions;
using Learn.SourceGenerator.Mvvm.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Learn.SourceGenerator.Mvvm
{
  /// <summary>
  ///   Source code generator for <c>ObservablePropertyAttribute</c>.
  /// </summary>
  /// <remarks>
  ///   REF: https://github.com/CommunityToolkit/dotnet/blob/main/CommunityToolkit.Mvvm.SourceGenerators/ComponentModel/ObservablePropertyGenerator.cs
  /// </remarks>
  public partial class ObservablePropertyGenerator : IIncrementalGenerator
  {
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
      IncrementalValuesProvider<IFieldSymbol> fieldSymbols =
          context.SyntaxProvider
          .CreateSyntaxProvider(
              static (node, _) => node is FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or RecordDeclarationSyntax, AttributeLists.Count: > 0 },
              static (context, _) => ((FieldDeclarationSyntax)context.Node).Declaration.Variables.Select(v => (IFieldSymbol)context.SemanticModel.GetDeclaredSymbol(v)!))
          .SelectMany(static (item, _) => item);

      // Filter the fields using [ObservableProperty]
      IncrementalValuesProvider<IFieldSymbol> fieldSymbolsWithAttribute =
          fieldSymbols
          .Where(static item => item.HasAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute"));

      // Get diagnostics for fields using [NotifyPropertyChangedFor], [NotifyCanExecuteChangedFor], [NotifyPropertyChangedRecipients] and [NotifyDataErrorInfo], but not [ObservableProperty]
      IncrementalValuesProvider<Diagnostic> fieldSymbolsWithOrphanedDependentAttributeWithErrors =
          fieldSymbols
          .Where(static item =>
              (item.HasAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedForAttribute") ||
               item.HasAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.NotifyCanExecuteChangedForAttribute") ||
               item.HasAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedRecipientsAttribute") ||
               item.HasAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.NotifyDataErrorInfoAttribute")) &&
               !item.HasAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute"))
          .Select(static (item, _) => GetDiagnosticForFieldWithOrphanedDependentAttributes(item));

      // Output the diagnostics
      context.ReportDiagnostics(fieldSymbolsWithOrphanedDependentAttributeWithErrors);

      // Filter by language version
      context.FilterWithLanguageVersion(ref fieldSymbolsWithAttribute, LanguageVersion.CSharp8, UnsupportedCSharpLanguageVersionError);

      // Gather info for all annotated fields
      IncrementalValuesProvider<(HierarchyInfo Hierarchy, Result<PropertyInfo?> Info)> propertyInfoWithErrors =
          fieldSymbolsWithAttribute
          .Select(static (item, _) =>
          {
            HierarchyInfo hierarchy = HierarchyInfo.From(item.ContainingType);
            PropertyInfo? propertyInfo = TryGetInfo(item, out ImmutableArray<Diagnostic> diagnostics);

            return (hierarchy, new Result<PropertyInfo?>(propertyInfo, diagnostics));
          });
    }
  }
}
