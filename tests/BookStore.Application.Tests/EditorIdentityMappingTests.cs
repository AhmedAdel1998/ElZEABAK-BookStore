using AutoMapper;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Mappings;
using BookStore.Application.Features.Suppliers.DTOs;
using BookStore.Application.Features.Suppliers.Mappings;
using BookStore.Domain.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BookStore.Application.Tests;

/// <summary>
/// Guards the identity round-trip that broke customer and supplier editing.
/// </summary>
/// <remarks>
/// <c>CustomerDto</c> and <c>SupplierDto</c> used to declare <c>public new Guid Id</c>, shadowing
/// the nullable <c>Id</c> on their editor base class. AutoMapper writes only the most-derived
/// member of a given name, so the base stayed null. The editor view models take the base type as
/// their parameter, read that null, and fell through to the *create* branch on save -- which meant
/// an existing customer could never be edited, and changing the phone number produced a duplicate
/// record while orphaning the original with its sales history.
/// </remarks>
public class EditorIdentityMappingTests
{
    private static IMapper CustomerMapper() =>
        new MapperConfiguration(configuration => configuration.AddProfile<CustomerMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static IMapper SupplierMapper() =>
        new MapperConfiguration(configuration => configuration.AddProfile<SupplierMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void CustomerDtoExposesItsIdentityThroughTheEditorBaseType()
    {
        var id = Guid.NewGuid();
        var dto = CustomerMapper().Map<CustomerDto>(new CustomerListReadModel { Id = id, FullName = "Layla", Phone = "01001234567" });

        // Read through the base type, exactly as CustomerEditorViewModel.ApplyModel does.
        CustomerEditorModel asEditorModel = dto;

        Assert.Equal(id, asEditorModel.Id);
        Assert.Equal(id, dto.PersistedId);
    }

    [Fact]
    public void SupplierDtoExposesItsIdentityThroughTheEditorBaseType()
    {
        var id = Guid.NewGuid();
        var dto = SupplierMapper().Map<SupplierDto>(new SupplierListReadModel { Id = id, CompanyName = "Dar Al Maaref", Phone = "0223456789" });

        SupplierEditorModel asEditorModel = dto;

        Assert.Equal(id, asEditorModel.Id);
        Assert.Equal(id, dto.PersistedId);
    }

    [Fact]
    public void CustomerListItemKeepsItsIdentity()
    {
        var id = Guid.NewGuid();
        var item = CustomerMapper().Map<CustomerListItem>(new CustomerListReadModel { Id = id, FullName = "Omar", Phone = "01112223334" });

        Assert.Equal(id, item.Id);
        Assert.Equal(id, item.PersistedId);
    }

    [Fact]
    public void SupplierListItemKeepsItsIdentity()
    {
        var id = Guid.NewGuid();
        var item = SupplierMapper().Map<SupplierListItem>(new SupplierListReadModel { Id = id, CompanyName = "Nahdet Misr", Phone = "0223456789" });

        Assert.Equal(id, item.Id);
        Assert.Equal(id, item.PersistedId);
    }

    [Fact]
    public void PersistedIdFailsLoudlyWhenTheRecordWasNeverSaved()
    {
        Assert.Throws<InvalidOperationException>(() => new CustomerDto().PersistedId);
        Assert.Throws<InvalidOperationException>(() => new SupplierDto().PersistedId);
    }
}
