using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;
using BookStore.Domain.ValueObjects;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents a supplier.
/// </summary>
public class Supplier : BaseEntity, IAggregateRoot
{
    private Supplier()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Supplier"/> class.
    /// </summary>
    /// <param name="companyName">The supplier company name.</param>
    public Supplier(string companyName)
    {
        SetCompanyName(companyName);
        IsActive = true;
    }

    /// <summary>
    /// Gets the company name.
    /// </summary>
    public string CompanyName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the contact name.
    /// </summary>
    public string? ContactName { get; private set; }

    /// <summary>
    /// Gets the phone number.
    /// </summary>
    public PhoneNumber? Phone { get; private set; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public Email? Email { get; private set; }

    /// <summary>
    /// Gets the address.
    /// </summary>
    public Address? Address { get; private set; }

    /// <summary>
    /// Gets supplier notes.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the supplier is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Updates supplier details.
    /// </summary>
    /// <param name="contactName">The contact name.</param>
    /// <param name="phone">The phone number.</param>
    /// <param name="email">The email address.</param>
    /// <param name="address">The address.</param>
    /// <param name="notes">Supplier notes.</param>
    public void UpdateDetails(string? contactName, PhoneNumber? phone, Email? email, Address? address, string? notes)
    {
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
        Notes = notes;
        MarkUpdated();
    }

    private void SetCompanyName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new ValidationException("Supplier company name is required.");
        }

        CompanyName = companyName.Trim();
    }
}
