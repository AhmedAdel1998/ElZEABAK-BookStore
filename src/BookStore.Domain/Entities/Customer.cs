using BookStore.Domain.Common;
using BookStore.Domain.Events;
using BookStore.Domain.Exceptions;
using BookStore.Domain.ValueObjects;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents a customer.
/// </summary>
public class Customer : BaseEntity, IAggregateRoot
{
    private Customer()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Customer"/> class.
    /// </summary>
    /// <param name="fullName">The customer full name.</param>
    public Customer(string fullName)
    {
        SetFullName(fullName);
        LoyaltyPoints = 0;
        IsActive = true;
        AddDomainEvent(new CustomerCreated(Id));
    }

    /// <summary>
    /// Gets the customer full name.
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

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
    /// Gets the loyalty points balance.
    /// </summary>
    public int LoyaltyPoints { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the customer is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Adds loyalty points.
    /// </summary>
    /// <param name="points">The points to add.</param>
    public void AddPoints(int points)
    {
        if (points <= 0)
        {
            throw new BusinessRuleException("Points to add must be greater than zero.");
        }

        LoyaltyPoints += points;
        MarkUpdated();
    }

    /// <summary>
    /// Redeems loyalty points.
    /// </summary>
    /// <param name="points">The points to redeem.</param>
    public void RedeemPoints(int points)
    {
        if (points <= 0)
        {
            throw new BusinessRuleException("Points to redeem must be greater than zero.");
        }

        if (LoyaltyPoints - points < 0)
        {
            throw new BusinessRuleException("Customer loyalty points cannot be negative.");
        }

        LoyaltyPoints -= points;
        MarkUpdated();
    }

    /// <summary>
    /// Updates contact information.
    /// </summary>
    /// <param name="phone">The phone number.</param>
    /// <param name="email">The email address.</param>
    /// <param name="address">The address.</param>
    public void UpdateContact(PhoneNumber? phone, Email? email, Address? address)
    {
        Phone = phone;
        Email = email;
        Address = address;
        MarkUpdated();
    }

    /// <summary>
    /// Updates customer profile information.
    /// </summary>
    /// <param name="fullName">The customer full name.</param>
    /// <param name="phone">The phone number.</param>
    /// <param name="email">The email address.</param>
    /// <param name="address">The address.</param>
    /// <param name="isActive">A value indicating whether the customer is active.</param>
    public void UpdateProfile(string fullName, PhoneNumber? phone, Email? email, Address? address, bool isActive)
    {
        SetFullName(fullName);
        Phone = phone;
        Email = email;
        Address = address;
        IsActive = isActive;
        MarkUpdated();
    }

    /// <summary>
    /// Activates the customer account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    /// <summary>
    /// Deactivates the customer account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationException("Customer full name is required.");
        }

        FullName = fullName.Trim();
    }
}
