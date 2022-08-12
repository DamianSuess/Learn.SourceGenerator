using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Learn.SourceGenerator.Mvvm.Extensions
{
  internal static class IncrementalGeneratorInitializationContextExtensions
  {
    /// <summary>
    /// Registers an output node into an <see cref="IncrementalGeneratorInitializationContext"/> to output diagnostics.
    /// </summary>
    /// <param name="context">The input <see cref="IncrementalGeneratorInitializationContext"/> instance.</param>
    /// <param name="diagnostics">The input <see cref="IncrementalValuesProvider{TValues}"/> sequence of diagnostics.</param>
    public static void ReportDiagnostics(this IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<Diagnostic> diagnostics)
    {
      context.RegisterSourceOutput(diagnostics, static (context, diagnostic) => context.ReportDiagnostic(diagnostic));
    }

    /// <summary>
    /// Implements a gate for a language version over items in an input <see cref="IncrementalValuesProvider{TValues}"/> source.
    /// </summary>
    /// <typeparam name="T">The type of items in the input <see cref="IncrementalValuesProvider{TValues}"/> source.</typeparam>
    /// <param name="context">The input <see cref="IncrementalGeneratorInitializationContext"/> value being used.</param>
    /// <param name="source">The source <see cref="IncrementalValuesProvider{TValues}"/> instance.</param>
    /// <param name="languageVersion">The minimum language version to gate for.</param>
    /// <param name="diagnosticDescriptor">The <see cref="DiagnosticDescriptor"/> to emit if the gate detects invalid usage.</param>
    /// <remarks>
    /// Items in <paramref name="source"/> will be filtered out if the gate fails. If it passes, items will remain untouched.
    /// </remarks>
    public static void FilterWithLanguageVersion<T>(
        this IncrementalGeneratorInitializationContext context,
        ref IncrementalValuesProvider<T> source,
        LanguageVersion languageVersion,
        DiagnosticDescriptor diagnosticDescriptor)
    {
      // Check whether the target language version is supported
      IncrementalValueProvider<bool> isGeneratorSupported =
          context.ParseOptionsProvider
          .Select((item, _) => item is CSharpParseOptions options && options.LanguageVersion >= languageVersion);

      // Combine each data item with the supported flag
      IncrementalValuesProvider<(T Data, bool IsGeneratorSupported)> dataWithSupportedInfo =
          source
          .Combine(isGeneratorSupported);

      // Get a marker node to show whether an invalid attribute is used
      IncrementalValueProvider<bool> isUnsupportedAttributeUsed =
          dataWithSupportedInfo
          .Select(static (item, _) => item.IsGeneratorSupported)
          .Where(static item => !item)
          .Collect()
          .Select(static (item, _) => item.Length > 0);

      // Report them to the output
      context.RegisterConditionalSourceOutput(isUnsupportedAttributeUsed, context =>
      {
        context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, null));
      });

      // Only let data through if the minimum language version is supported
      source =
          dataWithSupportedInfo
          .Where(static item => item.IsGeneratorSupported)
          .Select(static (item, _) => item.Data);
    }

    /// <summary>
    /// Conditionally invokes <see cref="IncrementalGeneratorInitializationContext.RegisterSourceOutput{TSource}(IncrementalValueProvider{TSource}, Action{SourceProductionContext, TSource})"/>
    /// if the value produced by the input <see cref="IncrementalValueProvider{TValue}"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="context">The input <see cref="IncrementalGeneratorInitializationContext"/> value being used.</param>
    /// <param name="source">The source <see cref="IncrementalValueProvider{TValues}"/> instance.</param>
    /// <param name="action">The conditional <see cref="Action"/> to invoke.</param>
    public static void RegisterConditionalSourceOutput(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<bool> source,
        Action<SourceProductionContext> action)
    {
      context.RegisterSourceOutput(source, (context, condition) =>
      {
        if (condition)
        {
          action(context);
        }
      });
    }

  }
}
