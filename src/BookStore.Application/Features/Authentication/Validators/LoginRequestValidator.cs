using BookStore.Application.Features.Authentication.DTOs;
using FluentValidation;

namespace BookStore.Application.Features.Authentication.Validators;

/// <summary>
/// Validates login requests.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestValidator"/> class.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(request => request.Username)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
