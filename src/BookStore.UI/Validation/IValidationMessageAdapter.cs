using FluentValidation.Results;

namespace BookStore.UI.Validation;

/// <summary>
/// Converts FluentValidation results into UI validation summary items.
/// </summary>
public interface IValidationMessageAdapter
{
    /// <summary>
    /// Converts a validation result into reusable validation summary items.
    /// </summary>
    /// <param name="validationResult">The FluentValidation result.</param>
    /// <returns>The validation summary items.</returns>
    IReadOnlyList<ValidationSummaryItem> Convert(ValidationResult validationResult);
}
