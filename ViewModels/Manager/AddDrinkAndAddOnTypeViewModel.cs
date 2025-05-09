using System;
using System.Threading.Tasks;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using Tmds.DBus.Protocol;
using Avalonia.Controls.Shapes;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class AddDrinkAndAddOnTypeViewModel : ViewModelBase
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private bool _isDrink;

        [ObservableProperty]
        private string _drinkTypeName = string.Empty;
        [ObservableProperty]
        private string _addOnTypeName = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;

        public AddDrinkAndAddOnTypeViewModel(IMenu menuService, Window window, bool isDrink)
        {
            _menuService = menuService;
            _window = window;
            _isDrink = isDrink;
        }

        [RelayCommand]
        private async Task AddMenuType()
        {
            if (IsDrink)
            {

                if (string.IsNullOrWhiteSpace(DrinkTypeName))
                {
                    ShowError("All field are required");
                    return;
                }

                var newDrinkType = new DrinkType
                {
                    DrinkTypeName = DrinkTypeName,
                };

                var (isSuccess, message, _) = await _menuService.AddDrinkType(newDrinkType, "EBISX@POS.com");

                if (isSuccess)
                {
                    await ShowSuccess(message);
                    _window.Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(AddOnTypeName))
                {
                    ShowError("All field are required");
                    return;
                }

                var newAddOnType = new AddOnType
                {
                    AddOnTypeName = AddOnTypeName,
                };

                var (isSuccess, message, _) = await _menuService.AddAddOnType(newAddOnType, "EBISX@POS.com");

                if (isSuccess)
                {
                    await ShowSuccess(message);
                    _window.Close();
                }
                else
                {
                    ShowError(message);
                }
            }

            try
            {
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                Debug.WriteLine($"❌ AddUser exception: {ex}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _window.Close();
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