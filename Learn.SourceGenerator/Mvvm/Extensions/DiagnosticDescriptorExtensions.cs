using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Learn.SourceGenerator.Mvvm.Extensions
{
  public static class DiagnosticDescriptorExtensions
  {
    /// <summary>
    /// Creates a new <see cref="Diagnostic"/> instance with the specified parameters.
    /// </summary>
    /// <param name="descriptor">The input <see cref="DiagnosticDescriptor"/> for the diagnostics to create.</param>
    /// <param name="symbol">The source <see cref="ISymbol"/> to attach the diagnostics to.</param>
    /// <param name="args">The optional arguments for the formatted message to include.</param>
    /// <returns>The resulting <see cref="Diagnostic"/> instance.</returns>
    public static Diagnostic CreateDiagnostic(this DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
    {
      return Diagnostic.Create(descriptor, symbol.Locations.FirstOrDefault(), args);
    }
  }
}
