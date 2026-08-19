using AutoMapper;
using BookStore.Application.Features.Categories.Commands.CreateCategory;
using BookStore.Application.Features.Categories.Commands.DeleteCategory;
using BookStore.Application.Features.Categories.Commands.UpdateCategory;
using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Categories.Mappings;
using BookStore.Application.Features.Categories.Queries.SearchCategories;
using BookStore.Application.Features.Categories.Validators;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public class CategoryModuleTests
{
    [Fact]
    public async Task CreateCategory_CreatesCategory_WhenRequestIsValid()
    {
        var fixture = new CategoryFixture();
        var handler = fixture.CreateCreateHandler();

        var result = await handler.HandleAsync(new CreateCategoryRequest("Fiction", "Novels"));

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.Repository.Categories);
        Assert.Equal("Fiction", result.Value?.Name);
    }

    [Fact]
    public async Task UpdateCategory_UpdatesCategory_WhenCategoryExists()
    {
        var fixture = new CategoryFixture();
        var category = fixture.Repository.AddExisting("Fiction", "Old");
        var handler = fixture.CreateUpdateHandler();

        var result = await handler.HandleAsync(new UpdateCategoryRequest(category.Id, "Literature", "Updated", false));

        Assert.True(result.IsSuccess);
        Assert.Equal("Literature", category.Name);
        Assert.False(category.IsActive);
    }

    [Fact]
    public async Task DeleteCategory_SoftDeletesCategory_WhenNoProductsAreAssigned()
    {
        var fixture = new CategoryFixture();
        var category = fixture.Repository.AddExisting("Fiction", null);
        var handler = fixture.CreateDeleteHandler();

        var result = await handler.HandleAsync(new DeleteCategoryRequest(category.Id));

        Assert.True(result.Succeeded);
        Assert.True(category.IsDeleted);
    }

    [Fact]
    public async Task DeleteCategory_BlocksDelete_WhenProductsAreAssigned()
    {
        var fixture = new CategoryFixture();
        var category = fixture.Repository.AddExisting("Fiction", null);
        fixture.Repository.ProductCounts[category.Id] = 2;
        var handler = fixture.CreateDeleteHandler();

        var result = await handler.HandleAsync(new DeleteCategoryRequest(category.Id));

        Assert.False(result.Succeeded);
        Assert.Equal("Category.HasProducts", result.Errors.Single().Code);
        Assert.False(category.IsDeleted);
    }

    [Fact]
    public async Task SearchCategories_SearchesByNameAndDescription_CaseInsensitive()
    {
        var fixture = new CategoryFixture();
        fixture.Repository.AddExisting("Fiction", "Novels");
        fixture.Repository.AddExisting("Stationery", "Pens and notebooks");
        var handler = fixture.CreateSearchHandler();

        var result = await handler.HandleAsync(new SearchCategoriesRequest("note"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Stationery", result.Value.Items.Single().Name);
    }

    [Fact]
    public async Task CreateCategoryValidator_RejectsDuplicateName()
    {
        var fixture = new CategoryFixture();
        fixture.Repository.AddExisting("Fiction", null);
        var validator = new CreateCategoryRequestValidator(fixture.Repository);

        var result = await validator.ValidateAsync(new CreateCategoryRequest("fiction", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Category already exists.");
    }

    [Fact]
    public void PermissionChecks_UseCategoryPermissionConstants()
    {
        var authorization = new FakeAuthorizationService([BookStore.Shared.Constants.PermissionConstants.CategoryView]);

        Assert.True(authorization.HasPermission(BookStore.Shared.Constants.PermissionConstants.CategoryView));
        Assert.False(authorization.HasPermission(BookStore.Shared.Constants.PermissionConstants.CategoryDelete));
    }

    private sealed class CategoryFixture
    {
        public CategoryFixture()
        {
            var mapperConfiguration = new MapperConfiguration(configuration => configuration.AddProfile<CategoryMappingProfile>(), NullLoggerFactory.Instance);
            Mapper = mapperConfiguration.CreateMapper();
            Repository = new FakeCategoryRepository();
            UnitOfWork = new FakeUnitOfWork(Repository);
        }

        public FakeCategoryRepository Repository { get; }

        public IMapper Mapper { get; }

        public FakeUnitOfWork UnitOfWork { get; }

        public CreateCategoryHandler CreateCreateHandler()
        {
            return new CreateCategoryHandler(UnitOfWork, new CreateCategoryRequestValidator(Repository), Mapper, NullLogger<CreateCategoryHandler>.Instance);
        }

        public UpdateCategoryHandler CreateUpdateHandler()
        {
            return new UpdateCategoryHandler(UnitOfWork, new UpdateCategoryRequestValidator(Repository), Mapper, NullLogger<UpdateCategoryHandler>.Instance);
        }

        public DeleteCategoryHandler CreateDeleteHandler()
        {
            return new DeleteCategoryHandler(UnitOfWork, new DeleteCategoryRequestValidator(), NullLogger<DeleteCategoryHandler>.Instance);
        }

        public SearchCategoriesHandler CreateSearchHandler()
        {
            var getHandler = new GetCategoriesHandler(UnitOfWork, new GetCategoriesRequestValidator(), Mapper, NullLogger<GetCategoriesHandler>.Instance);
            return new SearchCategoriesHandler(getHandler, new SearchCategoriesRequestValidator(), NullLogger<SearchCategoriesHandler>.Instance);
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Categories { get; } = [];

        public Dictionary<Guid, int> ProductCounts { get; } = [];

        public Category AddExisting(string name, string? description)
        {
            var category = new Category(name, description);
            Categories.Add(category);
            return category;
        }

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Categories.FirstOrDefault(category => category.Id == id && !category.IsDeleted));
        }

        public Task<IReadOnlyCollection<Category>> ListAsync(ISpecification<Category>? specification = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Category> categories = Categories.Where(category => !category.IsDeleted).ToArray();
            return Task.FromResult(categories);
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Category>> SearchAsync(string? searchTerm, int pageNumber, int pageSize, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            var query = Categories.Where(category => !category.IsDeleted);
            if (isActive.HasValue)
            {
                query = query.Where(category => category.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(category =>
                    category.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (category.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            IReadOnlyCollection<Category> result = query
                .OrderBy(category => category.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult(result);
        }

        public async Task<int> CountAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            return (await SearchAsync(searchTerm, 1, int.MaxValue, null, cancellationToken)).Count;
        }

        public Task<bool> ExistsByNameAsync(string name, Guid? excludedCategoryId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Categories.Any(category =>
                !category.IsDeleted &&
                category.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (excludedCategoryId is null || category.Id != excludedCategoryId.Value)));
        }

        public Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ProductCounts.TryGetValue(categoryId, out var count) ? count : 0);
        }

        public Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, int> counts = categoryIds.ToDictionary(id => id, id => ProductCounts.TryGetValue(id, out var count) ? count : 0);
            return Task.FromResult(counts);
        }
    }

    private sealed class FakeUnitOfWork(FakeCategoryRepository categoryRepository) : IUnitOfWork
    {
        public IProductRepository Products => throw new NotSupportedException();

        public ICategoryRepository Categories { get; } = categoryRepository;

        public ICustomerRepository Customers => throw new NotSupportedException();

        public ISupplierRepository Suppliers => throw new NotSupportedException();

        public IUserRepository Users => throw new NotSupportedException();

        public IRoleRepository Roles => throw new NotSupportedException();

        public ISaleRepository Sales => throw new NotSupportedException();

        public IInventoryRepository Inventory => throw new NotSupportedException();

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);

        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);

        public bool HasRole(string role) => false;

        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }
}
