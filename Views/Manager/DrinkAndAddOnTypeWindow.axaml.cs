using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS;

public partial class DrinkAndAddOnTypeWindow : Window
{
    private DrinkAndAddOnTypeViewModel ViewModel => (DrinkAndAddOnTypeViewModel)DataContext!;

    public DrinkAndAddOnTypeWindow()
    {
        InitializeComponent();

        // Get the data service from DI container
        var menuService = App.Current.Services.GetRequiredService<IMenu>();

        // Create and set the ViewModel
        DataContext = new DrinkAndAddOnTypeViewModel(menuService, this);
    }

    private async void RemoveAddOnTypeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.RemoveAddOnType();
    }
    private async void RemoveDrinkTypeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.RemoveDrinkType();
    }
}