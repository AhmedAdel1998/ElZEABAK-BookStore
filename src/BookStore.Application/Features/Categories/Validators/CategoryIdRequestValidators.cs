using BookStore.Application.Features.Categories.Commands.ActivateCategory;
using BookStore.Application.Features.Categories.Commands.DeactivateCategory;
using BookStore.Application.Features.Categories.Commands.DeleteCategory;
using BookStore.Application.Features.Categories.Queries.GetCategoryById;
using FluentValidation;

namespace BookStore.Application.Features.Categories.Validators;

/// <summary>
/// Validates category delete requests.
/// </summary>
public sealed class DeleteCategoryRequestValidator : AbstractValidator<DeleteCategoryRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteCategoryRequestValidator"/> class.</summary>
    public DeleteCategoryRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Category is required.");
}

/// <summary>
/// Validates category activation requests.
/// </summary>
public sealed class ActivateCategoryRequestValidator : AbstractValidator<ActivateCategoryRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ActivateCategoryRequestValidator"/> class.</summary>
    public ActivateCategoryRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Category is required.");
}

/// <summary>
/// Validates category deactivation requests.
/// </summary>
public sealed class DeactivateCategoryRequestValidator : AbstractValidator<DeactivateCategoryRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeactivateCategoryRequestValidator"/> class.</summary>
    public DeactivateCategoryRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Category is required.");
}

/// <summary>
/// Validates category-by-id queries.
/// </summary>
public sealed class GetCategoryByIdRequestValidator : AbstractValidator<GetCategoryByIdRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetCategoryByIdRequestValidator"/> class.</summary>
    public GetCategoryByIdRequestValidator() => RuleFor(request => request.Id).NotEmpty().WithMessage("Category is required.");
}
