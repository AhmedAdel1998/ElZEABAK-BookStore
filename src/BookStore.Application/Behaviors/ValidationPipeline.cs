using FluentValidation;
using BookStore.Shared.Results;

namespace BookStore.Application.Behaviors;

/// <summary>
/// Provides reusable validation execution for future command and query handlers.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public class ValidationPipeline<TRequest>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPipeline{TRequest}"/> class.
    /// </summary>
    /// <param name="validators">Validators registered for the request type.</param>
    public ValidationPipeline(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Validates a request with every registered validator.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The validation failures produced by registered validators.</returns>
    public async Task<IReadOnlyCollection<ValidationError>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            errors.AddRange(result.Errors.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage)));
        }

        return errors;
    }
}
