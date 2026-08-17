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
        ["Nav.Audit"] = "Audit Trail",
        ["Nav.DataQuality"] = "Data Quality",
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
        ["Dashboard.NoSalesCompared"] = "No sales today or yesterday",
        ["DataQuality.Issues"] = "Issues: ",
        ["DataQuality.MissingSupplier"] = "Missing supplier: ",
        ["DataQuality.InvalidPrice"] = "Invalid price: ",
        ["DataQuality.NegativeStock"] = "Negative stock: "
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
        ["Nav.Audit"] = "سجل التدقيق",
        ["Nav.DataQuality"] = "جودة البيانات",
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
        ["Dashboard.NoSalesCompared"] = "لا توجد مبيعات اليوم أو أمس",
        ["DataQuality.Issues"] = "المشكلات: ",
        ["DataQuality.MissingSupplier"] = "مورد غير محدد: ",
        ["DataQuality.InvalidPrice"] = "سعر غير صالح: ",
        ["DataQuality.NegativeStock"] = "مخزون سالب: "
    };

    private static readonly IReadOnlyDictionary<string, string> ExtraArabic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["BookStore POS"] = "نظام نقاط البيع",
        ["EL ZEABAK BookStore"] = "مكتبة الزعبك",
        ["Administrator"] = "مدير النظام",
        ["Search"] = "بحث",
        ["Refresh"] = "تحديث",
        ["Add"] = "إضافة",
        ["Edit"] = "تعديل",
        ["Delete"] = "حذف",
        ["Save"] = "حفظ",
        ["Cancel"] = "إلغاء",
        ["Close"] = "إغلاق",
        ["Back"] = "رجوع",
        ["Next"] = "التالي",
        ["Import"] = "استيراد",
        ["Export"] = "تصدير",
        ["Print"] = "طباعة",
        ["Validate"] = "تحقق",
        ["Restore"] = "استعادة",
        ["Database"] = "قاعدة البيانات",
        ["Printer"] = "الطابعة",
        ["Auto"] = "تلقائي",
        ["Cleanup"] = "تنظيف",
        ["Backup Now"] = "نسخ احتياطي الآن",
        ["Open Folder"] = "فتح المجلد",
        ["Run Checks"] = "تشغيل الفحص",
        ["Run Integrity"] = "فحص السلامة",
        ["New"] = "جديد",
        ["Details"] = "التفاصيل",
        ["Create"] = "إنشاء",
        ["Activate"] = "تفعيل",
        ["Deactivate"] = "تعطيل",
        ["Adjust"] = "تسوية",
        ["Remove"] = "حذف",
        ["Reset"] = "إعادة ضبط",
        ["Generate"] = "إنشاء",
        ["Preview"] = "معاينة",
        ["Select"] = "اختيار",
        ["View Details"] = "عرض التفاصيل",
        ["Test Print"] = "اختبار الطباعة",
        ["Reprint"] = "إعادة طباعة",
        ["New Category"] = "تصنيف جديد",
        ["Edit Category"] = "تعديل التصنيف",
        ["Category Details"] = "تفاصيل التصنيف",
        ["New Product"] = "منتج جديد",
        ["Edit Product"] = "تعديل المنتج",
        ["Product Details"] = "تفاصيل المنتج",
        ["New Customer"] = "عميل جديد",
        ["Edit Customer"] = "تعديل العميل",
        ["Customer Details"] = "تفاصيل العميل",
        ["New Supplier"] = "مورد جديد",
        ["Edit Supplier"] = "تعديل المورد",
        ["Supplier Details"] = "تفاصيل المورد",
        ["Change Password"] = "تغيير كلمة المرور",
        ["Barcode Preview"] = "معاينة الباركود",
        ["Barcode Settings"] = "إعدادات الباركود",
        ["Barcode Label Printing"] = "طباعة ملصقات الباركود",
        ["Print Labels"] = "طباعة الملصقات",
        ["Sales POS"] = "نقطة البيع",
        ["Stock Adjustment"] = "تسوية المخزون",
        ["Inventory Dashboard"] = "لوحة المخزون",
        ["Inventory History"] = "سجل المخزون",
        ["Receipt Preview"] = "معاينة الإيصال",
        ["Reprint Receipt"] = "إعادة طباعة الإيصال",
        ["Printer Test"] = "اختبار الطابعة",
        ["Reports Dashboard"] = "لوحة التقارير",
        ["Sales Summary"] = "ملخص المبيعات",
        ["Sales Details"] = "تفاصيل المبيعات",
        ["Profit Report"] = "تقرير الربح",
        ["Best-Selling Products"] = "المنتجات الأكثر مبيعا",
        ["Inventory Report"] = "تقرير المخزون",
        ["Inventory Movements"] = "حركات المخزون",
        ["Low Stock"] = "مخزون منخفض",
        ["Customer Report"] = "تقرير العملاء",
        ["Cashier Performance"] = "أداء الكاشير",
        ["Payment Methods"] = "طرق الدفع",
        ["Daily Sales"] = "المبيعات اليومية",
        ["Hourly Sales"] = "المبيعات بالساعة",
        ["Backup Details"] = "تفاصيل النسخة الاحتياطية",
        ["Restore Backup"] = "استعادة نسخة احتياطية",
        ["Database Health"] = "صحة قاعدة البيانات",
        ["Audit Trail"] = "سجل التدقيق",
        ["Data Quality"] = "جودة البيانات",
        ["Sales"] = "المبيعات",
        ["Payments"] = "المدفوعات",
        ["Payment"] = "الدفع",
        ["Checkout"] = "إتمام البيع",
        ["Invoice"] = "الفاتورة",
        ["Receipt"] = "الإيصال",
        ["Stock"] = "المخزون",
        ["Ledger"] = "السجل",
        ["Movements"] = "الحركات",
        ["Health"] = "الصحة",
        ["Integrity"] = "السلامة",
        ["Validation"] = "التحقق",
        ["Profit"] = "الربح",
        ["Selling"] = "البيع",
        ["Purchase"] = "الشراء",
        ["Online"] = "متصل",
        ["Ready"] = "جاهز",
        ["Duplicate"] = "مكرر",
        ["Visible"] = "ظاهر",
        ["Collapsed"] = "مطوي",
        ["True"] = "نعم",
        ["False"] = "لا",
        ["Light"] = "فاتح",
        ["Dark"] = "داكن",
        ["Products"] = "المنتجات",
        ["Categories"] = "التصنيفات",
        ["Customers"] = "العملاء",
        ["Suppliers"] = "الموردون",
        ["Inventory"] = "المخزون",
        ["Reports"] = "التقارير",
        ["Receipts"] = "الإيصالات",
        ["Users"] = "المستخدمون",
        ["Roles"] = "الأدوار",
        ["Backup"] = "النسخ الاحتياطي",
        ["Settings"] = "الإعدادات",
        ["Manage bookstore products and stock-ready metadata."] = "إدارة منتجات المكتبة وبياناتها الجاهزة للمخزون.",
        ["Manage product category classification."] = "إدارة تصنيفات المنتجات.",
        ["Manage customer contact information and sales history."] = "إدارة بيانات تواصل العملاء وسجل المبيعات.",
        ["Manage supplier master data and product associations."] = "إدارة بيانات الموردين وربط المنتجات.",
        ["Create or update reusable category metadata."] = "إنشاء أو تحديث بيانات التصنيف القابلة لإعادة الاستخدام.",
        ["Maintain customer identity and contact details."] = "حافظ على بيانات هوية العميل وتفاصيل التواصل.",
        ["Maintain supplier company, contact, address, and notes."] = "حافظ على بيانات شركة المورد والتواصل والعنوان والملاحظات.",
        ["Every cashier session starts from a protected workspace."] = "كل جلسة كاشير تبدأ من مساحة عمل محمية.",
        ["Scan items, find prices, and prepare totals instantly."] = "امسح الأصناف واعرض الأسعار وجهز الإجماليات فورا.",
        ["Stock is checked before checkout and reflected after sale."] = "يتم فحص المخزون قبل إتمام البيع وتحديثه بعد العملية.",
        ["No Products"] = "لا توجد منتجات",
        ["No products found."] = "لم يتم العثور على منتجات.",
        ["No products found"] = "لم يتم العثور على منتجات",
        ["No Categories"] = "لا توجد تصنيفات",
        ["No categories found."] = "لم يتم العثور على تصنيفات.",
        ["No categories matched your search."] = "لا توجد تصنيفات مطابقة للبحث.",
        ["No Customers"] = "لا يوجد عملاء",
        ["No customers found."] = "لم يتم العثور على عملاء.",
        ["No Suppliers"] = "لا يوجد موردون",
        ["No suppliers found."] = "لم يتم العثور على موردين.",
        ["No records found."] = "لم يتم العثور على سجلات.",
        ["Loading products..."] = "جار تحميل المنتجات...",
        ["Loading product..."] = "جار تحميل المنتج...",
        ["Loading categories..."] = "جار تحميل التصنيفات...",
        ["Loading category..."] = "جار تحميل التصنيف...",
        ["Loading customers..."] = "جار تحميل العملاء...",
        ["Loading suppliers..."] = "جار تحميل الموردين...",
        ["Loading inventory..."] = "جار تحميل المخزون...",
        ["Loading inventory dashboard..."] = "جار تحميل لوحة المخزون...",
        ["Loading stock ledger..."] = "جار تحميل سجل المخزون...",
        ["Loading report..."] = "جار تحميل التقرير...",
        ["Loading reports..."] = "جار تحميل التقارير...",
        ["Saving product..."] = "جار حفظ المنتج...",
        ["Saving category..."] = "جار حفظ التصنيف...",
        ["Checking database..."] = "جار فحص قاعدة البيانات...",
        ["Restoring database..."] = "جار استعادة قاعدة البيانات...",
        ["Working on backup operation..."] = "جار تنفيذ عملية النسخ الاحتياطي...",
        ["Product not found"] = "لم يتم العثور على المنتج",
        ["All Status"] = "كل الحالات",
        ["All Categories"] = "كل التصنيفات",
        ["Active"] = "نشط",
        ["Inactive"] = "غير نشط",
        ["Out of Stock"] = "نفد من المخزون",
        ["Min price"] = "أقل سعر",
        ["Max price"] = "أعلى سعر",
        ["Min qty"] = "أقل كمية",
        ["Max qty"] = "أعلى كمية",
        ["Category name"] = "اسم التصنيف",
        ["Optional description"] = "وصف اختياري",
        ["Cover Image"] = "صورة الغلاف",
        ["Barcode"] = "الباركود",
        ["Barcode Value"] = "قيمة الباركود",
        ["Barcode Format"] = "تنسيق الباركود",
        ["Barcode/Inventory"] = "الباركود والمخزون",
        ["Generate Barcode"] = "إنشاء باركود",
        ["Save Barcode"] = "حفظ الباركود",
        ["Show barcode"] = "إظهار الباركود",
        ["Print QR code"] = "طباعة رمز QR",
        ["ISBN"] = "الرقم الدولي",
        ["Title"] = "العنوان",
        ["Subtitle"] = "العنوان الفرعي",
        ["Description"] = "الوصف",
        ["Author"] = "المؤلف",
        ["Publisher"] = "الناشر",
        ["Edition"] = "الطبعة",
        ["Publish Date"] = "تاريخ النشر",
        ["Category"] = "التصنيف",
        ["Selling Price"] = "سعر البيع",
        ["Selling Value"] = "قيمة البيع",
        ["Purchase Price"] = "سعر الشراء",
        ["Purchase Value"] = "قيمة الشراء",
        ["Current Quantity"] = "الكمية الحالية",
        ["Minimum Stock"] = "حد المخزون الأدنى",
        ["Low Stock Threshold"] = "حد انخفاض المخزون",
        ["Shelf"] = "الرف",
        ["Shelf Location"] = "موقع الرف",
        ["Location"] = "الموقع",
        ["Quantity"] = "الكمية",
        ["Quantity Before"] = "الكمية قبل",
        ["Quantity After"] = "الكمية بعد",
        ["Quantity Change"] = "تغير الكمية",
        ["Inventory Status"] = "حالة المخزون",
        ["Inventory Value"] = "قيمة المخزون",
        ["Total Stock"] = "إجمالي المخزون",
        ["Stock Ledger"] = "سجل المخزون",
        ["Adjustment Type"] = "نوع التسوية",
        ["Transaction Type"] = "نوع الحركة",
        ["Reason"] = "السبب",
        ["Status"] = "الحالة",
        ["Name"] = "الاسم",
        ["Full Name"] = "الاسم الكامل",
        ["Customer Name"] = "اسم العميل",
        ["Product Title"] = "عنوان المنتج",
        ["Product"] = "المنتج",
        ["Product Count"] = "عدد المنتجات",
        ["Product Image"] = "صورة المنتج",
        ["Product Lookup"] = "بحث المنتجات",
        ["Associated Products"] = "المنتجات المرتبطة",
        ["Best Product"] = "أفضل منتج",
        ["Best Products"] = "أفضل المنتجات",
        ["Total Products"] = "إجمالي المنتجات",
        ["Email"] = "البريد الإلكتروني",
        ["Phone"] = "الهاتف",
        ["Address"] = "العنوان",
        ["Contact"] = "التواصل",
        ["Contact Name"] = "اسم جهة التواصل",
        ["Contact Person"] = "مسؤول التواصل",
        ["Products Count"] = "عدد المنتجات",
        ["Loyalty Points"] = "نقاط الولاء",
        ["Created"] = "تاريخ الإنشاء",
        ["Created Date"] = "تاريخ الإنشاء",
        ["Created By"] = "أنشئ بواسطة",
        ["Updated"] = "تاريخ التحديث",
        ["Last Updated"] = "آخر تحديث",
        ["Last Purchase"] = "آخر شراء",
        ["Date"] = "التاريخ",
        ["Time"] = "الوقت",
        ["Area"] = "المنطقة",
        ["Action"] = "الإجراء",
        ["Outcome"] = "النتيجة",
        ["User"] = "المستخدم",
        ["Entity"] = "الكيان",
        ["Detail"] = "التفاصيل",
        ["Severity"] = "الخطورة",
        ["Issue"] = "المشكلة",
        ["Item"] = "العنصر",
        ["Recommendation"] = "التوصية",
        ["File"] = "الملف",
        ["Path"] = "المسار",
        ["Size"] = "الحجم",
        ["Disk Bytes"] = "حجم القرص بالبايت",
        ["SHA-256"] = "بصمة SHA-256",
        ["Application Version"] = "إصدار التطبيق",
        ["Database Version"] = "إصدار قاعدة البيانات",
        ["Schema Version"] = "إصدار المخطط",
        ["Connection Type"] = "نوع الاتصال",
        ["Local database"] = "قاعدة بيانات محلية",
        ["Type"] = "النوع",
        ["Total"] = "الإجمالي",
        ["Total Purchases"] = "إجمالي المشتريات",
        ["Transactions"] = "المعاملات",
        ["Today's Sales"] = "مبيعات اليوم",
        ["Sales Count"] = "عدد المبيعات",
        ["Sales History"] = "سجل المبيعات",
        ["Sale Date"] = "تاريخ البيع",
        ["Cashier"] = "الكاشير",
        ["Cashiers"] = "الكاشيرون",
        ["Top Cashier"] = "أفضل كاشير",
        ["Top Customer"] = "أفضل عميل",
        ["Walk-in"] = "عميل مباشر",
        ["Discount"] = "الخصم",
        ["Tax"] = "الضريبة",
        ["Tax/Currency"] = "الضريبة والعملة",
        ["Tax enabled"] = "الضريبة مفعلة",
        ["Tax included in price"] = "الضريبة ضمن السعر",
        ["Tax Number"] = "الرقم الضريبي",
        ["Tax Category"] = "تصنيف الضريبة",
        ["Default Rate"] = "النسبة الافتراضية",
        ["Currency Code"] = "رمز العملة",
        ["Currency Symbol"] = "رمز العملة",
        ["Decimal Places"] = "المنازل العشرية",
        ["Default Type"] = "النوع الافتراضي",
        ["Page"] = "صفحة",
        ["Page "] = "صفحة ",
        ["Page size"] = "حجم الصفحة",
        ["Page size "] = "حجم الصفحة ",
        ["Pagination prepared"] = "تم تجهيز الترقيم",
        ["Pagination controls prepared for future enhancement."] = "تم تجهيز عناصر الترقيم للتحسين لاحقا.",
        ["Pagination controls prepared for future enhancement"] = "تم تجهيز عناصر الترقيم للتحسين لاحقا",
        ["Product rows "] = "صفوف المنتجات ",
        ["Sales rows "] = "صفوف المبيعات ",
        ["Customer created "] = "تم إنشاء العميل ",
        ["Supplier created "] = "تم إنشاء المورد ",
        ["Qty "] = "الكمية ",
        ["Total "] = "الإجمالي ",
        ["Updated "] = "آخر تحديث ",
        ["Product export is prepared for future implementation."] = "تم تجهيز تصدير المنتجات للتنفيذ لاحقا.",
        ["Product import is prepared for future implementation."] = "تم تجهيز استيراد المنتجات للتنفيذ لاحقا.",
        ["Operation failed."] = "فشلت العملية.",
        ["Unable to load products."] = "تعذر تحميل المنتجات.",
        ["Unable to save product."] = "تعذر حفظ المنتج.",
        ["Unable to duplicate product."] = "تعذر تكرار المنتج.",
        ["Product created successfully."] = "تم إنشاء المنتج بنجاح.",
        ["Product updated successfully."] = "تم تحديث المنتج بنجاح.",
        ["Product deleted successfully."] = "تم حذف المنتج بنجاح.",
        ["Product activated successfully."] = "تم تفعيل المنتج بنجاح.",
        ["Product deactivated successfully."] = "تم تعطيل المنتج بنجاح.",
        ["Product duplicated successfully."] = "تم تكرار المنتج بنجاح.",
        ["Product found."] = "تم العثور على المنتج.",
        ["Product was not found."] = "لم يتم العثور على المنتج.",
        ["Save the product before duplicating it."] = "احفظ المنتج قبل تكراره.",
        ["Delete Product"] = "حذف المنتج",
        ["Unable to load categories."] = "تعذر تحميل التصنيفات.",
        ["Unable to save category."] = "تعذر حفظ التصنيف.",
        ["Unable to delete category."] = "تعذر حذف التصنيف.",
        ["Category created successfully."] = "تم إنشاء التصنيف بنجاح.",
        ["Category updated successfully."] = "تم تحديث التصنيف بنجاح.",
        ["Category deleted successfully."] = "تم حذف التصنيف بنجاح.",
        ["Category activated successfully."] = "تم تفعيل التصنيف بنجاح.",
        ["Category deactivated successfully."] = "تم تعطيل التصنيف بنجاح.",
        ["Category was not found."] = "لم يتم العثور على التصنيف.",
        ["No category selected."] = "لم يتم اختيار تصنيف.",
        ["Delete Category"] = "حذف التصنيف",
        ["Unable to load customers."] = "تعذر تحميل العملاء.",
        ["Unable to save customer."] = "تعذر حفظ العميل.",
        ["Customer could not be found."] = "تعذر العثور على العميل.",
        ["Customer created successfully."] = "تم إنشاء العميل بنجاح.",
        ["Customer updated successfully."] = "تم تحديث العميل بنجاح.",
        ["Customer deleted successfully."] = "تم حذف العميل بنجاح.",
        ["Customer activated successfully."] = "تم تفعيل العميل بنجاح.",
        ["Customer deactivated successfully."] = "تم تعطيل العميل بنجاح.",
        ["Customer search failed."] = "فشل البحث عن العملاء.",
        ["No customer selected."] = "لم يتم اختيار عميل.",
        ["Delete Customer"] = "حذف العميل",
        ["Unable to load suppliers."] = "تعذر تحميل الموردين.",
        ["Unable to save supplier."] = "تعذر حفظ المورد.",
        ["Supplier could not be found."] = "تعذر العثور على المورد.",
        ["Supplier created successfully."] = "تم إنشاء المورد بنجاح.",
        ["Supplier updated successfully."] = "تم تحديث المورد بنجاح.",
        ["Supplier deleted successfully."] = "تم حذف المورد بنجاح.",
        ["Supplier activated successfully."] = "تم تفعيل المورد بنجاح.",
        ["Supplier deactivated successfully."] = "تم تعطيل المورد بنجاح.",
        ["No supplier selected."] = "لم يتم اختيار مورد.",
        ["Delete Supplier"] = "حذف المورد",
        ["Products will be preserved."] = "سيتم الاحتفاظ بالمنتجات.",
        ["Unable to load inventory."] = "تعذر تحميل المخزون.",
        ["Unable to load stock ledger."] = "تعذر تحميل سجل المخزون.",
        ["Adjustment saved."] = "تم حفظ التسوية.",
        ["Unable to load report."] = "تعذر تحميل التقرير.",
        ["Unable to load reports dashboard."] = "تعذر تحميل لوحة التقارير.",
        ["No rows found for the selected filters."] = "لا توجد صفوف للفلاتر المحددة.",
        ["Unable to load audit trail."] = "تعذر تحميل سجل التدقيق.",
        ["Unable to run data quality checks."] = "تعذر تشغيل فحوصات جودة البيانات.",
        ["Unable to load backups."] = "تعذر تحميل النسخ الاحتياطية.",
        ["Unable to create backup."] = "تعذر إنشاء النسخة الاحتياطية.",
        ["Unable to validate backup."] = "تعذر التحقق من النسخة الاحتياطية.",
        ["Unable to delete backup."] = "تعذر حذف النسخة الاحتياطية.",
        ["Unable to clean up backups."] = "تعذر تنظيف النسخ الاحتياطية.",
        ["Unable to restore backup."] = "تعذر استعادة النسخة الاحتياطية.",
        ["Backup folder was not found."] = "لم يتم العثور على مجلد النسخ الاحتياطي.",
        ["Delete Backup"] = "حذف النسخة الاحتياطية",
        ["Delete the selected backup file and metadata?"] = "هل تريد حذف ملف النسخة الاحتياطية وبياناته؟",
        ["Restoring will replace the current database. Continue?"] = "ستستبدل الاستعادة قاعدة البيانات الحالية. هل تريد المتابعة؟",
        ["Unable to load database health."] = "تعذر تحميل حالة قاعدة البيانات.",
        ["Integrity check failed."] = "فشل فحص السلامة.",
        ["Unable to generate barcode."] = "تعذر إنشاء الباركود.",
        ["Unable to print barcode."] = "تعذر طباعة الباركود.",
        ["Invalid barcode."] = "باركود غير صالح.",
        ["Barcode generated successfully."] = "تم إنشاء الباركود بنجاح.",
        ["Barcode printed."] = "تم طباعة الباركود.",
        ["Barcode settings are loaded from appsettings and database settings."] = "يتم تحميل إعدادات الباركود من إعدادات التطبيق وقاعدة البيانات.",
        ["Unable to load receipt preview."] = "تعذر تحميل معاينة الإيصال.",
        ["Unable to print receipt."] = "تعذر طباعة الإيصال.",
        ["Unable to search receipts."] = "تعذر البحث في الإيصالات.",
        ["Unable to reprint receipt."] = "تعذر إعادة طباعة الإيصال.",
        ["Receipt printed successfully."] = "تمت طباعة الإيصال بنجاح.",
        ["Receipt reprinted successfully."] = "تمت إعادة طباعة الإيصال بنجاح.",
        ["Printer test completed successfully."] = "اكتمل اختبار الطابعة بنجاح.",
        ["Printer test failed."] = "فشل اختبار الطابعة.",
        ["Login failed."] = "فشل تسجيل الدخول.",
        ["Administrator could not be created."] = "تعذر إنشاء مدير النظام.",
        ["Allow discounts"] = "السماح بالخصومات",
        ["Allow negative stock"] = "السماح بالمخزون السالب",
        ["Allow price override"] = "السماح بتعديل السعر",
        ["Auto inventory adjustment"] = "تسوية تلقائية للمخزون",
        ["Auto-focus barcode input"] = "تركيز تلقائي على حقل الباركود",
        ["Auto-generate barcodes"] = "إنشاء الباركود تلقائيا",
        ["Automatic Generation"] = "إنشاء تلقائي",
        ["Manual Generation"] = "إنشاء يدوي",
        ["Automatic receipt printing"] = "طباعة الإيصال تلقائيا",
        ["Require customer for credit sales"] = "اشتراط عميل لمبيعات الآجل",
        ["Start new sale after checkout"] = "بدء عملية جديدة بعد إتمام البيع",
        ["Create pre-restore backup"] = "إنشاء نسخة احتياطية قبل الاستعادة",
        ["Validate after backup"] = "التحقق بعد النسخ الاحتياطي",
        ["Backup enabled"] = "النسخ الاحتياطي مفعل",
        ["Backup/Security"] = "النسخ الاحتياطي والأمان",
        ["Save Backup"] = "حفظ النسخ الاحتياطي",
        ["Save Currency"] = "حفظ العملة",
        ["Save Inventory"] = "حفظ المخزون",
        ["Save Security"] = "حفظ الأمان",
        ["Save Tax"] = "حفظ الضريبة",
        ["Password Policy"] = "سياسة كلمة المرور",
        ["Current Password"] = "كلمة المرور الحالية",
        ["New Password"] = "كلمة المرور الجديدة",
        ["Confirm Password"] = "تأكيد كلمة المرور",
        ["Max Login Attempts"] = "الحد الأقصى لمحاولات الدخول",
        ["Lockout Duration"] = "مدة القفل",
        ["Session Timeout"] = "مهلة الجلسة",
        ["Minimum margin %"] = "أقل هامش %",
        ["Retention Count"] = "عدد النسخ المحتفظ بها",
        ["Restore confirmation"] = "تأكيد الاستعادة",
        ["I understand that restoring will replace the current database."] = "أفهم أن الاستعادة ستستبدل قاعدة البيانات الحالية.",
        ["Company Name"] = "اسم الشركة",
        ["Store Name"] = "اسم المتجر",
        ["Logo Path"] = "مسار الشعار",
        ["Footer Text"] = "نص التذييل",
        ["Display Mode"] = "وضع العرض",
        ["Language"] = "اللغة",
        ["EL ZEABAK"] = "الزعبك",
        ["POS"] = "نقطة البيع",
        ["Template"] = "القالب",
        ["Format"] = "التنسيق",
        ["Prefix"] = "البادئة",
        ["Length"] = "الطول",
        ["Starting Number"] = "رقم البداية",
        ["Label Print"] = "طباعة الملصقات",
        ["Labels"] = "الملصقات",
        ["Label Width mm"] = "عرض الملصق مم",
        ["Label Height mm"] = "ارتفاع الملصق مم",
        ["Paper Width"] = "عرض الورق",
        ["Copies"] = "النسخ",
        ["Printer Name"] = "اسم الطابعة",
        ["Printer Type"] = "نوع الطابعة",
        ["Print Timeout"] = "مهلة الطباعة",
        ["Print Timeout ms"] = "مهلة الطباعة بالمللي ثانية",
        ["Scan Timeout"] = "مهلة المسح",
        ["Scan Timeout ms"] = "مهلة المسح بالمللي ثانية",
        ["Open cash drawer"] = "فتح درج النقدية",
        ["Cut paper"] = "قص الورق",
        ["Show cashier"] = "إظهار الكاشير",
        ["Show customer"] = "إظهار العميل",
        ["Show logo"] = "إظهار الشعار",
        ["Reference"] = "المرجع",
        ["Receipt ready"] = "الإيصال جاهز",
        ["Receipt and audit ready"] = "الإيصال وسجل التدقيق جاهزان",
        ["Fast barcode scan"] = "مسح باركود سريع",
        ["Inventory guarded"] = "المخزون محمي",
        ["Inventory protection"] = "حماية المخزون",
        ["Today"] = "اليوم",
        ["Yesterday"] = "أمس",
        ["This Week"] = "هذا الأسبوع",
        ["This Month"] = "هذا الشهر",
        ["Previous Month"] = "الشهر السابق",
        ["This Year"] = "هذا العام",
        ["Custom Range"] = "نطاق مخصص",
        ["Before"] = "قبل",
        ["After"] = "بعد",
        ["Frequency"] = "التكرار",
        ["Position"] = "الموضع",
        ["Notes"] = "الملاحظات",
        ["Integrity Check"] = "فحص السلامة",
        ["Product rows"] = "صفوف المنتجات",
        ["Sales rows"] = "صفوف المبيعات",
        ["Live POS flow"] = "تشغيل مباشر لنقطة البيع",
        ["Every change writes a stock ledger entry."] = "كل تغيير يسجل حركة في سجل المخزون.",
        ["General, book, pricing, stock, category, media, and status metadata."] = "بيانات عامة وبيانات الكتاب والأسعار والمخزون والتصنيف والوسائط والحالة.",
        ["Generate, validate, and lookup products."] = "إنشاء الباركود والتحقق منه والبحث عن المنتجات.",
        ["Prepare single, multi-label, or sheet output."] = "تجهيز مخرجات ملصق واحد أو عدة ملصقات أو صفحة كاملة.",
        ["Loaded from appsettings; persistence prepared for future implementation."] = "يتم التحميل من إعدادات التطبيق؛ الحفظ مجهز للتنفيذ لاحقا."
    };

    /// <summary>
    /// Reverse index from an authored English resource value to its key, so a literal picked up
    /// from XAML can be matched back to its Arabic counterpart without scanning the dictionary.
    /// Several keys share a value (for example "Settings"); the first wins, and the values are
    /// identical translations anyway.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> EnglishValueToKey =
        English.GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase);

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
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return;
        }

        // Resource dictionaries, the window collection, and every subscriber of CultureChanged
        // are dispatcher-affine. Settings changes can arrive on a background thread, so marshal.
        if (!application.Dispatcher.CheckAccess())
        {
            application.Dispatcher.Invoke(() => ApplyCulture(language));
            return;
        }

        var normalized = NormalizeLanguage(language);
        CurrentLanguage = normalized;
        _strings = normalized.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? Arabic : English;

        var culture = BuildCulture(normalized);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        var resources = application.Resources;
        resources["AppFlowDirection"] = FlowDirection;
        resources["AppTextAlignment"] = IsRightToLeft ? TextAlignment.Right : TextAlignment.Left;
        foreach (var pair in English)
        {
            resources[pair.Key] = T(pair.Key);
        }

        // FrameworkElement.Language drives the culture that XAML bindings use to convert
        // numbers and dates in BOTH directions. Stock ar-EG formats decimals with U+066B
        // and groups with U+066C, so "1234.56" typed on a numeric keypad fails to parse and
        // two-way bindings silently keep the previous value. WPF resolves Language through
        // the cached read-only CultureInfo, so a customized clone cannot be injected here.
        // Money and quantity round-tripping is pinned to the invariant-style layout instead;
        // Arabic text and RTL layout are unaffected.
        var bindingLanguage = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
        foreach (Window window in application.Windows)
        {
            window.Language = bindingLanguage;
        }

        _logger.LogInformation("UI culture applied: {Language}", CurrentLanguage);
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Builds the working culture for a language, keeping Arabic month and day names while
    /// forcing Western digit grouping so numeric entry and display round-trip reliably.
    /// </summary>
    private static CultureInfo BuildCulture(string language)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(language).Clone();
        if (!language.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            return culture;
        }

        var numbers = culture.NumberFormat;
        numbers.NumberDecimalSeparator = ".";
        numbers.CurrencyDecimalSeparator = ".";
        numbers.PercentDecimalSeparator = ".";
        numbers.NumberGroupSeparator = ",";
        numbers.CurrencyGroupSeparator = ",";
        numbers.PercentGroupSeparator = ",";
        numbers.NativeDigits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];
        numbers.DigitSubstitution = DigitShapes.None;

        // ar-EG embeds RIGHT-TO-LEFT MARK inside its date patterns, which renders as stray
        // gaps inside grid cells and receipts.
        var dates = culture.DateTimeFormat;
        dates.ShortDatePattern = StripDirectionalMarks(dates.ShortDatePattern);
        dates.LongDatePattern = StripDirectionalMarks(dates.LongDatePattern);
        dates.ShortTimePattern = StripDirectionalMarks(dates.ShortTimePattern);
        dates.LongTimePattern = StripDirectionalMarks(dates.LongTimePattern);

        return culture;
    }

    private static string StripDirectionalMarks(string pattern) =>
        pattern.Replace("‎", string.Empty).Replace("‏", string.Empty);

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

    /// <summary>
    /// Translates an authored English literal. Only whole-string dictionary matches are
    /// translated: the caller cannot distinguish interface labels from live business data
    /// (product titles, customer names, barcodes, file paths), so partial substitution would
    /// corrupt records. Unknown text is returned untouched, and English mode is a no-op
    /// because callers always supply the English original.
    /// </summary>
    public string TranslateLiteral(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !IsRightToLeft)
        {
            return text;
        }

        var trimmed = text.Trim();
        if (ExtraArabic.TryGetValue(trimmed, out var translated))
        {
            return PreserveOuterWhitespace(text, translated);
        }

        if (EnglishValueToKey.TryGetValue(trimmed, out var key) && Arabic.TryGetValue(key, out translated))
        {
            return PreserveOuterWhitespace(text, translated);
        }

        return text;
    }

    private static string PreserveOuterWhitespace(string original, string translated)
    {
        var leading = original.Length - original.TrimStart().Length;
        var trailing = original.Length - original.TrimEnd().Length;
        return new string(' ', leading) + translated + new string(' ', trailing);
    }

    private static string NormalizeLanguage(string? language)
    {
        return string.IsNullOrWhiteSpace(language) || language.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
            ? "ar-EG"
            : "en-US";
    }

    private async void OnSettingsChanged(object? sender, SettingsChangedEvent e)
    {
        if (e.Category != SettingsCategory.Appearance)
        {
            return;
        }

        // async void on an event handler: an escaping exception would reach the unhandled
        // handler and tear down the app, so a failed re-read must not propagate.
        try
        {
            await ApplyConfiguredCultureAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to reapply UI culture after an appearance settings change");
        }
    }
}

