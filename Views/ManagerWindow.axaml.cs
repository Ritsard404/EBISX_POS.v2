using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.Services;
using EBISX_POS.ViewModels;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using EBISX_POS.State;
using EBISX_POS.Models;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Options;
using EBISX_POS.Util;
using EBISX_POS.Views.Manager;
using EBISX_POS.ViewModels.Manager;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace EBISX_POS.Views
{
    public partial class ManagerWindow : Window
    {
        private readonly string _cashTrackReportPath;

        private readonly IServiceProvider? _serviceProvider;
        private readonly MenuService _menuService;
        private readonly AuthService _authService;

        // Constructor with IServiceProvider parameter
        public ManagerWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = this;

            _serviceProvider = serviceProvider;

            // fetch only what we actually need
            _menuService = serviceProvider.GetRequiredService<MenuService>();
            _authService = serviceProvider.GetRequiredService<AuthService>();
            _cashTrackReportPath = serviceProvider
                                        .GetRequiredService<IOptions<SalesReport>>()
                                        .Value.CashTrackReport;

            // cache these checks so you only call them once
            bool hasManager = !string.IsNullOrWhiteSpace(CashierState.ManagerEmail);
            bool hasCashier = !string.IsNullOrWhiteSpace(CashierState.CashierEmail);

            // show overlay & hide BackButton only if nobody is signed in
            var overlayOn = !(hasManager || hasCashier);
            ButtonOverlay.IsVisible = overlayOn;

            BackButton.IsVisible = (hasManager || hasCashier);

            // enable SalesReport only for managers
            SalesReport.IsEnabled = hasManager;
            CashPullOut.IsEnabled = !hasManager;

            Mode.IsEnabled = hasManager;
            Mode.IsChecked = CashierState.IsTrainMode;

            DataLayout.IsVisible = hasManager || !(hasManager || hasCashier);

            // disable LogOut for managers (since they’d go back to login instead)
            LogOut.IsEnabled = !hasManager;
        }
        public ManagerWindow() : this(App.Current.Services.GetRequiredService<IServiceProvider>())
        { }
        // This constructor is required for Avalonia to instantiate the view in XAML.

        private void ShowLoader(bool show)
        {
            LoadingOverlay.IsVisible = show;
        }

        private void SalesReport_Button(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //var reportWindow = _serviceProvider?.GetRequiredService<DailySalesReportView>();
            //reportWindow?.Show();
            //if (!string.IsNullOrWhiteSpace(CashierState.CashierEmail))
            //    return;

            //var swipeManager = new ManagerSwipeWindow(header: "Z Reading", message: "Please ask the manager to swipe.", ButtonName: "Swipe");
            //bool isSwiped = await swipeManager.ShowDialogAsync(this);

            //if (isSwiped)
            //{
            ShowLoader(true);
            ReceiptPrinterUtil.PrintZReading(_serviceProvider!);
            ShowLoader(false);
            //}

        }

        private void TransactionLog(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var reportWindow = _serviceProvider?.GetRequiredService<SalesHistoryWindow>();
            reportWindow?.ShowDialog(this);
        }

        private async void Cash_Track_Button(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //var cashTrack = _serviceProvider?.GetRequiredService<CashTrackView>();
            //if (cashTrack != null)
            //{
            //    cashTrack.GenerateCashTrack(sender, e);
            //}


            ShowLoader(true);
            var reportService = App.Current.Services.GetRequiredService<ReportService>();

            var (CashInDrawer, CurrentCashDrawer) = await reportService.CashTrack();

            // Ensure the target directory exists
            if (!Directory.Exists(_cashTrackReportPath))
            {
                Directory.CreateDirectory(_cashTrackReportPath);
            }

            // Define the file path
            string fileName = $"Cash-Track-{CashierState.CashierEmail}-{DateTimeOffset.UtcNow.ToString("MMMM-dd-yyyy-HH-mm-ss")}.txt";
            string filePath = Path.Combine(_cashTrackReportPath, fileName);

            string reportContent = $@"
                ==================================
                        Cash Track Report
                ==================================
                Cash In Drawer: {CashInDrawer:C}
                Total Cash Drawer: {CurrentCashDrawer:C}
                ";

            reportContent = string.Join("\n", reportContent.Split("\n").Select(line => line.Trim()));
            File.WriteAllText(filePath, reportContent);

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            ShowLoader(false);
        }

        private void Back_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var isManager = !string.IsNullOrWhiteSpace(CashierState.ManagerEmail);

            // Only reset if they were a manager
            if (isManager)
                CashierState.CashierStateReset();

            // Pick which window to open
            Window nextWindow = isManager
                ? new LogInWindow()
                : new MainWindow(_menuService, _authService)
                {
                    DataContext = new MainWindowViewModel(_menuService)
                };


            if (Application.Current.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = nextWindow;
            }

            nextWindow.Show();
            Close();
        }


        private async void CashPullOut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var setCashDrawer = new SetCashDrawerWindow("Withdraw");
            await setCashDrawer.ShowDialog(this);
        }
        private async void ManagerLog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var managerLog = new UserLogsWindow();
            managerLog.DataContext = new UserLogsViewModel(true);
            await managerLog.ShowDialog(this);
        }
        private async void CashierLog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var managerLog = new UserLogsWindow();
            managerLog.DataContext = new UserLogsViewModel(false);
            await managerLog.ShowDialog(this);
        }
        private async void LoadData_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowLoader(true);
            try
            {
                if (!await NetworkHelper.IsOnlineAsync())
                {
                    await ShowMessageAsync("No Internet Connection",
                                           "Please check your network and try again.");
                    return;  // loader will be hidden, window stays open
                }

                var (isSuccess, message) = await _authService.LoadDataAsync();
                if (!isSuccess)
                {
                    await ShowMessageAsync("Load Failed",
                                           string.IsNullOrWhiteSpace(message)
                                               ? "An unknown error occurred."
                                               : message);
                    return;
                }

                await PostLoadFlowAsync();
            }
            catch (Exception ex)
            {
                // log or report ex
                await ShowMessageAsync("Error", "Sorry, something went wrong.");
            }
            finally
            {
                ShowLoader(false);
            }
        }

        private async Task PostLoadFlowAsync()
        {
            // if manager email is set, ask if they want to log in as cashier
            if (!string.IsNullOrWhiteSpace(CashierState.ManagerEmail))
            {
                var result = await MessageBoxManager
                   .GetMessageBoxStandard(new MessageBoxStandardParams
                   {
                       ContentHeader = "Data Updated!",
                       ContentMessage = "Data loaded successfully. Log in as cashier now?",
                       ButtonDefinitions = ButtonEnum.OkCancel,
                       WindowStartupLocation = WindowStartupLocation.CenterOwner,
                       CanResize = false,
                       SizeToContent = SizeToContent.WidthAndHeight,
                       Width = 400,
                       ShowInCenter = true,

                   })
                   .ShowAsPopupAsync(this);

                if (result != ButtonResult.Ok)
                    return;
                CashierState.CashierStateReset();
            }

            ShowLoginAndClose();
        }

        private void ShowLoginAndClose()
        {
            var loginWin = new LogInWindow();
            if (Application.Current.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = loginWin;
            }
            loginWin.Show();
            this.Close();
        }

        private Task<ButtonResult> ShowMessageAsync(string header, string message)
            => MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = header,
                    ContentMessage = message,
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true
                })
                .ShowWindowDialogAsync(this);

        private async void Refund_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var refundOrder = new SetCashDrawerWindow("Returned");
            await refundOrder.ShowDialog(this);
        }

        private async void LogOut_Button(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (OrderState.CurrentOrder.Any())
            {

                await MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = $"Error",
                        ContentMessage = "Unable to log out – there is a pending order.",
                        ButtonDefinitions = ButtonEnum.Ok, // Defines the available buttons
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = MsBox.Avalonia.Enums.Icon.Error
                    }).ShowAsPopupAsync(this);
                return;
            }

            var box = MessageBoxManager.GetMessageBoxStandard(
                new MessageBoxStandardParams
                {
                    ContentHeader = $"Log Out",
                    ContentMessage = "Please ask the manager to swipe.",
                    ButtonDefinitions = ButtonEnum.OkCancel, // Defines the available buttons
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                });

            var managerEmail = "user1@example.com";

            var result = await box.ShowAsPopupAsync(this);
            switch (result)
            {
                case ButtonResult.Ok:

                    ShowLoader(true);
                    var setCashDrawer = new SetCashDrawerWindow("Cash-Out");
                    await setCashDrawer.ShowDialog(this);

                    ReceiptPrinterUtil.PrintXReading(_serviceProvider!);

                    // Open the TenderOrderWindow
                    var (isSuccess, Message) = await _authService.LogOut(managerEmail);
                    if (isSuccess)
                    {
                        CashierState.CashierName = null;
                        CashierState.CashierEmail = null;
                        OrderState.CurrentOrder.Clear();
                        OrderState.CurrentOrderItem = new OrderItemState();
                        TenderState.tenderOrder.Reset();

                        var logInWindow = new LogInWindow();
                        if (Application.Current.ApplicationLifetime
                            is IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            desktop.MainWindow = logInWindow;
                        }
                        logInWindow.Show();
                        ShowLoader(false);
                        Close();
                    }
                    return;
                case ButtonResult.Cancel:
                    return;
                default:
                    return;
            }

        }
        private async void ChangeMode_Button(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowLoader(true);
            if (string.IsNullOrWhiteSpace(CashierState.ManagerEmail))
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = $"Error",
                        ContentMessage = "Unable to change mode – invalid credential.",
                        ButtonDefinitions = ButtonEnum.Ok, // Defines the available buttons
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = MsBox.Avalonia.Enums.Icon.Error,
                        SystemDecorations = SystemDecorations.None
                    }).ShowAsPopupAsync(this);
                return;
            }

            var isTrainMode = await _authService.ChangeModeAsync(CashierState.ManagerEmail);
            CashierState.IsTrainMode = isTrainMode;
            Mode.IsChecked = CashierState.IsTrainMode;
            ShowLoader(false);
        }


    }
}