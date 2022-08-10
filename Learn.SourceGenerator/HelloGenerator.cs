using Microsoft.CodeAnalysis;

namespace Learn.SourceGenerator
{
  /// <summary>
  ///   Simple generator
  ///   Ref: https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
  /// </summary>
  [Generator]
  public class HelloGenerator : ISourceGenerator
  {
    public void Execute(GeneratorExecutionContext context)
    {
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
