using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class DrinkAndAddOnTypeViewModel : ObservableObject
    {
        private readonly IMenu _menuService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<AddOnType> _addOnTypes = new();

        [ObservableProperty]
        private ObservableCollection<DrinkType> _drinkTypes = new();

        [ObservableProperty]
        private AddOnType? _selectedAddOnType;

        [ObservableProperty]
        private DrinkType? _selectedDrinkType;

        [ObservableProperty]
        private bool _isLoading;

        public DrinkAndAddOnTypeViewModel(IMenu menu, Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _menuService = menu ?? throw new ArgumentNullException(nameof(menu));

            _ = LoadAddOnsAndDrinks();
        }

        private async Task LoadAddOnsAndDrinks()
        {
            try
            {
                IsLoading = true;
                var addOnTypes = await _menuService.GetAddOnTypes();
                var drinkTypes = await _menuService.GetDrinkTypes();
                if (addOnTypes != null)
                {
                    AddOnTypes.Clear();
                    foreach (var addOn in addOnTypes)
                    {
                        AddOnTypes.Add(addOn);
                    }
                }
                if (drinkTypes != null)
                {
                    DrinkTypes.Clear();
                    foreach (var drinkType in drinkTypes)
                    {
                        DrinkTypes.Add(drinkType);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading Add ons and DrinkTypes: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void SaveAddOnTypeChanges(AddOnType addOnType)
        {
            if (addOnType == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.UpdateAddOnType(addOnType, "EBISX@POS.com");

                if (isSuccess)
                {
                    await LoadAddOnsAndDrinks();
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

        private async void SaveDrinkTypeChanges(DrinkType drinkType)
        {
            if (drinkType == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.UpdateDrinkType(drinkType, "EBISX@POS.com");

                if (isSuccess)
                {
                    await LoadAddOnsAndDrinks();
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
        private async Task NewAddOnType()
        {
            try
            {
                var addDrinkAndAddOnTypeWindow = new AddDrinkAndAddOnTypeWindow();
                addDrinkAndAddOnTypeWindow.DataContext = new AddDrinkAndAddOnTypeViewModel(_menuService, addDrinkAndAddOnTypeWindow, false);

                await addDrinkAndAddOnTypeWindow.ShowDialog(_window);
                await LoadAddOnsAndDrinks();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening add user window: {ex}");
            }
        }

        [RelayCommand]
        private async Task NewDrinkType()
        {
            try
            {
                var addDrinkAndAddOnTypeWindow = new AddDrinkAndAddOnTypeWindow();
                addDrinkAndAddOnTypeWindow.DataContext = new AddDrinkAndAddOnTypeViewModel(_menuService, addDrinkAndAddOnTypeWindow, true);

                await addDrinkAndAddOnTypeWindow.ShowDialog(_window);
                await LoadAddOnsAndDrinks();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening add user window: {ex}");
            }
        }

        public async Task RemoveAddOnType()
        {
            if (SelectedAddOnType == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.DeleteAddOnType(SelectedAddOnType.Id, "EBISX@POS.com");

                if (isSuccess)
                {
                    await LoadAddOnsAndDrinks();
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

        public async Task RemoveDrinkType()
        {
            if (SelectedDrinkType == null) return;

            try
            {
                var (isSuccess, message) = await _menuService.DeleteDrinkType(SelectedDrinkType.Id, "EBISX@POS.com");

                if (isSuccess)
                {
                    await LoadAddOnsAndDrinks();
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

        partial void OnSelectedDrinkTypeChanged(DrinkType? value)
        {
            if (value != null)
            {
                SaveDrinkTypeChanges(value);
            }
        }
        partial void OnSelectedAddOnTypeChanged(AddOnType? value)
        {
            if (value != null)
            {
                SaveAddOnTypeChanges(value);
            }
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