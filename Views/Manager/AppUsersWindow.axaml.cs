using Avalonia.Controls;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS;

public partial class AppUsersWindow : Window
{
    public AppUsersWindow()
    {
        InitializeComponent();
        
        // Get the data service from DI container
        var dataService = App.Current.Services.GetRequiredService<IData>();
        
        // Create and set the ViewModel
        DataContext = new AppUsersViewModel(dataService, this);
    }
}