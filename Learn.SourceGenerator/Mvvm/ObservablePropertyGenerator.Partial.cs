using System;
using System.Collections.Generic;
using System.Text;

namespace Learn.SourceGenerator.Mvvm
{
  internal partial class ObservablePropertyGenerator
  {
    /// <summary>
    /// Validates the containing type for a given field being annotated.
    /// </summary>
    /// <param name="fieldSymbol">The input <see cref="IFieldSymbol"/> instance to process.</param>
    /// <param name="shouldInvokeOnPropertyChanging">Whether or not property changing events should also be raised.</param>
    /// <returns>Whether or not the containing type for <paramref name="fieldSymbol"/> is valid.</returns>
    private static bool IsTargetTypeValid(
        IFieldSymbol fieldSymbol,
        out bool shouldInvokeOnPropertyChanging)
    {
      // The [ObservableProperty] attribute can only be used in types that are known to expose the necessary OnPropertyChanged and OnPropertyChanging methods.
      // That means that the containing type for the field needs to match one of the following conditions:
      //   - It inherits from ObservableObject (in which case it also implements INotifyPropertyChanging).
      //   - It has the [ObservableObject] attribute (on itself or any of its base types).
      //   - It has the [INotifyPropertyChanged] attribute (on itself or any of its base types).
      bool isObservableObject = fieldSymbol.ContainingType.InheritsFromFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.ObservableObject");
      bool hasObservableObjectAttribute = fieldSymbol.ContainingType.HasOrInheritsAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.ObservableObjectAttribute");
      bool hasINotifyPropertyChangedAttribute = fieldSymbol.ContainingType.HasOrInheritsAttributeWithFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.INotifyPropertyChangedAttribute");

      shouldInvokeOnPropertyChanging = isObservableObject || hasObservableObjectAttribute;

      return isObservableObject || hasObservableObjectAttribute || hasINotifyPropertyChangedAttribute;
    }

