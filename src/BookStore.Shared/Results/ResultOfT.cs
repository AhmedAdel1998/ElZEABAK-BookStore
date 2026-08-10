namespace BookStore.Shared.Results;

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The value type returned by the operation.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the operation value when the operation succeeds.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The successful operation value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public new static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
