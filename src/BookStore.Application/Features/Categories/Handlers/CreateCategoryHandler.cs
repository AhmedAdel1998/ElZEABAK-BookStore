using AutoMapper;
using BookStore.Application.Features.Categories.Commands.CreateCategory;
using BookStore.Application.Features.Categories.Responses;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Categories.Handlers;

/// <summary>
/// Handles category creation.
/// </summary>
public sealed class CreateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCategoryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryHandler"/> class.
    /// </summary>
    public CreateCategoryHandler(IUnitOfWork unitOfWork, IValidator<CreateCategoryRequest> validator, IMapper mapper, ILogger<CreateCategoryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    public async Task<Result<CategoryResponse>> HandleAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Category creation validation failed: {Errors}", string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));
            return Result<CategoryResponse>.Failure(validation.Errors[0].ErrorMessage);
        }

        try
        {
            var category = new Category(request.Name, request.Description);
            if (!request.IsActive)
            {
                category.Deactivate();
            }

            await _unitOfWork.Categories.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<CategoryResponse>(category);
            _logger.LogInformation("Category created: {CategoryId} {CategoryName}", category.Id, category.Name);
            return Result<CategoryResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while creating category");
            return Result<CategoryResponse>.Failure("Unable to create category.");
        }
    }
}