    /// <summary>
    /// Processes a given field.
    /// </summary>
    /// <param name="fieldSymbol">The input <see cref="IFieldSymbol"/> instance to process.</param>
    /// <param name="diagnostics">The resulting diagnostics from the processing operation.</param>
    /// <returns>The resulting <see cref="PropertyInfo"/> instance for <paramref name="fieldSymbol"/>, if successful.</returns>
    public static PropertyInfo? TryGetInfo(IFieldSymbol fieldSymbol, out ImmutableArray<Diagnostic> diagnostics)
    {
      ImmutableArray<Diagnostic>.Builder builder = ImmutableArray.CreateBuilder<Diagnostic>();

      // Validate the target type
      if (!IsTargetTypeValid(fieldSymbol, out bool shouldInvokeOnPropertyChanging))
      {
        builder.Add(
            InvalidContainingTypeForObservablePropertyFieldError,
            fieldSymbol,
            fieldSymbol.ContainingType,
            fieldSymbol.Name);

        diagnostics = builder.ToImmutable();

        return null;
      }

      // Get the property type and name
      string typeNameWithNullabilityAnnotations = fieldSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations();
      string fieldName = fieldSymbol.Name;
      string propertyName = GetGeneratedPropertyName(fieldSymbol);

      // Check for name collisions
      if (fieldName == propertyName)
      {
        builder.Add(
            ObservablePropertyNameCollisionError,
            fieldSymbol,
            fieldSymbol.ContainingType,
            fieldSymbol.Name);

        diagnostics = builder.ToImmutable();

        // If the generated property would collide, skip generating it entirely. This makes sure that
        // users only get the helpful diagnostic about the collision, and not the normal compiler error
        // about a definition for "Property" already existing on the target type, which might be confusing.
        return null;
      }

      // Check for special cases that are explicitly not allowed
      if (IsGeneratedPropertyInvalid(propertyName, fieldSymbol.Type))
      {
        builder.Add(
            InvalidObservablePropertyError,
            fieldSymbol,
            fieldSymbol.ContainingType,
            fieldSymbol.Name);

        diagnostics = builder.ToImmutable();

        return null;
      }

      ImmutableArray<string>.Builder propertyChangedNames = ImmutableArray.CreateBuilder<string>();
      ImmutableArray<string>.Builder propertyChangingNames = ImmutableArray.CreateBuilder<string>();
      ImmutableArray<string>.Builder notifiedCommandNames = ImmutableArray.CreateBuilder<string>();
      ImmutableArray<AttributeInfo>.Builder forwardedAttributes = ImmutableArray.CreateBuilder<AttributeInfo>();
      bool notifyRecipients = false;
      bool notifyDataErrorInfo = false;
      bool hasOrInheritsClassLevelNotifyPropertyChangedRecipients = false;
      bool hasOrInheritsClassLevelNotifyDataErrorInfo = false;
      bool hasAnyValidationAttributes = false;

      // Track the property changing event for the property, if the type supports it
      if (shouldInvokeOnPropertyChanging)
      {
        propertyChangingNames.Add(propertyName);
      }

      // The current property is always notified
      propertyChangedNames.Add(propertyName);

      // Get the class-level [NotifyPropertyChangedRecipients] setting, if any
      if (TryGetIsNotifyingRecipients(fieldSymbol, out bool isBroadcastTargetValid))
      {
        notifyRecipients = isBroadcastTargetValid;
        hasOrInheritsClassLevelNotifyPropertyChangedRecipients = true;
      }

      // Get the class-level [NotifyDataErrorInfo] setting, if any
      if (TryGetNotifyDataErrorInfo(fieldSymbol, out bool isValidationTargetValid))
      {
        notifyDataErrorInfo = isValidationTargetValid;
        hasOrInheritsClassLevelNotifyDataErrorInfo = true;
      }

      // Gather attributes info
      foreach (AttributeData attributeData in fieldSymbol.GetAttributes())
      {
        // Gather dependent property and command names
        if (TryGatherDependentPropertyChangedNames(fieldSymbol, attributeData, propertyChangedNames, builder) ||
            TryGatherDependentCommandNames(fieldSymbol, attributeData, notifiedCommandNames, builder))
        {
          continue;
        }

        // Check whether the property should also notify recipients
        if (TryGetIsNotifyingRecipients(fieldSymbol, attributeData, builder, hasOrInheritsClassLevelNotifyPropertyChangedRecipients, out isBroadcastTargetValid))
        {
          notifyRecipients = isBroadcastTargetValid;

          continue;
        }

        // Check whether the property should also be validated
        if (TryGetNotifyDataErrorInfo(fieldSymbol, attributeData, builder, hasOrInheritsClassLevelNotifyDataErrorInfo, out isValidationTargetValid))
        {
          notifyDataErrorInfo = isValidationTargetValid;

          continue;
        }

        // Track the current attribute for forwarding if it is a validation attribute
        if (attributeData.AttributeClass?.InheritsFromFullyQualifiedName("global::System.ComponentModel.DataAnnotations.ValidationAttribute") == true)
        {
          hasAnyValidationAttributes = true;

          forwardedAttributes.Add(AttributeInfo.From(attributeData));
        }

        // Also track the current attribute for forwarding if it is of any of the following types:
        //   - Display attributes (System.ComponentModel.DataAnnotations.DisplayAttribute)
        //   - UI hint attributes(System.ComponentModel.DataAnnotations.UIHintAttribute)
        //   - Scaffold column attributes (System.ComponentModel.DataAnnotations.ScaffoldColumnAttribute)
        //   - Editable attributes (System.ComponentModel.DataAnnotations.EditableAttribute)
        //   - Key attributes (System.ComponentModel.DataAnnotations.KeyAttribute)
        if (attributeData.AttributeClass?.HasOrInheritsFromFullyQualifiedName("global::System.ComponentModel.DataAnnotations.UIHintAttribute") == true ||
            attributeData.AttributeClass?.HasOrInheritsFromFullyQualifiedName("global::System.ComponentModel.DataAnnotations.ScaffoldColumnAttribute") == true ||
            attributeData.AttributeClass?.HasFullyQualifiedName("global::System.ComponentModel.DataAnnotations.DisplayAttribute") == true ||
            attributeData.AttributeClass?.HasFullyQualifiedName("global::System.ComponentModel.DataAnnotations.EditableAttribute") == true ||
            attributeData.AttributeClass?.HasFullyQualifiedName("global::System.ComponentModel.DataAnnotations.KeyAttribute") == true)
        {
          forwardedAttributes.Add(AttributeInfo.From(attributeData));
        }
      }

      // Log the diagnostic for missing ObservableValidator, if needed
      if (hasAnyValidationAttributes &&
          !fieldSymbol.ContainingType.InheritsFromFullyQualifiedName("global::CommunityToolkit.Mvvm.ComponentModel.ObservableValidator"))
      {
        builder.Add(
            MissingObservableValidatorInheritanceForValidationAttributeError,
            fieldSymbol,
            fieldSymbol.ContainingType,
            fieldSymbol.Name,
            forwardedAttributes.Count);
      }

      // Log the diagnostic for missing validation attributes, if any
      if (notifyDataErrorInfo && !hasAnyValidationAttributes)
      {
        builder.Add(
            MissingValidationAttributesForNotifyDataErrorInfoError,
            fieldSymbol,
            fieldSymbol.ContainingType,
            fieldSymbol.Name);
      }

      diagnostics = builder.ToImmutable();

      return new(
          typeNameWithNullabilityAnnotations,
          fieldName,
          propertyName,
          propertyChangingNames.ToImmutable(),
          propertyChangedNames.ToImmutable(),
          notifiedCommandNames.ToImmutable(),
          notifyRecipients,
          notifyDataErrorInfo,
          forwardedAttributes.ToImmutable());
    }

    /// <summary>
    /// Gets the diagnostics for a field with invalid attribute uses.
    /// </summary>
    /// <param name="fieldSymbol">The input <see cref="IFieldSymbol"/> instance to process.</param>
    /// <returns>The resulting <see cref="Diagnostic"/> instance for <paramref name="fieldSymbol"/>.</returns>
    public static Diagnostic GetDiagnosticForFieldWithOrphanedDependentAttributes(IFieldSymbol fieldSymbol)
    {
      return FieldWithOrphanedDependentObservablePropertyAttributesError.CreateDiagnostic(
          fieldSymbol,
          fieldSymbol.ContainingType,
          fieldSymbol.Name);
    }
  }
}
