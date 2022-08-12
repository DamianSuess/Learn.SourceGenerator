using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Learn.SourceGenerator.Mvvm.Diagnostics
{
  internal static class DiagnosticDescriptors
  {
    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> indicating when <c>[ObservableProperty]</c> is applied to a field in an invalid type.
    /// <para>
    /// Format: <c>"The field {0}.{1} needs to be annotated with [ObservableProperty] in order to enable using [NotifyPropertyChangedFor], [NotifyCanExecuteChangedFor], [NotifyPropertyChangedRecipients] and [NotifyDataErrorInfo]"</c>.
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor FieldWithOrphanedDependentObservablePropertyAttributesError = new DiagnosticDescriptor(
        id: "MVVMTK0020",
        title: "Invalid use of attributes dependent on [ObservableProperty]",
        messageFormat: "The field {0}.{1} needs to be annotated with [ObservableProperty] in order to enable using [NotifyPropertyChangedFor], [NotifyCanExecuteChangedFor], [NotifyPropertyChangedRecipients] and [NotifyDataErrorInfo]",
        category: typeof(ObservablePropertyGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Fields not annotated with [ObservableProperty] cannot use [NotifyPropertyChangedFor], [NotifyCanExecuteChangedFor], [NotifyPropertyChangedRecipients] and [NotifyDataErrorInfo].",
        helpLinkUri: "https://aka.ms/mvvmtoolkit/errors/mvvmtk0020");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> indicating when an unsupported C# language version is being used.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedCSharpLanguageVersionError = new DiagnosticDescriptor(
        id: "MVVMTK0008",
        title: "Unsupported C# language version",
        messageFormat: "The source generator features from the MVVM Toolkit require consuming projects to set the C# language version to at least C# 8.0",
        category: typeof(CSharpParseOptions).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The source generator features from the MVVM Toolkit require consuming projects to set the C# language version to at least C# 8.0. Make sure to add <LangVersion>8.0</LangVersion> (or above) to your .csproj file.",
        helpLinkUri: "https://aka.ms/mvvmtoolkit/errors/mvvmtk0008");
  }
}
