namespace BookStore.Shared.Results;

/// <summary>
/// Represents an operation result with structured errors and validation failures.
/// </summary>
public class OperationResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets operation errors.
    /// </summary>
    public IReadOnlyCollection<Error> Errors { get; init; } = [];

    /// <summary>
    /// Gets validation errors.
    /// </summary>
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; init; } = [];

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    /// <returns>A successful operation result.</returns>
    public static OperationResult Success() => new() { Succeeded = true };

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="errors">The operation errors.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Failure(params Error[] errors) => new() { Succeeded = false, Errors = errors };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="validationErrors">The validation errors.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Invalid(params ValidationError[] validationErrors) => new() { Succeeded = false, ValidationErrors = validationErrors };
}
