using System.Collections;
using System.Windows.Controls;

namespace BookStore.UI.Validation;

/// <summary>
/// Displays reusable validation summary content produced by FluentValidation-backed view models.
/// </summary>
public class ValidationSummaryControl : ItemsControl
{
    /// <summary>
    /// Sets validation messages displayed by the summary.
    /// </summary>
    /// <param name="items">The validation items.</param>
    public void SetMessages(IEnumerable items)
    {
        ItemsSource = items;
    }
}
