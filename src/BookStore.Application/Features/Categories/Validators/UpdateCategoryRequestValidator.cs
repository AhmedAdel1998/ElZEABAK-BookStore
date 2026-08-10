using BookStore.Application.Features.Categories.Commands.UpdateCategory;
using BookStore.Domain.Interfaces;
using FluentValidation;

namespace BookStore.Application.Features.Categories.Validators;

/// <summary>
/// Validates category update requests.
/// </summary>
public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryRequestValidator"/> class.
    /// </summary>
    /// <param name="categoryRepository">The category repository.</param>
    public UpdateCategoryRequestValidator(ICategoryRepository categoryRepository)
    {
        RuleFor(request => request.Id)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.")
            .MustAsync(async (request, name, cancellationToken) => !await categoryRepository.ExistsByNameAsync(name, request.Id, cancellationToken))
            .WithMessage("Category already exists.");

        RuleFor(request => request.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}
