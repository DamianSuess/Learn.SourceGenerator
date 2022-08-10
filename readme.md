# Learn Source Generator in .NET

## Overview

Ever wanted to type less for the most common things you type over and over? Oh yes, me too. Probably would shave off 6-months lost time over the years.


## Geting started

### Basic Generator

1. Create 2 projects:
  * Console App: `Sample.ConsoleApp`
  * Standard Library: `Learn.SourceGenerator`
2. Add NuGet packages to the Standard Lib project, `Microsoft.CodeAnalysis.Analyzers` and `Microsoft.CodeAnalysis.CSharp`.

### Console App

1. In the console app, change the class type from `internal class` to `partial class`.
2. Next, add our method we're going to generate

```cs
namespace Sample.ConsoleApp;

// Refactor fron `internal class` to `partial class`
partial class Program
{
  static partial void HelloFrom(string name);

  static void Main(string[] args)
  {
    HelloFrom("my generated method Code");
  }
}
```

3. Reference the source generator library
4. Modify the Console App's csproj file

```xml
<!-- Add this as a new ItemGroup, replacing paths and names appropriately -->
<ItemGroup>
    <ProjectReference Include="..\PathTo\SourceGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Generator - Standard Library

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
