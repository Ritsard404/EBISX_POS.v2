
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class MenuViewModel : ObservableObject
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<API.Models.Menu> _menus = new();

        [ObservableProperty]
        private API.Models.Menu? _selectedMenu;

        [ObservableProperty]
        private bool _isLoading;

        public MenuViewModel(IMenu menu, Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _menuService = menu ?? throw new ArgumentNullException(nameof(menu));
        }

        private async Task LoadMenus()
        {
            try
            {
                IsLoading = true;
                var menus = await _menuService.GetAllMenus();
                if (menus != null)
                {
                    Menus.Clear();
                    foreach (var menu in menus)
                    {
                        Menus.Add(menu);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading Menus: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void SaveCategoryChanges(API.Models.Menu menu)
        {
            if (menu == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.UpdateMenu(menu, "EBISX@POS.com");

                if (isSuccess)
                {
                    await LoadMenus();
                    //await ShowSuccess(message);
                    return;
                }
                ShowError(message);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user changes: {ex}");
            }
        }

        public async Task RemoveMenu()
        {
            if (SelectedMenu == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.DeleteMenu(SelectedMenu.Id, "EBISX@POS.com");

                if (isSuccess)
                {
                    await LoadMenus();
                    await ShowSuccess(message);
                    return;
                }
                ShowError(message);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user changes: {ex}");
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            _window.Close();
        }

        [RelayCommand]
        private async Task AddNewMenu()
        {
            var window = new AddMenuWindow();
            window.DataContext = new AddMenuViewModel(_menuService, window);
            await window.ShowDialog(_window);
            await LoadMenus();
        }

        private async void ShowError(string message)
        {
            var msgBox = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Error",
                    ContentMessage = message,
                    Icon = Icon.Error
                });

            await msgBox.ShowAsPopupAsync(_window);
        }

        private async Task ShowSuccess(string message)
        {
            var successBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "Success",
                ContentMessage = message,
                Icon = Icon.Success
            });
            await successBox.ShowAsPopupAsync(_window);
        }
    }
}