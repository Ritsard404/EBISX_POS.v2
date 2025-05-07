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
    public class OrderSummaryViewModel : ViewModelBase
    {

        // Expose the static current order item through a property.
        public OrderItemState CurrentOrderItem => OrderState.CurrentOrderItem;

        public ObservableCollection<OrderItemState> CurrentOrder { get; } = OrderState.CurrentOrder;

        public ObservableCollection<OrderItem> OrderItems { get; } = new ObservableCollection<OrderItem>
        {
            new OrderItem
            {
                ID = 1,
                Quantity = 1,
                SubOrder = new List<SubOrder>
                {
                    new SubOrder { ID = 1, Name = "Cheeseburger", Price = 75.00m },
                    new SubOrder { ID = 2, Name = "Extra Cheese", Price = 10.00m }
                }
            },
            new OrderItem
            {
                ID = 2,
                Quantity = 2,
                SubOrder = new List<SubOrder>
                {
                    new SubOrder { ID = 3, Name = "Fries", Size = "Large", Price = 50.00m },
                    new SubOrder { ID = 4, Name = "Ketchup Packet", Price = 5.00m }
                }
            },
            //new OrderItem
            //{
            //    ID = 3,
            //    Quantity = 1,
            //    SubOrder = new List<SubOrder>
            //    {
            //        new SubOrder { ID = 5, Name = "Soda", Size = "Medium", Price = 30.00m },
            //        new SubOrder { ID = 6, Name = "Ice Refill", Price = 5.00m }
            //    }
            //},
            //new OrderItem
            //{
            //    ID = 4,
            //    Quantity = 3,
            //    SubOrder = new List<SubOrder>
            //    {
            //        new SubOrder { ID = 7, Name = "Chicken Nuggets", Size = "6pcs", Price = 120.00m },
            //        new SubOrder { ID = 8, Name = "BBQ Sauce", Price = 10.00m },
            //        new SubOrder { ID = 9, Name = "Coke", Size = "Large", Price = 10.00m }
            //    }
            //},
            //new OrderItem
            //{
            //    ID = 5,
            //    Quantity = 1,
            //    SubOrder = new List<SubOrder>
            //    {
            //        new SubOrder { ID = 10, Name = "Vanilla Ice Cream", Price = 35.00m },
            //        new SubOrder { ID = 11, Name = "Chocolate Syrup", Price = 10.00m }
            //    }
            //},
            //new OrderItem
            //{
            //    ID = 6,
            //    Quantity = 2,
            //    SubOrder = new List<SubOrder>
            //    {
            //        new SubOrder { ID = 12, Name = "Spaghetti Meal", Price = 90.00m },
            //        new SubOrder { ID = 13, Name = "Garlic Bread", Price = 15.00m }
            //    }
            //},
            //new OrderItem
            //{
            //    ID = 7,
            //    Quantity = 1,
            //    SubOrder = new List<SubOrder>
            //    {
            //        new SubOrder { ID = 14, Name = "Coffee", Price = 40.00m },
            //        new SubOrder { ID = 15, Name = "Cream & Sugar", Price = 5.00m }
            //    }
            //},
            //new OrderItem
            //{
            //    ID = 8,
            //    Quantity = 2,
            //    SubOrder = new List<SubOrder>
            //    {
            //        new SubOrder { ID = 16, Name = "Chicken Meal", Size = "Regular", Price = 150.00m },
            //        new SubOrder { ID = 17, Name = "Rice", Price = 20.00m },
            //        new SubOrder { ID = 18, Name = "Gravy", Price = 10.00m }
            //    }
            //},
            //new OrderItem
            //{
            //    ID = 9,
            //    Quantity = 1,
            //    SubOrder = new List<SubOrder>
            //    {
            //        new SubOrder { ID = 19, Name = "Coke", Size = "Small", Price = 20.00m }
            //    }
            //}
        };


        public OrderSummaryViewModel()
        {
            // Subscribe to changes of the static property.
            OrderState.StaticPropertyChanged += OnOrderStateStaticPropertyChanged;

            OrderState.CurrentOrderItem.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentOrderItem));
            };


            OrderState.CurrentOrder.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentOrder));
            };

        }

        private void OnOrderStateStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderState.CurrentOrderItem))
            {
                OnPropertyChanged(nameof(CurrentOrderItem));
            }
        }
    }
}

