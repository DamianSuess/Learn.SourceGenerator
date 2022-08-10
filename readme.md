# Learn Source Generator in .NET

## Overview

Ever wanted to type less for the most common things you type over and over? Oh yes, me too. Probably would shave off 6-months lost time over the years.


## Geting started

1. Add NuGet packages, `Microsoft.CodeAnalysis.Analyzers` and `Microsoft.CodeAnalysis.CSharp`.

## Sample

```cs
using Microsoft.CodeAnalysis;

namespace Learn.SourceGenerator
{
  [Generator]
  public class HelloGenerator : ISourceGenerator
  {
    public void Execute(GeneratorExecutionContext context)
    {
      // Code generation goes here

      // Find the main method
      var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

      // Build up the source code
      string source = $@" // Auto-generated code
using System;

namespace {mainMethod.ContainingNamespace.ToDisplayString()}
{{
  public static partial class {mainMethod.ContainingType.Name}
  {{
    static partial void HelloFrom(string name) =>
      Console.WriteLine($""Generator says: Hi from '{{name}}'"");
  }}
}}
";
      var typeName = mainMethod.ContainingType.Name;

      // Add the source code to the compilation
      context.AddSource($"{typeName}.g.cs", source);
    }

    public void Initialize(GeneratorInitializationContext context)
    {
      // No initialization required for this one
    }
  }
}
```

## References

* [https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview]
* [https://github.com/amis92/csharp-source-generators]
