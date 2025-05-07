using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.Services;
using EBISX_POS.Services.DTO.Report;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class UserLogsViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTimeOffset fromDate = DateTime.Today;

        [ObservableProperty]
        private DateTimeOffset toDate = DateTime.Today;

        public IRelayCommand FilterCommand { get; }

        private readonly List<UserActionLogDTO> allUserLogs; // Temp until data is fetched

        [ObservableProperty]
        private bool isManagerLogs;

        [ObservableProperty]
        private bool isLoading = false;

        private const int PageSize = 10;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private ObservableCollection<UserActionLogDTO> paginatedLogsList = new();

        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }

        public UserLogsViewModel(bool isManagerLogs)
        {
            var reportService = App.Current.Services.GetRequiredService<ReportService>();
            allUserLogs = new List<UserActionLogDTO>(); // Temp until data is fetched

            FilterCommand = new RelayCommand(async () => await FilterByDateRange());
            NextPageCommand = new RelayCommand(GoToNextPage, CanGoToNextPage);
            PreviousPageCommand = new RelayCommand(GoToPreviousPage, CanGoToPreviousPage);

            IsManagerLogs = isManagerLogs;

            // Optionally fetch records for today as default
            _ = LoadLogsAsync(DateTime.Today, DateTime.Today);
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
            var paginated = allUserLogs
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            PaginatedLogsList = new ObservableCollection<UserActionLogDTO>(paginated);
            TotalPages = (int)Math.Ceiling((double)allUserLogs.Count / PageSize);

            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        private bool CanGoToPreviousPage() => CurrentPage > 1;
        private async Task LoadLogsAsync(DateTime from, DateTime to)
        {
            IsLoading = true;
            var reportService = App.Current.Services.GetRequiredService<ReportService>();
            var logs = await reportService.UserActionLog(IsManagerLogs, from, to);

            allUserLogs.Clear();
            allUserLogs.AddRange(logs);

            CurrentPage = 1;
            UpdatePaginatedList();

            IsLoading = false;
        }

        private async Task FilterByDateRange()
        {
            await LoadLogsAsync(FromDate.Date, ToDate.Date);
        }
    }
}