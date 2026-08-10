using BookStore.Application.Features.Categories.Queries.GetCategories;
using BookStore.Application.Features.Categories.Queries.SearchCategories;
using FluentValidation;

namespace BookStore.Application.Features.Categories.Validators;

/// <summary>
/// Validates category list requests.
/// </summary>
public sealed class GetCategoriesRequestValidator : AbstractValidator<GetCategoriesRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GetCategoriesRequestValidator"/> class.</summary>
    public GetCategoriesRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
    }
}

/// <summary>
/// Validates category search requests.
/// </summary>
public sealed class SearchCategoriesRequestValidator : AbstractValidator<SearchCategoriesRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SearchCategoriesRequestValidator"/> class.</summary>
    public SearchCategoriesRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 200);
        RuleFor(request => request.SearchTerm).MaximumLength(100);
    }
}
