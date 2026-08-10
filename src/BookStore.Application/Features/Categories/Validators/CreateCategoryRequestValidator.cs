using BookStore.Application.Features.Categories.Commands.CreateCategory;
using BookStore.Domain.Interfaces;
using FluentValidation;

namespace BookStore.Application.Features.Categories.Validators;

/// <summary>
/// Validates category creation requests.
/// </summary>
public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryRequestValidator"/> class.
    /// </summary>
    /// <param name="categoryRepository">The category repository.</param>
    public CreateCategoryRequestValidator(ICategoryRepository categoryRepository)
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.")
            .MustAsync(async (name, cancellationToken) => !await categoryRepository.ExistsByNameAsync(name, null, cancellationToken))
            .WithMessage("Category already exists.");

        RuleFor(request => request.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}
