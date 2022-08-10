using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis;

namespace Learn.SourceGenerator.Mvvm.Attributes
{
  /// <summary>
  ///   Attribute for generated property containing type of <see cref="ObservableObject"/>.
  ///   <para>
  ///     <code>
  ///     public class SomeViewModel
  ///     {
  ///       [ObservableProperty]
  ///       private string _isEnabled;
  ///     }
  ///     </code>
  ///   </para>
  /// </summary>
  /// <remarks>
  ///   The generated propty should be able to recognize fields using the following formats
  ///   <c>pascalCase</c> or <c>_pascalCase</c>. The resulting converstion will generate
  ///   a property which is in CamelCase.
  /// </remarks>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
  public sealed class ObservablePropertyAttribute : Attribute
  {
  }
}
