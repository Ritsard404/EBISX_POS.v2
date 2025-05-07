using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity; // Add this line
using EBISX_POS.Models;
using EBISX_POS.ViewModels;
using EBISX_POS.Services; // Ensure this is added
using System.Diagnostics;
using System.Threading.Tasks;
using EBISX_POS.State;
using EBISX_POS.API.Models;
using System.Linq;

namespace EBISX_POS.Views
{
    public partial class ItemListView : UserControl
    {
        private readonly MenuService _menuService;


        private Button? _selectedItemButton;
        private string? _selectedItem;

        public ItemListView(MenuService menuService) // Ensure this constructor is public
        {
            InitializeComponent();
            _menuService = menuService;
            DataContext = new ItemListViewModel(menuService); // Set initial DataContext
            this.Loaded += OnLoaded; // Add this line

        }

        private void OnLoaded(object? sender, RoutedEventArgs e) // Update the event handler signature
        {
            if (ItemsList.ItemContainerGenerator.ContainerFromIndex(0) is ToggleButton firstButton)
            {
                firstButton.IsChecked = true;
                _selectedItemButton = firstButton;
                _selectedItem = firstButton.Content?.ToString();
            }
        }

        public async Task LoadMenusAsync(int categoryId)
        {
            if (DataContext is ItemListViewModel viewModel)
            {
                await viewModel.LoadMenusAsync(categoryId);
            }
        }

        public void UpdateDataContext(ItemListViewModel viewModel)
        {
            DataContext = viewModel;
        }

        private async void OnItemClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.DataContext is ItemMenu item)
            {
                HandleSelection(ref _selectedItemButton, clickedButton, ref _selectedItem);

                if (item.IsSolo || item.IsAddOn)
                {
                    var owner = this.VisualRoot as Window;

                    if (OrderState.CurrentOrderItem.Quantity < 1)
                    {
                        OrderState.CurrentOrderItem.Quantity = 1;
                    }

                    // Determine the item type based on the item's properties.
                    string itemType = item.IsDrink ? "Drink" : item.IsAddOn ? "AddOn" : "Menu";

                    // Clear the current sub-orders.
                    OrderState.CurrentOrderItem.SubOrders.Clear();

                    // Update the order item with the appropriate type and details.
                    OrderState.UpdateItemOrder(
                        itemType: itemType,
                        itemId: item.Id,
                        name: item.ItemName + (item.IsSolo ? " (Solo)" : ""),
                        price: item.Price,
                        size: item.Size
                    );


                    var mainWindow = this.VisualRoot as MainWindow;

                    if (mainWindow != null)
                    {
                        mainWindow.IsLoadMenu.IsVisible = true;
                        mainWindow.IsMenuAvail.IsVisible = false;
                    }
                    // Finalize the current order and exit.
                    await OrderState.FinalizeCurrentOrder(isSolo: true, owner);
                    if (mainWindow != null)
                    {
                        mainWindow.IsLoadMenu.IsVisible = false;
                        mainWindow.IsMenuAvail.IsVisible = true;
                    }
                    return;
                }


                OrderState.CurrentOrderItem.SubOrders.Clear();
                OrderState.CurrentOrderItem.Quantity = (OrderState.CurrentOrderItem.Quantity < 1)
                    ? 1
                    : OrderState.CurrentOrderItem.Quantity;
                OrderState.UpdateItemOrder(itemType: "Menu", itemId: item.Id, name: item.ItemName, price: item.Price, size: null);

                var detailsWindow = new SubItemWindow(item, _menuService)
                {
                    DataContext = new SubItemWindowViewModel(item, _menuService)
                };


                await detailsWindow.ShowDialog((Window)this.VisualRoot);
            }
        }

        private void HandleSelection(ref Button? selectedButton, Button clickedButton, ref string? selectedValue)
        {
            if (selectedButton == clickedButton)
            {
                selectedButton = null;
                selectedValue = null;
            }
            else
            {
                selectedButton = clickedButton;
                selectedValue = clickedButton.Content?.ToString();
            }
        }
    }
}
