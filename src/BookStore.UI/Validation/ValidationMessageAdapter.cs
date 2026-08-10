using FluentValidation.Results;

namespace BookStore.UI.Validation;

/// <summary>
/// Default FluentValidation-to-UI validation adapter.
/// </summary>
public sealed class ValidationMessageAdapter : IValidationMessageAdapter
{
    /// <inheritdoc />
    public IReadOnlyList<ValidationSummaryItem> Convert(ValidationResult validationResult)
    {
        return validationResult.Errors
            .Select(error => new ValidationSummaryItem(error.PropertyName, error.ErrorMessage))
            .ToArray();
    }
}
