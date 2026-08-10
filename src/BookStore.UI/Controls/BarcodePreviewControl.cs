using System.Windows;
using System.Windows.Controls;
using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.UI.Controls;

/// <summary>
/// Reusable barcode preview control.
/// </summary>
public class BarcodePreviewControl : Control
{
    /// <summary>Identifies the BarcodeValue dependency property.</summary>
    public static readonly DependencyProperty BarcodeValueProperty = DependencyProperty.Register(nameof(BarcodeValue), typeof(string), typeof(BarcodePreviewControl), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the Format dependency property.</summary>
    public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(nameof(Format), typeof(BarcodeFormat), typeof(BarcodePreviewControl), new PropertyMetadata(BarcodeFormat.Code128));

    /// <summary>Identifies the ProductTitle dependency property.</summary>
    public static readonly DependencyProperty ProductTitleProperty = DependencyProperty.Register(nameof(ProductTitle), typeof(string), typeof(BarcodePreviewControl), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the Zoom dependency property.</summary>
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(BarcodePreviewControl), new PropertyMetadata(1d));

    /// <summary>Gets or sets barcode value.</summary>
    public string BarcodeValue { get => (string)GetValue(BarcodeValueProperty); set => SetValue(BarcodeValueProperty, value); }

    /// <summary>Gets or sets barcode format.</summary>
    public BarcodeFormat Format { get => (BarcodeFormat)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>Gets or sets product title.</summary>
    public string ProductTitle { get => (string)GetValue(ProductTitleProperty); set => SetValue(ProductTitleProperty, value); }

    /// <summary>Gets or sets preview zoom.</summary>
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
}
