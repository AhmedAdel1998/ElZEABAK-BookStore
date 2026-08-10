namespace BookStore.UI.Navigation;

/// <summary>
/// Stores selected customer navigation state.
/// </summary>
public interface ICustomerNavigationState
{
    /// <summary>Gets or sets selected customer identifier.</summary>
    Guid? SelectedCustomerId { get; set; }
}

/// <summary>
/// Default selected customer navigation state.
/// </summary>
public sealed class CustomerNavigationState : ICustomerNavigationState
{
    /// <inheritdoc />
    public Guid? SelectedCustomerId { get; set; }
}
