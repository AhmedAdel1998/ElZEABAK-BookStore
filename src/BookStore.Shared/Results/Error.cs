namespace BookStore.Shared.Results;

/// <summary>
/// Represents an application error.
/// </summary>
/// <param name="Code">The stable error code.</param>
/// <param name="Message">The user-safe error message.</param>
public sealed record Error(string Code, string Message);
