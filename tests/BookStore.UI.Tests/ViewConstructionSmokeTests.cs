using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;

namespace BookStore.UI.Tests;

public sealed class ViewConstructionSmokeTests
{
    [Fact]
    public void EveryParameterlessView_ConstructsWithApplicationResourcesOnStaThread()
    {
        Exception? failure = null;
        var constructed = 0;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                var viewTypes = typeof(App).Assembly.GetTypes()
                    .Where(type => !type.IsAbstract
                        && typeof(FrameworkElement).IsAssignableFrom(type)
                        && (type.Namespace?.StartsWith("BookStore.UI.Views", StringComparison.Ordinal) == true
                            || type.Namespace?.StartsWith("BookStore.UI.Controls", StringComparison.Ordinal) == true)
                        && type.GetConstructor(Type.EmptyTypes) is not null)
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .ToArray();

                foreach (var viewType in viewTypes)
                {
                    _ = Activator.CreateInstance(viewType);
                    constructed++;
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "WPF view construction did not complete within 30 seconds.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        Assert.True(constructed >= 20, $"Expected at least 20 constructible views/controls, but found {constructed}.");
    }
}
