namespace BookStore.Domain.Enums;

/// <summary>
/// Defines supported payment methods.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Cash payment.
    /// </summary>
    Cash = 0,

    /// <summary>
    /// Card payment.
    /// </summary>
    Card = 1,

    /// <summary>
    /// Mobile wallet payment.
    /// </summary>
    MobileWallet = 2,

    /// <summary>
    /// Bank transfer payment.
    /// </summary>
    BankTransfer = 3
}
