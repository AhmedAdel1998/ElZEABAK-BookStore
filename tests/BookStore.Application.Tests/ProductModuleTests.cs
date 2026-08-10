using AutoMapper;
using BookStore.Application.Features.Products.Commands.CreateProduct;
using BookStore.Application.Features.Products.Commands.DeleteProduct;
using BookStore.Application.Features.Products.Commands.UpdateProduct;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Handlers;
using BookStore.Application.Features.Products.Mappings;
using BookStore.Application.Features.Products.Queries.SearchProducts;
using BookStore.Application.Features.Products.Validators;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.Specifications;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public class ProductModuleTests
{
    [Fact]
    public async Task CreateProduct_CreatesProduct_WhenValid()
    {
        var fixture = new ProductFixture();
        var result = await fixture.CreateCreateHandler().HandleAsync(new CreateProductRequest(fixture.CreateModel("BC-100", "Clean Code")));

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.Repository.Products);
        Assert.Equal("Clean Code", result.Value?.Title);
    }

    [Fact]
    public async Task UpdateProduct_UpdatesProduct_WhenValid()
    {
        var fixture = new ProductFixture();
        var product = fixture.Repository.AddExisting("BC-100", "Old Title");
        var model = fixture.CreateModel("BC-101", "New Title");
        model.Id = product.Id;

        var result = await fixture.CreateUpdateHandler().HandleAsync(new UpdateProductRequest(model));

        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", product.Title);
        Assert.Equal("BC-101", product.Barcode.Value);
    }

    [Fact]
    public async Task DeleteProduct_SoftDeletesProduct_WhenNoCompletedSaleReferences()
    {
        var fixture = new ProductFixture();
        var product = fixture.Repository.AddExisting("BC-100", "Clean Code");

        var result = await fixture.CreateDeleteHandler().HandleAsync(new DeleteProductRequest(product.Id));

        Assert.True(result.Succeeded);
        Assert.True(product.IsDeleted);
    }

    [Fact]
    public async Task DeleteProduct_BlocksDelete_WhenCompletedSaleReferencesExist()
    {
        var fixture = new ProductFixture();
        var product = fixture.Repository.AddExisting("BC-100", "Clean Code");
        fixture.Repository.CompletedSaleReferences.Add(product.Id);

        var result = await fixture.CreateDeleteHandler().HandleAsync(new DeleteProductRequest(product.Id));

        Assert.False(result.Succeeded);
        Assert.Equal("Product.CompletedSaleReference", result.Errors.Single().Code);
    }

    [Fact]
    public async Task SearchProducts_SearchesByTitleAndAuthor()
    {
        var fixture = new ProductFixture();
        fixture.Repository.AddExisting("BC-100", "Clean Code", author: "Robert Martin");
        fixture.Repository.AddExisting("BC-200", "Domain Modeling", author: "Eric Evans");

        var result = await fixture.CreateSearchHandler().HandleAsync(new SearchProductsRequest(new ProductFilter { SearchTerm = "eric" }));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Domain Modeling", result.Value.Items.Single().Title);
    }

    [Fact]
    public async Task ProductValidation_RejectsDuplicateBarcode()
    {
        var fixture = new ProductFixture();
        fixture.Repository.AddExisting("BC-100", "Clean Code");
        var validator = new ProductEditorModelValidator(fixture.Repository);

        var result = await validator.ValidateAsync(fixture.CreateModel("BC-100", "Other"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Duplicate barcode.");
    }

    [Fact]
    public async Task ProductValidation_RejectsInvalidIsbn()
    {
        var fixture = new ProductFixture();
        var model = fixture.CreateModel("BC-100", "Clean Code");
        model.ISBN = "123";

        var result = await new ProductEditorModelValidator(fixture.Repository).ValidateAsync(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Invalid ISBN.");
    }

    [Fact]
    public async Task ProductValidation_RejectsInvalidImageExtension()
    {
        var fixture = new ProductFixture();
        var model = fixture.CreateModel("BC-100", "Clean Code");
        model.ImagePath = "cover.txt";

        var result = await new ProductEditorModelValidator(fixture.Repository).ValidateAsync(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Image must be JPG, PNG, or WEBP.");
    }

    [Fact]
    public void PermissionChecks_UseProductPermissionConstants()
    {
        var authorization = new FakeAuthorizationService([PermissionConstants.ProductView, PermissionConstants.ProductExport]);

        Assert.True(authorization.HasPermission(PermissionConstants.ProductView));
        Assert.True(authorization.HasPermission(PermissionConstants.ProductExport));
        Assert.False(authorization.HasPermission(PermissionConstants.ProductDelete));
    }

    private sealed class ProductFixture
    {
        public ProductFixture()
        {
            Repository = new FakeProductRepository();
            UnitOfWork = new FakeUnitOfWork(Repository);
            Mapper = new MapperConfiguration(configuration => configuration.AddProfile<ProductMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        }

        public FakeProductRepository Repository { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public IMapper Mapper { get; }

        public ProductEditorModel CreateModel(string barcode, string title) => new()
        {
            Barcode = barcode,
            Title = title,
            Author = "Author",
            CategoryId = Guid.NewGuid(),
            PurchasePrice = 10,
            SellingPrice = 15,
            Quantity = 1,
            MinimumStock = 0,
            IsActive = true
        };

        public Product AddExisting(string barcode, string title) => Repository.AddExisting(barcode, title);

        public CreateProductHandler CreateCreateHandler()
        {
            var productValidator = new ProductEditorModelValidator(Repository);
            return new CreateProductHandler(UnitOfWork, new CreateProductRequestValidator(productValidator), Mapper, NullLogger<CreateProductHandler>.Instance);
        }

        public UpdateProductHandler CreateUpdateHandler()
        {
            var productValidator = new ProductEditorModelValidator(Repository);
            return new UpdateProductHandler(UnitOfWork, new UpdateProductRequestValidator(productValidator), Mapper, NullLogger<UpdateProductHandler>.Instance);
        }

        public DeleteProductHandler CreateDeleteHandler()
        {
            return new DeleteProductHandler(UnitOfWork, new DeleteProductRequestValidator(), NullLogger<DeleteProductHandler>.Instance);
        }

        public SearchProductsHandler CreateSearchHandler()
        {
            return new SearchProductsHandler(UnitOfWork, new SearchProductsRequestValidator(), Mapper, NullLogger<SearchProductsHandler>.Instance);
        }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> Products { get; } = [];
        public HashSet<Guid> CompletedSaleReferences { get; } = [];

        public Product AddExisting(string barcode, string title, string author = "Author")
        {
            var product = new Product(new Barcode(barcode), title, 10, 15, Guid.NewGuid());
            product.UpdateDetails(null, null, author, null, null, null);
            Products.Add(product);
            return product;
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Products.FirstOrDefault(product => product.Id == id && !product.IsDeleted));
        }

        public Task<IReadOnlyCollection<Product>> ListAsync(ISpecification<Product>? specification = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Product> products = Products.Where(product => !product.IsDeleted).ToArray();
            return Task.FromResult(products);
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            Products.Add(product);
            return Task.CompletedTask;
        }

        public void Remove(Product product) => Products.Remove(product);

        public Task<IReadOnlyCollection<Product>> SearchAsync(string? searchTerm, bool? isActive, bool lowStockOnly, Guid? categoryId, decimal? minPrice, decimal? maxPrice, int? minQuantity, int? maxQuantity, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = Apply(searchTerm, isActive, lowStockOnly, categoryId, minPrice, maxPrice, minQuantity, maxQuantity);
            IReadOnlyCollection<Product> products = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray();
            return Task.FromResult(products);
        }

        public Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, bool lowStockOnly = false, Guid? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null, int? minQuantity = null, int? maxQuantity = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Apply(searchTerm, isActive, lowStockOnly, categoryId, minPrice, maxPrice, minQuantity, maxQuantity).Count());
        }

        public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludedProductId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Products.Any(product => product.Barcode.Value.Equals(barcode, StringComparison.OrdinalIgnoreCase) && (excludedProductId is null || product.Id != excludedProductId.Value) && !product.IsDeleted));
        }

        public Task<bool> ExistsByIsbnAsync(string isbn, Guid? excludedProductId = null, CancellationToken cancellationToken = default)
        {
            var normalized = isbn.Replace("-", string.Empty, StringComparison.Ordinal);
            return Task.FromResult(Products.Any(product => product.ISBN?.Value == normalized && (excludedProductId is null || product.Id != excludedProductId.Value) && !product.IsDeleted));
        }

        public Task<bool> HasCompletedSaleReferencesAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CompletedSaleReferences.Contains(productId));
        }

        public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Products.FirstOrDefault(product => product.Barcode.Value.Equals(barcode, StringComparison.OrdinalIgnoreCase) && !product.IsDeleted));
        }

        private IEnumerable<Product> Apply(string? searchTerm, bool? isActive, bool lowStockOnly, Guid? categoryId, decimal? minPrice, decimal? maxPrice, int? minQuantity, int? maxQuantity)
        {
            var query = Products.Where(product => !product.IsDeleted);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(product =>
                    product.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (product.Author?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    product.Barcode.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (isActive.HasValue) query = query.Where(product => product.IsActive == isActive.Value);
            if (lowStockOnly) query = query.Where(product => product.Quantity <= product.MinimumStock);
            if (categoryId.HasValue) query = query.Where(product => product.CategoryId == categoryId.Value);
            if (minPrice.HasValue) query = query.Where(product => product.SellingPrice >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(product => product.SellingPrice <= maxPrice.Value);
            if (minQuantity.HasValue) query = query.Where(product => product.Quantity >= minQuantity.Value);
            if (maxQuantity.HasValue) query = query.Where(product => product.Quantity <= maxQuantity.Value);
            return query.OrderBy(product => product.Title);
        }
    }

    private sealed class FakeUnitOfWork(FakeProductRepository products) : IUnitOfWork
    {
        public IProductRepository Products { get; } = products;
        public ICategoryRepository Categories => throw new NotSupportedException();
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
