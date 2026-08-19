using System.Windows.Controls;

namespace BookStore.UI.Controls;

/// <summary>
/// Shared paging footer for list views. Binds to the hosting view model's
/// <c>PageNumber</c>, <c>TotalPages</c>, <c>TotalCount</c>, <c>NextPageCommand</c> and
/// <c>PreviousPageCommand</c> through the inherited data context.
/// </summary>
public partial class PaginationBar : UserControl
{
    /// <summary>Initializes a new instance of the <see cref="PaginationBar"/> class.</summary>
    public PaginationBar()
    {
        InitializeComponent();
    }
}
