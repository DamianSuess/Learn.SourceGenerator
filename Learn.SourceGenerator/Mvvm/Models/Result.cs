// This file is ported and adapted from ComputeSharp (Sergio0694/ComputeSharp),
// more info in ThirdPartyNotices.txt in the root of the project.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Learn.SourceGenerator.Mvvm.Models;

/// <summary>
/// A model representing a value and an associated set of diagnostic errors.
/// </summary>
/// <typeparam name="TValue">The type of the wrapped value.</typeparam>
/// <param name="value">The wrapped value for the current result.</param>
/// <param name="errors">The associated diagnostic errors, if any.</param>
internal sealed record Result<TValue>(TValue value, ImmutableArray<Diagnostic> errors);
