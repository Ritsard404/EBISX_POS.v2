using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using EBISX_POS.Models;
using EBISX_POS.Services;
using EBISX_POS.State;

namespace EBISX_POS.ViewModels
{
    public partial class OrderSummaryViewModel : ViewModelBase
    {
        // Expose the static current order item through a property.
        public OrderItemState CurrentOrderItem => OrderState.CurrentOrderItem;

        public ObservableCollection<OrderItemState> CurrentOrder { get; } = OrderState.CurrentOrder;

        [ObservableProperty]
        private string totalAmount = "₱ 0.00";

        public OrderSummaryViewModel()
        {
            // Subscribe to changes of the static property.
            OrderState.StaticPropertyChanged += OnOrderStateStaticPropertyChanged;

            OrderState.CurrentOrderItem.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentOrderItem));
                UpdateTotalAmount();
            };

            OrderState.CurrentOrder.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentOrder));
                UpdateTotalAmount();
            };

            // Initial total amount update
            UpdateTotalAmount();
        }

        private void UpdateTotalAmount()
        {
            TenderState.tenderOrder.CalculateTotalAmount();
            TotalAmount = $"₱ {TenderState.tenderOrder.TotalAmount:N2}";
        }

        private void OnOrderStateStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderState.CurrentOrderItem))
            {
                OnPropertyChanged(nameof(CurrentOrderItem));
                UpdateTotalAmount();
            }
        }
    }
}

