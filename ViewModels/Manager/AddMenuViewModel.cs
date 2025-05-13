using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.API.Services.Interfaces;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Threading.Tasks;
using EBISX_POS.API.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using System.IO;

namespace EBISX_POS.ViewModels.Manager
{
    /// <summary>
    /// ViewModel for adding new menu items to the system
    /// </summary>
    public partial class AddMenuViewModel : ObservableObject
    {
        // Dependencies
        private readonly IMenu _menuService;
        private readonly Window _window;
        private const string DEFAULT_USER = "EBISX@POS.com";

        // Observable properties for UI binding
        [ObservableProperty]
        private API.Models.Menu? _menuDetails;

        [ObservableProperty]
        private List<Category>? _categories;

        [ObservableProperty]
        private List<AddOnType>? _addOns;

        [ObservableProperty]
        private List<DrinkType>? _drinks;

        [ObservableProperty]
        private List<string> _drinkSizes = new List<string>
        {
            "",
            "Small",
            "Medium",
            "Large",
            "Extra Large"
        };

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private Bitmap? _selectedImage;

        /// <summary>
        /// Initializes a new instance of the AddMenuViewModel
        /// </summary>
        /// <param name="menuService">Service for menu operations</param>
        /// <param name="window">Parent window for dialogs</param>
        /// <exception cref="ArgumentNullException">Thrown when menuService or window is null</exception>
        public AddMenuViewModel(IMenu menuService, Window window)
        {
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _window = window ?? throw new ArgumentNullException(nameof(window));

            // Initialize MenuDetails with default values
            MenuDetails = new API.Models.Menu
            {
                MenuName = string.Empty,
                MenuPrice = 0,
                MenuIsAvailable = true,
                HasDrink = true,
                HasAddOn = true,
                IsAddOn = false,
                Category = new Category { Id = 0, CtgryName = "" },
                DrinkType = new DrinkType { Id = 0, DrinkTypeName = "" },
                AddOnType = new AddOnType { Id = 0, AddOnTypeName = "" }
            };

            // Load initial data
            _ = LoadCombos();
        }

        /// <summary>
        /// Loads all required combo box data (categories, drinks, add-ons)
        /// </summary>
        private async Task LoadCombos()
        {
            try
            {
                IsLoading = true;

                // 1) Load from service
                var cats = await _menuService.Categories();
                var drks = await _menuService.GetDrinkTypes();
                var adds = await _menuService.GetAddOnTypes();

                // 2) Insert a "blank" entry at index 0
                cats?.Insert(0, new Category { Id = 0, CtgryName = "" });
                drks?.Insert(0, new DrinkType { Id = 0, DrinkTypeName = "" });
                adds?.Insert(0, new AddOnType { Id = 0, AddOnTypeName = "" });

                // 3) Assign back to your ObservableProperties
                Categories = cats;
                Drinks = drks;
                AddOns = adds;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading combo data: {ex}");
                await ShowError("Failed to load menu data. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Validates the menu details before submission
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        private bool ValidateMenuDetails()
        {
            if (MenuDetails == null)
            {
                ShowError("Menu details cannot be empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(MenuDetails.MenuName))
            {
                ShowError("Menu name is required");
                return false;
            }

            if (MenuDetails.MenuPrice <= 0)
            {
                ShowError("Menu price must be greater than 0");
                return false;
            }

            if (MenuDetails.Category == null || MenuDetails.Category.Id == 0)
            {
                ShowError("Please select a category");
                return false;
            }

            // Validate drink type if HasDrink is true
            if (MenuDetails.HasDrink && (MenuDetails.DrinkType == null || MenuDetails.DrinkType.Id == 0))
            {
                ShowError("Please select a drink type when HasDrink is enabled");
                return false;
            }

            // Validate add-on type if HasAddOn is true
            if (MenuDetails.HasAddOn && (MenuDetails.AddOnType == null || MenuDetails.AddOnType.Id == 0))
            {
                ShowError("Please select an add-on type when HasAddOn is enabled");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Command to handle image upload
        /// </summary>
        [RelayCommand]
        public async Task UploadImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Choose an image",
                AllowMultiple = false,
                Filters =
                {
                    new FileDialogFilter
                    {
                        Name = "Image Files",
                        Extensions = { "png", "jpg", "jpeg", "bmp", "gif" }
                    }
                }
            };

            var result = await dlg.ShowAsync(_window);
            if (result is null || result.Length == 0)
                return;

            var path = result[0];
            try
            {
                using var stream = File.OpenRead(path);
                var bitmap = await Task.Run(() => Bitmap.DecodeToWidth(stream, 300));
                if (bitmap != null)
                {
                    SelectedImage = bitmap;
                    MenuDetails!.MenuImagePath = path;
                    OnPropertyChanged(nameof(SelectedImage));
                }
                else
                {
                    await ShowError("Failed to decode image file.");
                }
            }
            catch (IOException)
            {
                await ShowError("Failed to load image file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image: {ex}");
                await ShowError("An unexpected error occurred while loading the image.");
            }
        }

        /// <summary>
        /// Command to add a new menu item
        /// </summary>
        [RelayCommand]
        private async Task AddMenu()
        {
            if (IsLoading) return;

            if (!ValidateMenuDetails())
            {
                return;
            }

            try
            {
                IsLoading = true;

                var newMenu = new API.Models.Menu
                {
                    MenuName = MenuDetails!.MenuName,
                    MenuPrice = MenuDetails.MenuPrice,
                    Category = MenuDetails.Category,
                    Size = MenuDetails.Size,
                    DrinkType = MenuDetails.DrinkType,
                    AddOnType = MenuDetails.AddOnType,
                    HasAddOn = MenuDetails.HasAddOn,
                    HasDrink = MenuDetails.HasDrink,
                    IsAddOn = MenuDetails.IsAddOn,
                    MenuImagePath = MenuDetails.MenuImagePath,
                    MenuIsAvailable = MenuDetails.MenuIsAvailable
                };

                var (isSuccess, message, _) = await _menuService.AddMenu(newMenu, DEFAULT_USER);

                if (isSuccess)
                {
                    await ShowSuccess(message);
                    _window.Close();
                }
                else
                {
                    await ShowError(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding menu: {ex}");
                await ShowError("Failed to add menu. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Command to cancel the add menu operation
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            _window.Close();
        }
        [RelayCommand]

        private void ClearImage()
        {
            SelectedImage = null;
            MenuDetails.MenuImagePath = null;
        }

        /// <summary>
        /// Shows a success message dialog
        /// </summary>
        /// <param name="message">Message to display</param>
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

        /// <summary>
        /// Shows an error message dialog
        /// </summary>
        /// <param name="message">Error message to display</param>
        private async Task ShowError(string message)
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "Error",
                ContentMessage = message,
                Icon = Icon.Error
            });

            await msgBox.ShowAsPopupAsync(_window);
        }
    }
}
