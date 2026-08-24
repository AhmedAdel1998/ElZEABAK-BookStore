namespace BookStore.Shared.Constants;

/// <summary>
/// Limits shared by paged queries and the request validators that enforce them.
/// </summary>
public static class PagingConstants
{
    /// <summary>
    /// The largest page a query may ask for. Request validators reject anything above this, so a
    /// caller that wants "everything" for a picker has to ask for exactly this much: asking for
    /// more fails validation and returns nothing at all, which is how the product editor's category
    /// dropdown came to be permanently empty.
    /// </summary>
    public const int MaxPageSize = 200;
}
