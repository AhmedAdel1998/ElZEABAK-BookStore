using System.Globalization;
using System.Threading;
using System.Windows;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookStore.UI.Services;

/// <summary>
/// Lightweight runtime localization service for WPF resource keys and RTL layout.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["App.Title"] = "EL ZEABAK BookStore",
        ["App.Product"] = "BookStore POS",
        ["Auth.SignIn"] = "Sign in",
        ["Auth.SignInSubtitle"] = "Continue to the bookstore dashboard",
        ["Auth.Username"] = "Username",
        ["Auth.Password"] = "Password",
        ["Auth.ShowPassword"] = "Show",
        ["Auth.RememberMe"] = "Remember me",
        ["Auth.Login"] = "Login",
        ["Auth.Exit"] = "Exit",
        ["Auth.SigningIn"] = "Signing in...",
        ["Setup.CreateAdministrator"] = "Create Administrator",
        ["Setup.Subtitle"] = "First-run setup for this bookstore database",
        ["Setup.FullName"] = "Full Name",
        ["Setup.Email"] = "Email",
        ["Setup.ConfirmPassword"] = "Confirm Password",
        ["Setup.CreateAdmin"] = "Create Admin",
        ["Setup.Creating"] = "Creating administrator...",
        ["Shell.SearchTooltip"] = "Global search",
        ["Shell.Theme"] = "Theme",
        ["Shell.Notifications"] = "Notifications",
        ["Shell.ClearNotifications"] = "Clear",
        ["Shell.NoNotifications"] = "No active notifications.",
        ["Shell.Settings"] = "Settings",
        ["Shell.Refresh"] = "Refresh",
        ["Shell.User"] = "User: ",
        ["Shell.Database"] = "   DB: ",
        ["Shell.Internet"] = "   Internet: ",
        ["Shell.Tasks"] = "   Tasks: ",
        ["Status.Connected"] = "Connected",
        ["Status.Offline"] = "Offline",
        ["Auth.PermissionDenied"] = "You do not have permission to open this area.",
        ["Loading.Default"] = "Loading...",
        ["Loading.OpeningPage"] = "Opening page...",
        ["Nav.Dashboard"] = "Dashboard",
        ["Nav.Categories"] = "Categories",
        ["Nav.Products"] = "Products",
        ["Nav.Inventory"] = "Inventory",
        ["Nav.Barcode"] = "Barcode",
        ["Nav.SalesPOS"] = "Sales (POS)",
        ["Nav.Customers"] = "Customers",
        ["Nav.Suppliers"] = "Suppliers",
        ["Nav.Reports"] = "Reports",
        ["Nav.Receipts"] = "Receipts",
        ["Nav.Settings"] = "Settings",
        ["Nav.Users"] = "Users",
        ["Nav.Roles"] = "Roles",
        ["Nav.Backup"] = "Backup",
        ["Nav.Logout"] = "Logout",
        ["POS.Cashier"] = "POS Cashier",
        ["POS.Title"] = "POS",
        ["POS.NewSale"] = "New Sale",
        ["POS.Suspend"] = "Suspend",
        ["POS.Cancel"] = "Cancel",
        ["POS.CancelTitle"] = "Cancel sale",
        ["POS.CancelConfirm"] = "Cancel the active sale?",
        ["POS.Scan"] = "Scan",
        ["POS.Add"] = "Add",
        ["POS.AutoAdd"] = "Auto-add scanned QR",
        ["POS.Category"] = "Category: ",
        ["POS.Search"] = "Search",
        ["POS.Find"] = "Find",
        ["POS.Customer"] = "Customer",
        ["POS.Select"] = "Select",
        ["POS.WalkIn"] = "Walk-in",
        ["POS.CreateCustomer"] = "Create Customer",
        ["POS.SelectedQuantity"] = "Selected Quantity",
        ["POS.AddProduct"] = "Add Product",
        ["POS.Product"] = "Product",
        ["POS.Barcode"] = "Barcode",
        ["POS.Qty"] = "Qty",
        ["POS.StockLeft"] = "Stock Left",
        ["POS.Price"] = "Price",
        ["POS.Discount"] = "Discount",
        ["POS.Tax"] = "Tax",
        ["POS.Total"] = "Total",
        ["POS.UpdateQty"] = "Update Qty",
        ["POS.LineDiscount"] = "Line Discount",
        ["POS.Remove"] = "Remove",
        ["POS.Payment"] = "Payment",
        ["POS.Subtotal"] = "Subtotal",
        ["POS.Discounts"] = "Discounts",
        ["POS.InvoiceDiscount"] = "Invoice Discount",
        ["POS.Change"] = "Change",
        ["POS.PaymentMethod"] = "Payment Method",
        ["POS.AmountPaid"] = "Amount Paid",
        ["POS.ReceiptCopies"] = "Receipt Copies",
        ["POS.Apply"] = "Apply",
        ["POS.CompleteSale"] = "Complete Sale",
        ["POS.HeldSales"] = "Held Sales",
        ["POS.Resume"] = "Resume",
        ["POS.ReadyToScan"] = "Ready to scan.",
        ["POS.NoCategory"] = "No category",
        ["POS.ScanTitle"] = "Scan",
        ["POS.ScanNotAdded"] = "Barcode {0} was not added.",
        ["POS.ProductNotAdded"] = "{0} was not added.",
        ["POS.AddedProduct"] = "Added {0}.",
        ["POS.AddedToInvoice"] = "Added to invoice {0}; cart lines: {1}.",
        ["POS.CustomerSelected"] = "Customer selected.",
        ["POS.CustomerCreated"] = "Customer created and selected.",
        ["POS.SaleSuspended"] = "Sale suspended.",
        ["POS.EmptySale"] = "A sale must contain at least one item.",
        ["POS.CompleteConfirm"] = "Complete payment and prepare receipt?",
        ["POS.ReceiptPrinted"] = "{0} receipt copy/copies printed successfully for {1}.",
        ["POS.ReceiptFailed"] = "Sale completed, but receipt printing failed. {0}",
        ["POS.OperationFailed"] = "The POS operation could not be completed.",
        ["POS.CartSummary"] = "{0} lines | {1} items | Total {2:N2}",
        ["POS.PaymentReady"] = "Payment ready | Change {0:N2}",
        ["POS.PaymentRemaining"] = "Remaining {0:N2}",
        ["POS.StockHealthy"] = "Stock looks healthy for this cart.",
        ["POS.StockWatch"] = "{0} cart line(s) are near low stock.",
        ["POS.PayExact"] = "Exact",
        ["POS.QuickCash"] = "Quick cash",
        ["POS.DecreaseQty"] = "−",
        ["POS.IncreaseQty"] = "+",
        ["Settings.Title"] = "Settings",
        ["Settings.Subtitle"] = "Centralized store configuration persisted in the bookstore database.",
        ["Settings.Unsaved"] = "Unsaved changes will be lost.",
        ["Settings.Store"] = "Store",
        ["Settings.AppearanceApp"] = "Appearance/App",
        ["Settings.Theme"] = "Theme",
        ["Settings.Language"] = "Language",
        ["Settings.Arabic"] = "Arabic",
        ["Settings.English"] = "English",
        ["Settings.SaveAppearance"] = "Save Appearance",
        ["Settings.Saved"] = "Settings saved.",
        ["Settings.NotSaved"] = "Settings could not be saved.",
        ["Language.SwitchToArabic"] = "العربية",
        ["Language.SwitchToEnglish"] = "English",
        ["Dashboard.Title"] = "Dashboard",
        ["Dashboard.Subtitle"] = "Live operating view for sales, inventory, and bookstore activity.",
        ["Dashboard.LoadFailed"] = "Some dashboard data could not be loaded.",
        ["Dashboard.TodaySales"] = "Today sales",
        ["Dashboard.TodaySalesHint"] = "Gross sales collected today",
        ["Dashboard.Transactions"] = "Transactions",
        ["Dashboard.TransactionsHint"] = "Completed invoices today",
        ["Dashboard.InventoryValue"] = "Inventory value",
        ["Dashboard.InventoryValueHint"] = "Current stock selling value",
        ["Dashboard.PurchaseValue"] = "Purchase value",
        ["Dashboard.PurchaseValueHint"] = "Current stock cost value",
        ["Dashboard.ProfitTodayHint"] = "Estimated gross profit today",
        ["Dashboard.OutOfStock"] = "Out of stock",
        ["Dashboard.OutOfStockHint"] = "Products unavailable for sale",
        ["Dashboard.DiscountsToday"] = "Discounts today",
        ["Dashboard.DiscountsTodayHint"] = "Discounts granted today",
        ["Dashboard.LowStock"] = "Low stock",
        ["Dashboard.LowStockHint"] = "Products needing attention",
        ["Dashboard.BestSeller"] = "Best seller",
        ["Dashboard.TopCustomer"] = "Top customer",
        ["Dashboard.TopCashier"] = "Top cashier",
        ["Dashboard.StockStatus"] = "Stock status",
        ["Dashboard.StockStatusHint"] = "Low stock / out of stock",
        ["Dashboard.ActiveToday"] = "Active today",
        ["Dashboard.NoSalesYet"] = "No sales yet today",
        ["Dashboard.CustomerSignal"] = "Customer activity signal",
        ["Dashboard.CashierSignal"] = "Cashier performance signal",
        ["Dashboard.LastUpdated"] = "Updated",
        ["Dashboard.OperationsPulse"] = "Operations pulse",
        ["Dashboard.InventoryHealth"] = "Inventory health",
        ["Dashboard.SalesMomentum"] = "Sales momentum",
        ["Dashboard.LiveSignals"] = "Live signals",
        ["Dashboard.QuickActions"] = "Quick actions",
        ["Dashboard.OpenPOS"] = "Open POS",
        ["Dashboard.OpenPOSHint"] = "Start scanning and selling products",
        ["Dashboard.Products"] = "Products",
        ["Dashboard.ProductsHint"] = "Manage books, categories, prices, and barcodes",
        ["Dashboard.Inventory"] = "Inventory",
        ["Dashboard.InventoryHint"] = "Review stock levels and movements",
        ["Dashboard.Reports"] = "Reports",
        ["Dashboard.ReportsHint"] = "Open sales and profit analytics",
        ["Dashboard.BackupHealth"] = "Backup health",
        ["Dashboard.BackupHealthHint"] = "Review local backups and database integrity",
        ["Dashboard.Profit"] = "Profit",
        ["Dashboard.TotalProducts"] = "Total products",
        ["Dashboard.NoData"] = "No data",
        ["Dashboard.VsYesterday"] = "vs yesterday",
        ["Dashboard.NewSalesToday"] = "New sales today",
        ["Dashboard.NoSalesCompared"] = "No sales today or yesterday"
    };

    private static readonly IReadOnlyDictionary<string, string> Arabic = new Dictionary<string, string>
    {
        ["App.Title"] = "مكتبة الزعبك",
        ["App.Product"] = "نظام نقاط البيع",
        ["Auth.SignIn"] = "تسجيل الدخول",
        ["Auth.SignInSubtitle"] = "متابعة إلى لوحة تحكم المكتبة",
        ["Auth.Username"] = "اسم المستخدم",
        ["Auth.Password"] = "كلمة المرور",
        ["Auth.ShowPassword"] = "إظهار",
        ["Auth.RememberMe"] = "تذكرني",
        ["Auth.Login"] = "دخول",
        ["Auth.Exit"] = "خروج",
        ["Auth.SigningIn"] = "جار تسجيل الدخول...",
        ["Setup.CreateAdministrator"] = "إنشاء مدير النظام",
        ["Setup.Subtitle"] = "إعداد أول تشغيل لقاعدة بيانات المكتبة",
        ["Setup.FullName"] = "الاسم الكامل",
        ["Setup.Email"] = "البريد الإلكتروني",
        ["Setup.ConfirmPassword"] = "تأكيد كلمة المرور",
        ["Setup.CreateAdmin"] = "إنشاء المدير",
        ["Setup.Creating"] = "جار إنشاء المدير...",
        ["Shell.SearchTooltip"] = "بحث عام",
        ["Shell.Theme"] = "الثيم",
        ["Shell.Notifications"] = "الإشعارات",
        ["Shell.ClearNotifications"] = "مسح",
        ["Shell.NoNotifications"] = "لا توجد إشعارات حالية.",
        ["Shell.Settings"] = "الإعدادات",
        ["Shell.Refresh"] = "تحديث",
        ["Shell.User"] = "المستخدم: ",
        ["Shell.Database"] = "   قاعدة البيانات: ",
        ["Shell.Internet"] = "   الإنترنت: ",
        ["Shell.Tasks"] = "   المهام: ",
        ["Status.Connected"] = "متصل",
        ["Status.Offline"] = "غير متصل",
        ["Auth.PermissionDenied"] = "لا تملك صلاحية فتح هذه المنطقة.",
        ["Loading.Default"] = "جار التحميل...",
        ["Loading.OpeningPage"] = "جار فتح الصفحة...",
        ["Nav.Dashboard"] = "لوحة التحكم",
        ["Nav.Categories"] = "التصنيفات",
        ["Nav.Products"] = "المنتجات",
        ["Nav.Inventory"] = "المخزون",
        ["Nav.Barcode"] = "الباركود",
        ["Nav.SalesPOS"] = "المبيعات",
        ["Nav.Customers"] = "العملاء",
        ["Nav.Suppliers"] = "الموردون",
        ["Nav.Reports"] = "التقارير",
        ["Nav.Receipts"] = "الإيصالات",
        ["Nav.Settings"] = "الإعدادات",
        ["Nav.Users"] = "المستخدمون",
        ["Nav.Roles"] = "الأدوار",
        ["Nav.Backup"] = "النسخ الاحتياطي",
        ["Nav.Logout"] = "تسجيل الخروج",
        ["POS.Cashier"] = "كاشير المبيعات",
        ["POS.Title"] = "نقطة البيع",
        ["POS.NewSale"] = "عملية جديدة",
        ["POS.Suspend"] = "تعليق",
        ["POS.Cancel"] = "إلغاء",
        ["POS.CancelTitle"] = "إلغاء البيع",
        ["POS.CancelConfirm"] = "هل تريد إلغاء عملية البيع الحالية؟",
        ["POS.Scan"] = "مسح الكود",
        ["POS.Add"] = "إضافة",
        ["POS.AutoAdd"] = "إضافة تلقائية عند المسح",
        ["POS.Category"] = "التصنيف: ",
        ["POS.Search"] = "بحث",
        ["POS.Find"] = "بحث",
        ["POS.Customer"] = "العميل",
        ["POS.Select"] = "اختيار",
        ["POS.WalkIn"] = "عميل مباشر",
        ["POS.CreateCustomer"] = "إنشاء عميل",
        ["POS.SelectedQuantity"] = "الكمية المحددة",
        ["POS.AddProduct"] = "إضافة المنتج",
        ["POS.Product"] = "المنتج",
        ["POS.Barcode"] = "الباركود",
        ["POS.Qty"] = "الكمية",
        ["POS.StockLeft"] = "المتبقي",
        ["POS.Price"] = "السعر",
        ["POS.Discount"] = "الخصم",
        ["POS.Tax"] = "الضريبة",
        ["POS.Total"] = "الإجمالي",
        ["POS.UpdateQty"] = "تحديث الكمية",
        ["POS.LineDiscount"] = "خصم الصنف",
        ["POS.Remove"] = "حذف",
        ["POS.Payment"] = "الدفع",
        ["POS.Subtotal"] = "المجموع",
        ["POS.Discounts"] = "الخصومات",
        ["POS.InvoiceDiscount"] = "خصم الفاتورة",
        ["POS.Change"] = "الباقي",
        ["POS.PaymentMethod"] = "طريقة الدفع",
        ["POS.AmountPaid"] = "المبلغ المدفوع",
        ["POS.ReceiptCopies"] = "نسخ الإيصال",
        ["POS.Apply"] = "تطبيق",
        ["POS.CompleteSale"] = "إتمام البيع",
        ["POS.HeldSales"] = "عمليات معلقة",
        ["POS.Resume"] = "استكمال",
        ["POS.ReadyToScan"] = "جاهز لمسح الكود.",
        ["POS.NoCategory"] = "بدون تصنيف",
        ["POS.ScanTitle"] = "المسح",
        ["POS.ScanNotAdded"] = "لم تتم إضافة الباركود {0}.",
        ["POS.ProductNotAdded"] = "لم تتم إضافة {0}.",
        ["POS.AddedProduct"] = "تمت إضافة {0}.",
        ["POS.AddedToInvoice"] = "تمت الإضافة إلى الفاتورة {0}؛ عدد السطور: {1}.",
        ["POS.CustomerSelected"] = "تم اختيار العميل.",
        ["POS.CustomerCreated"] = "تم إنشاء العميل واختياره.",
        ["POS.SaleSuspended"] = "تم تعليق العملية.",
        ["POS.EmptySale"] = "يجب أن تحتوي العملية على صنف واحد على الأقل.",
        ["POS.CompleteConfirm"] = "هل تريد إتمام الدفع وتجهيز الإيصال؟",
        ["POS.ReceiptPrinted"] = "تمت طباعة {0} نسخة إيصال للفاتورة {1}.",
        ["POS.ReceiptFailed"] = "تم إتمام البيع لكن فشلت طباعة الإيصال. {0}",
        ["POS.OperationFailed"] = "تعذر إتمام عملية نقطة البيع.",
        ["POS.CartSummary"] = "{0} سطور | {1} أصناف | الإجمالي {2:N2}",
        ["POS.PaymentReady"] = "الدفع جاهز | الباقي {0:N2}",
        ["POS.PaymentRemaining"] = "المتبقي {0:N2}",
        ["POS.StockHealthy"] = "المخزون مناسب لهذه العملية.",
        ["POS.StockWatch"] = "{0} سطر/سطور في السلة قريب من نفاد المخزون.",
        ["POS.PayExact"] = "بالضبط",
        ["POS.QuickCash"] = "دفع سريع",
        ["POS.DecreaseQty"] = "−",
        ["POS.IncreaseQty"] = "+",
        ["Settings.Title"] = "الإعدادات",
        ["Settings.Subtitle"] = "إدارة إعدادات المتجر المحفوظة في قاعدة بيانات المكتبة.",
        ["Settings.Unsaved"] = "سيتم فقد التغييرات غير المحفوظة.",
        ["Settings.Store"] = "المتجر",
        ["Settings.AppearanceApp"] = "المظهر والتطبيق",
        ["Settings.Theme"] = "الثيم",
        ["Settings.Language"] = "اللغة",
        ["Settings.Arabic"] = "العربية",
        ["Settings.English"] = "الإنجليزية",
        ["Settings.SaveAppearance"] = "حفظ المظهر",
        ["Settings.Saved"] = "تم حفظ الإعدادات.",
        ["Settings.NotSaved"] = "تعذر حفظ الإعدادات.",
        ["Language.SwitchToArabic"] = "العربية",
        ["Language.SwitchToEnglish"] = "English",
        ["Dashboard.Title"] = "لوحة التحكم",
        ["Dashboard.Subtitle"] = "متابعة مباشرة للمبيعات والمخزون ونشاط المكتبة.",
        ["Dashboard.LoadFailed"] = "تعذر تحميل بعض بيانات لوحة التحكم.",
        ["Dashboard.TodaySales"] = "مبيعات اليوم",
        ["Dashboard.TodaySalesHint"] = "إجمالي المبيعات المحصلة اليوم",
        ["Dashboard.Transactions"] = "الفواتير",
        ["Dashboard.TransactionsHint"] = "الفواتير المكتملة اليوم",
        ["Dashboard.InventoryValue"] = "قيمة المخزون",
        ["Dashboard.InventoryValueHint"] = "قيمة بيع المخزون الحالي",
        ["Dashboard.PurchaseValue"] = "قيمة الشراء",
        ["Dashboard.PurchaseValueHint"] = "تكلفة المخزون الحالية",
        ["Dashboard.ProfitTodayHint"] = "إجمالي الربح التقديري اليوم",
        ["Dashboard.OutOfStock"] = "نفد من المخزون",
        ["Dashboard.OutOfStockHint"] = "منتجات غير متاحة للبيع",
        ["Dashboard.DiscountsToday"] = "خصومات اليوم",
        ["Dashboard.DiscountsTodayHint"] = "إجمالي الخصومات الممنوحة اليوم",
        ["Dashboard.LowStock"] = "مخزون منخفض",
        ["Dashboard.LowStockHint"] = "منتجات تحتاج متابعة",
        ["Dashboard.BestSeller"] = "الأكثر مبيعا",
        ["Dashboard.TopCustomer"] = "أفضل عميل",
        ["Dashboard.TopCashier"] = "أفضل كاشير",
        ["Dashboard.StockStatus"] = "حالة المخزون",
        ["Dashboard.StockStatusHint"] = "منخفض / نفد من المخزون",
        ["Dashboard.ActiveToday"] = "نشط اليوم",
        ["Dashboard.NoSalesYet"] = "لا توجد مبيعات اليوم",
        ["Dashboard.CustomerSignal"] = "مؤشر نشاط العملاء",
        ["Dashboard.CashierSignal"] = "مؤشر أداء الكاشير",
        ["Dashboard.LastUpdated"] = "آخر تحديث",
        ["Dashboard.OperationsPulse"] = "نبض التشغيل",
        ["Dashboard.InventoryHealth"] = "صحة المخزون",
        ["Dashboard.SalesMomentum"] = "زخم المبيعات",
        ["Dashboard.LiveSignals"] = "مؤشرات مباشرة",
        ["Dashboard.QuickActions"] = "إجراءات سريعة",
        ["Dashboard.OpenPOS"] = "فتح الكاشير",
        ["Dashboard.OpenPOSHint"] = "ابدأ المسح والبيع مباشرة",
        ["Dashboard.Products"] = "المنتجات",
        ["Dashboard.ProductsHint"] = "إدارة الكتب والتصنيفات والأسعار والباركود",
        ["Dashboard.Inventory"] = "المخزون",
        ["Dashboard.InventoryHint"] = "مراجعة مستويات وحركات المخزون",
        ["Dashboard.Reports"] = "التقارير",
        ["Dashboard.ReportsHint"] = "فتح تحليلات المبيعات والأرباح",
        ["Dashboard.BackupHealth"] = "صحة النسخ الاحتياطي",
        ["Dashboard.BackupHealthHint"] = "مراجعة النسخ المحلية وسلامة قاعدة البيانات",
        ["Dashboard.Profit"] = "الربح",
        ["Dashboard.TotalProducts"] = "إجمالي المنتجات",
        ["Dashboard.NoData"] = "لا توجد بيانات",
        ["Dashboard.VsYesterday"] = "مقارنة بأمس",
        ["Dashboard.NewSalesToday"] = "مبيعات جديدة اليوم",
        ["Dashboard.NoSalesCompared"] = "لا توجد مبيعات اليوم أو أمس"
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LocalizationService> _logger;
    private IReadOnlyDictionary<string, string> _strings = English;

    public LocalizationService(IServiceScopeFactory scopeFactory, ISettingsChangedNotifier notifier, ILogger<LocalizationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        notifier.SettingsChanged += OnSettingsChanged;
    }

    public event EventHandler? CultureChanged;

    public string CurrentLanguage { get; private set; } = "en-US";

    public bool IsRightToLeft => CurrentLanguage.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    public FlowDirection FlowDirection => IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public async Task ApplyConfiguredCultureAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var appearance = await settingsService.GetAsync<AppearanceSettingsDto>();
        ApplyCulture(appearance.Language);
    }

    public void ApplyCulture(string language)
    {
        var normalized = NormalizeLanguage(language);
        CurrentLanguage = normalized;
        _strings = normalized.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? Arabic : English;

        var culture = CultureInfo.GetCultureInfo(normalized);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        var resources = System.Windows.Application.Current.Resources;
        resources["AppFlowDirection"] = FlowDirection;
        resources["AppTextAlignment"] = IsRightToLeft ? TextAlignment.Right : TextAlignment.Left;
        foreach (var pair in English)
        {
            resources[pair.Key] = T(pair.Key);
        }

        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            window.Language = System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag);
        }

        _logger.LogInformation("UI culture applied: {Language}", CurrentLanguage);
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ToggleLanguageAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var appearance = await settingsService.GetAsync<AppearanceSettingsDto>();
        appearance.Language = IsRightToLeft ? "en-US" : "ar-EG";

        var result = await settingsService.SetAsync(appearance);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error ?? "Language could not be changed.");
        }
    }

    public string T(string key) => _strings.TryGetValue(key, out var value) ? value : key;

    private static string NormalizeLanguage(string? language)
    {
        return string.IsNullOrWhiteSpace(language) || language.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
            ? "ar-EG"
            : "en-US";
    }

    private async void OnSettingsChanged(object? sender, SettingsChangedEvent e)
    {
        if (e.Category == SettingsCategory.Appearance)
        {
            await ApplyConfiguredCultureAsync();
        }
    }
}

