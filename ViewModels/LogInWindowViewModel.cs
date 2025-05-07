using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.API.Services.DTO.Auth;
using EBISX_POS.Services;
using EBISX_POS.State;
using EBISX_POS.Views;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace EBISX_POS.ViewModels
{
    public partial class LogInWindowViewModel : ViewModelBase
    {
        private readonly MenuService _menuService;
        private readonly AuthService _authService;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private CashierDTO? selectedCashier;

        [ObservableProperty]
        private string managerEmail;

        private bool _hasNavigated = false;

        [ObservableProperty]
        private bool hasTrainMode = false;


        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ObservableCollection<CashierDTO> Cashiers { get; } = new();

        public LogInWindowViewModel(AuthService authService, MenuService menuService)
        {
            _authService = authService;
            _menuService = menuService;
            
            InitializeAsync();
        }
        public async void InitializeAsync()
        {
            try
            {
                // Wait for database initialization to complete
                await Task.Delay(1000); // Give time for database initialization
                
                await CheckData(); // this might navigate and close
                await CheckMode();
                await LoadCashiersAsync();
                await CheckPendingOrderAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
                NotificationService.NetworkIssueMessage();
            }
        }

        private async Task LoadCashiersAsync()
        {
            try
            {
                IsLoading = true;


                var cashiers = await _authService.GetCashiersAsync();
                Cashiers.Clear();

                Cashiers.Add(new CashierDTO { Name = string.Empty, Email = string.Empty });

                foreach (var cashier in cashiers)
                {
                    Cashiers.Add(cashier);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading cashiers: {ex.Message}");
                NotificationService.NetworkIssueMessage();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckData()
        {
            try
            {
                IsLoading = true;
                var (isSuccess, message) = await _authService.CheckData();
                Debug.WriteLine($"CheckData → Success={isSuccess}, Message={message}");
                if (isSuccess)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            var managerWindow = new ManagerWindow();
                            desktop.MainWindow = managerWindow;
                            managerWindow.Show();
                            
                            // Close the LoginWindow
                            if (desktop.Windows.FirstOrDefault(w => w is LogInWindow) is LogInWindow loginWindow)
                            {
                                loginWindow.Close();
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CheckData: {ex.Message}");
                NotificationService.NetworkIssueMessage();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckMode()
        {
            try
            {

                IsLoading = true;
                var isSuccess = await _authService.IsTrainModeAsync();
                if (isSuccess)
                {
                    CashierState.IsTrainMode = true;
                    HasTrainMode = true;

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking mode: {ex.Message}");
                NotificationService.NetworkIssueMessage();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckPendingOrderAsync()
        {
            if (_hasNavigated) return;
            try
            {

                IsLoading = true;
                var (success, cashierEmail, cashierName ) = await _authService.HasPendingOrder();
                if (success)
                {
                    if (_hasNavigated) return;

                    var owner = GetCurrentWindow();

                    _hasNavigated = true;

                    var alertBox = MessageBoxManager.GetMessageBoxStandard(
                        new MessageBoxStandardParams
                        {
                            ContentHeader = "Cashier Session Restored",
                            ContentMessage = "Your previous session has been successfully restored after an unexpected closure. Please review your cashier data and continue.",
                            ButtonDefinitions = ButtonEnum.Ok,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false,
                            SizeToContent = SizeToContent.WidthAndHeight,
                            Width = 400,
                            ShowInCenter = true,
                            SystemDecorations = SystemDecorations.None,
                        });

                    await alertBox.ShowAsPopupAsync(owner);

                    NavigateToMainWindow(cashierEmail: cashierEmail, cashierName: cashierName, owner);
                    //owner.Close();
                }
                IsLoading = false;
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking pending order: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LogInAsync()
        {
            try
            {
                CashierState.CashierStateReset();

                var owner = GetCurrentWindow();
                ErrorMessage = string.Empty;
                IsLoading = true;

                var logInDTO = new LogInDTO
                {
                    CashierEmail = SelectedCashier?.Email ?? "",
                    ManagerEmail = ManagerEmail
                };

                var result = await _authService.LogInAsync(logInDTO);
                if (!result.sucess)
                {
                    ErrorMessage = result.name;
                    OnPropertyChanged(nameof(HasError));
                    return;
                }

                if (result.isManager)
                {
                    CashierState.ManagerEmail = result.email;
                    var managerWindow = new ManagerWindow();
                    if (Application.Current.ApplicationLifetime
                        is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.MainWindow = managerWindow;
                    }
                    managerWindow.Show();

                    owner.Close();
                    return;
                }

                NavigateToMainWindow(cashierEmail: result.email, cashierName: result.name, owner);
                //owner.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log in error: {ex.Message}");
                ErrorMessage = "An unexpected error occurred.";
                OnPropertyChanged(nameof(HasError));
                NotificationService.NetworkIssueMessage();
            }
            finally
            {
                IsLoading = false;
            }
        }
        private Window? GetCurrentWindow()
        {
            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                return desktopLifetime.MainWindow;
            }
            return null;
        }

        private void NavigateToMainWindow(string cashierEmail, string cashierName, Window owner)
        {
            CashierState.CashierEmail = cashierEmail;
            CashierState.CashierName = cashierName;

            var mainWindow = new MainWindow(_menuService, _authService)
            {
                DataContext = new MainWindowViewModel(_menuService)
            };

            mainWindow.Show();
            owner.Close();

            //if (Application.Current.ApplicationLifetime
            //    is IClassicDesktopStyleApplicationLifetime desktop)
            //{
            //    desktop.MainWindow = mainWindow;
            //}
        }
    }
}
