using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.Services;
using EBISX_POS.Services.DTO.Report;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<InvoiceDTO> salesHistoryList = new();

        [ObservableProperty]
        private DateTimeOffset fromDate = DateTime.Today;

        [ObservableProperty]
        private DateTimeOffset toDate = DateTime.Today;

        public IRelayCommand FilterCommand { get; }

        private readonly List<InvoiceDTO> allSalesHistory; 
        
        [ObservableProperty]
        private InvoiceDTO? selectedInvoice;

        [ObservableProperty]
        private bool isLoading = false;

        private const int PageSize = 10;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private ObservableCollection<InvoiceDTO> paginatedSalesHistoryList = new();

        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }

        public SalesHistoryViewModel()
        {
            var reportService = App.Current.Services.GetRequiredService<ReportService>();

            allSalesHistory = new List<InvoiceDTO>(); // Temp until data is fetched

            FilterCommand = new RelayCommand(async () => await FilterByDateRange());
            NextPageCommand = new RelayCommand(GoToNextPage, CanGoToNextPage);
            PreviousPageCommand = new RelayCommand(GoToPreviousPage, CanGoToPreviousPage);

            // Optionally fetch records for today as default
            _ = LoadInvoicesAsync(DateTime.Today, DateTime.Today);
        }

        private void GoToNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePaginatedList();
            }
        }

        private void GoToPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePaginatedList();
            }
        }
        private void UpdatePaginatedList()
        {
            var paginated = allSalesHistory
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            PaginatedSalesHistoryList = new ObservableCollection<InvoiceDTO>(paginated);
            TotalPages = (int)Math.Ceiling((double)allSalesHistory.Count / PageSize);

            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        private bool CanGoToPreviousPage() => CurrentPage > 1;

        private async Task LoadInvoicesAsync(DateTime from, DateTime to)
        {
            IsLoading = true;
            var reportService = App.Current.Services.GetRequiredService<ReportService>();
            var invoices = await reportService.GetInvoicesByDateRange(from, to);

            allSalesHistory.Clear();
            allSalesHistory.AddRange(invoices ?? new List<InvoiceDTO>());

            CurrentPage = 1;
            UpdatePaginatedList();

            IsLoading = false;
        }

        private async Task FilterByDateRange()
        {
            await LoadInvoicesAsync(FromDate.Date, ToDate.Date);
        }
    }
}
