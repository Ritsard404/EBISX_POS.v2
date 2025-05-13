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
using Tmds.DBus.Protocol;
using Avalonia.Collections;
using System.Linq;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class MenuViewModel : ObservableObject
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<API.Models.Menu> _menus = new ObservableCollection<API.Models.Menu>();

        [ObservableProperty]
        private API.Models.Menu? _selectedMenu;

        [ObservableProperty]
        private bool _isLoading;

        public MenuViewModel(IMenu menu, Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _menuService = menu ?? throw new ArgumentNullException(nameof(menu));

            _ = LoadMenus();
        }

        private async Task LoadMenus()
        {
            try
            {
                IsLoading = true;
                var menus = await _menuService.GetAllMenus();
                if (menus != null)
                {
                    // Clear and update the collection
                    Menus.Clear();
                    foreach (var menu in menus)
                    {
                        Menus.Add(menu);
                    }
                    
                    // Force collection update
                    OnPropertyChanged(nameof(Menus));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading Menus: {ex}");
                ShowError("Failed to load menus. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RemoveMenu(API.Models.Menu menu)
        {
            if (menu == null) return;

            try
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = $"Menu Deletion",
                        ContentMessage = "Press Ok to proceed deletion.",
                        ButtonDefinitions = ButtonEnum.OkCancel,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        SystemDecorations=SystemDecorations.BorderOnly,
                        Icon = Icon.Warning
                    });

                var result = await box.ShowAsPopupAsync(_window);
                if(result == ButtonResult.Ok)
                {
                    var (isSuccess, message) = await _menuService.DeleteMenu(menu.Id, "EBISX@POS.com");

                    if (isSuccess)
                    {
                        // Remove from collection and force update
                        Menus.Remove(menu);
                        OnPropertyChanged(nameof(Menus));
                        await ShowSuccess(message);
                        return;
                    }

                    ShowError(message);
                    return;
                }
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
            var result = await window.ShowDialog<bool?>(_window);
            
            if (result == true)
            {
                Debug.WriteLine("New menu added successfully.");
                await LoadMenus();
            }
        }

        public async Task EditMenu(API.Models.Menu menu)
        {
            var window = new AddMenuWindow();
            window.DataContext = new AddMenuViewModel(_menuService, window, menu);
            var result = await window.ShowDialog<bool?>(_window);
            
            if (result == true)
            {
                Debug.WriteLine($"Menu {menu.MenuName} updated successfully.");
                await LoadMenus();
            }
        }

        private void ShowError(string message)
        {
            var msgBox = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Error",
                    ContentMessage = message,
                    Icon = Icon.Error
                });

            msgBox.ShowAsPopupAsync(_window);
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