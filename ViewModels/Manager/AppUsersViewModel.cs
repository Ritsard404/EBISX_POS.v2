using System.Collections.ObjectModel;
using System.Threading.Tasks;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Avalonia.Controls;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class AppUsersViewModel : ViewModelBase
    {
        private readonly IData _dataService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public List<string> Roles { get; } = new() { "Manager", "Cashier" };

        public List<StatusOption> StatusOptions { get; } = new()
        {
            new StatusOption { Display = "Active", Value = true },
            new StatusOption { Display = "Inactive", Value = false }
        };

        public AppUsersViewModel(IData dataService, Window window)
        {
            Debug.WriteLine("→ AppUsersViewModel constructor started");
            _dataService = dataService;
            _window = window;
            
            // Initialize with empty collection
            Users = new ObservableCollection<User>();
            
            // Load users after initialization
            Task.Run(async () => 
            {
                await Task.Delay(100); // Small delay to ensure UI is ready
                await LoadUsersCommand.ExecuteAsync(null);
            });
            
            Debug.WriteLine("→ AppUsersViewModel constructor finished");
        }

        [RelayCommand]
        private async Task LoadUsers()
        {
            Debug.WriteLine("→ LoadUsers started");
            if (_dataService == null)
            {
                Debug.WriteLine("‼️ LoadUsers: _dataService is null");
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                Debug.WriteLine("… calling GetUsers()");
                var users = await _dataService.GetUsers();
                Users = new ObservableCollection<User>(users);
                Debug.WriteLine($"✅ Loaded {users.Count} users.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load users: {ex.Message}";
                Debug.WriteLine($"❌ LoadUsers exception: {ex}");
            }
            finally
            {
                IsLoading = false;
                Debug.WriteLine("→ LoadUsers finished");
            }
        }

        [RelayCommand]
        private async Task SaveUserChanges(User user)
        {
            Debug.WriteLine($"→ SaveUserChanges started for {user.UserEmail}");
            if (user == null)
            {
                Debug.WriteLine("‼️ SaveUserChanges: user is null");
                return;
            }
            if (_dataService == null)
            {
                Debug.WriteLine("‼️ SaveUserChanges: _dataService is null");
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                Debug.WriteLine($"… calling UpdateUser for {user.UserEmail}");
                var (isSuccess, message) = await _dataService.UpdateUser(user, "user@example.com");
                Debug.WriteLine($"… UpdateUser returned success={isSuccess}, message='{message}'");

                if (!isSuccess)
                {
                    ErrorMessage = message;
                    Debug.WriteLine($"❌ SaveUserChanges error: {message}");
                }
                else
                {
                    Debug.WriteLine("✅ SaveUserChanges succeeded, reloading users");
                    await LoadUsersCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save changes: {ex.Message}";
                Debug.WriteLine($"❌ SaveUserChanges exception: {ex}");
            }
            finally
            {
                IsLoading = false;
                Debug.WriteLine("→ SaveUserChanges finished");
            }
        }

        [RelayCommand]
        private async Task AddUser()
        {
            var addUserWindow = new AddUserWindow
            {
                DataContext = new AddUserViewModel(_dataService, _window)
            };

            await addUserWindow.ShowDialog(_window);
            await LoadUsersCommand.ExecuteAsync(null);
        }

        partial void OnSelectedUserChanged(User? value)
        {
            Debug.WriteLine($"→ OnSelectedUserChanged fired: {(value?.UserEmail ?? "null")}");
            if (value != null)
            {
                SaveUserChangesCommand.Execute(value);
            }
        }
    }

    public class StatusOption
    {
        public string Display { get; set; } = string.Empty;
        public bool Value { get; set; }
    }
}