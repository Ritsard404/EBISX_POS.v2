﻿using Avalonia.Controls;
using EBISX_POS.API.Services.DTO.Order;
using EBISX_POS.Models;
using EBISX_POS.Services;
using EBISX_POS.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EBISX_POS.State
{
    public static class OrderState
    {
        public static ObservableCollection<OrderItemState> CurrentOrder { get; set; } = new ObservableCollection<OrderItemState>();
        //public static ObservableCollection<OrderItem> OrderItems = new ObservableCollection<OrderItem>();
        //public static OrderItemState CurrentOrderItem { get; set; } = new OrderItemState();

        private static OrderItemState _currentOrderItem = new OrderItemState();
        public static OrderItemState CurrentOrderItem
        {
            get => _currentOrderItem;
            set
            {
                if (_currentOrderItem != value)
                {
                    _currentOrderItem = value;
                    OnStaticPropertyChanged(nameof(CurrentOrderItem));
                    OnStaticPropertyChanged(nameof(CurrentOrderItem.Quantity));
                }
            }
        }

        // Static event to notify when static properties change
        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;

        private static void OnStaticPropertyChanged(string propertyName)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }


        public static void UpdateItemOrder(string itemType, int itemId, string name, decimal price, string? size, bool? hasDrink, bool? hasAddOn)
        {
            // Determine the correct predicate based on the item type.
            Func<SubOrderItem, bool> predicate = itemType switch
            {
                "Drink" => c => c.DrinkId != null,
                "Add-On" => c => c.AddOnId != null,
                "Menu" => c => c.MenuId != null,
            };

            // Look for an existing sub-order that matches the predicate.
            var item = CurrentOrderItem.SubOrders.FirstOrDefault(predicate);
            CurrentOrderItem.HasDrinks = hasDrink ?? false;
            CurrentOrderItem.HasAddOns = hasAddOn ?? false;

            if (item != null)
            {
                // Update the existing sub-order.
                item.Name = name;
                item.ItemPrice = price;
                item.Size = size;
                

                // Optionally update the ID field, if needed.
                if (itemType == "Drink")
                    item.DrinkId = itemId;
                else if (itemType == "Add-On")
                    item.AddOnId = itemId;
                else if (itemType == "Menu")
                    item.MenuId = itemId;

                CurrentOrderItem.RefreshDisplaySubOrders();
            }
            else
            {
                // No matching sub-order exists, so create and add a new one.
                var newItem = new SubOrderItem
                {
                    Name = name,
                    ItemPrice = price,
                    Size = size
                };

                if (itemType == "Drink")
                    newItem.DrinkId = itemId;
                else if (itemType == "Add-On")
                    newItem.AddOnId = itemId;
                else
                    newItem.MenuId = itemId;

                CurrentOrderItem.SubOrders.Add(newItem);
                CurrentOrderItem.RefreshDisplaySubOrders();
            }

            //DisplayCurrentOrder();

        }

        public static async Task<bool> FinalizeCurrentOrder(bool isSolo, Window owner)
        {
            var orderService = App.Current.Services.GetRequiredService<OrderService>();

            var subOrders = CurrentOrderItem?.DisplaySubOrders;

            var hasDrinks = CurrentOrderItem?.HasDrinks;
            var hasAddOns = CurrentOrderItem?.HasAddOns;

            var isNoDrinks = CurrentOrderItem.SubOrders
                .All(s => s.DrinkId == null);
            var isNoAddOn = CurrentOrderItem.SubOrders
                .All(s => s.AddOnId == null);

            if (!isSolo
                && (
                     (hasDrinks == true && isNoDrinks)
                     ||
                     (hasAddOns == true && isNoAddOn)
                   )
            )
            //if (!isSolo && (isNoDrinks || isNoAddOn))
            {

                //var parentWindow = this.VisualRoot as Window; // Find the parent window

                //var swipeManager = new ManagerSwipeWindow(header: "Required Drink/Side!", message: "Please select a drink/side.", ButtonName: "Ok");
                //var (sucess, email) = await swipeManager.ShowDialogAsync(owner);

                //return false;

                var box = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentHeader = "Required Drink/Side!",
                        ContentMessage = "Please select a drink/side.",
                        ButtonDefinitions = ButtonEnum.Ok, // Defines the available buttons
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = false,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Width = 400,
                        ShowInCenter = true,
                        Icon = Icon.Info,
                        SystemDecorations = SystemDecorations.None,
                    });


                var result = await box.ShowAsPopupAsync(owner);
                switch (result)
                {
                    case ButtonResult.Ok:
                        return false;
                    default:
                        return false;
                };

            };


            // Add the current order item to the collection
            CurrentOrder.Add(CurrentOrderItem);

            var newOrderItem = new AddOrderDTO
            {
                qty = CurrentOrderItem.Quantity,
                entryId = CurrentOrderItem.ID,
                menuId = subOrders?.FirstOrDefault(m => m.MenuId != null)?.MenuId ?? 0,
                drinkId = subOrders?.FirstOrDefault(m => m.DrinkId != null)?.DrinkId ?? 0,
                addOnId = subOrders?.FirstOrDefault(m => m.AddOnId != null)?.AddOnId ?? 0,
                drinkPrice = subOrders?.FirstOrDefault(m => m.DrinkId != null)?.Quantity > 0 ? subOrders?.FirstOrDefault(m => m.DrinkId != null)?.ItemPrice : 0,
                addOnPrice = subOrders?.FirstOrDefault(m => m.AddOnId != null)?.Quantity > 0 ? subOrders?.FirstOrDefault(m => m.AddOnId != null)?.ItemPrice : 0,
                cashierEmail = CashierState.CashierEmail ?? ""
            };

            // Reset the current order item to a new instance for the next order\
            CurrentOrderItem = new OrderItemState();

            // Optionally, notify any subscribers that the current order item has changed
            CurrentOrderItem.RefreshDisplaySubOrders(true);

            // Call the AddOrderItem method.
            var (isSuccess, message) = await orderService.AddOrderItem(newOrderItem);


            if (isSuccess)
            {
                // Reset the current order item to a new instance for the next order\
                //CurrentOrderItem = new OrderItemState();

                //// Optionally, notify any subscribers that the current order item has changed
                //CurrentOrderItem.RefreshDisplaySubOrders(true);
                OnStaticPropertyChanged(nameof(CurrentOrderItem));
                OnStaticPropertyChanged(nameof(CurrentOrder));

                return true;
            }
            else
            {
                Debug.WriteLine($"Error: {message}");
                return false;
            }
        }

        public static async void VoidCurrentOrder(OrderItemState orderItem, string managerEmail)
        {
            // Retrieve the OrderService instance from the DI container.
            var orderService = App.Current.Services.GetRequiredService<OrderService>();

            var voidOrder = CurrentOrder.FirstOrDefault(i => i.ID == orderItem.ID);

            var entryId = new VoidOrderItemDTO()
            {
                entryId = orderItem.ID,
                managerEmail = managerEmail,
                cashierEmail = CashierState.CashierEmail ?? ""

            };

            // Call the AddOrderItem method.
            var (isSuccess, message) = await orderService.VoidOrderItem(entryId);
            if (!isSuccess)
            {
                Debug.WriteLine($"Error: {message}");
                return;
            }

            CurrentOrder.Remove(voidOrder);

            OnStaticPropertyChanged(nameof(CurrentOrder));
        }

        public static void DisplayCurrentOrder()
        {
            Debug.WriteLine("=== Current Order Details ===");

            if (CurrentOrder.Count == 0)
            {
                Debug.WriteLine("No orders found.");
                return;
            }

            foreach (var order in CurrentOrder)
            {
                Debug.WriteLine($"Order ID: {order.ID}");
                Debug.WriteLine($"Order Type: {order.OrderType}");
                Debug.WriteLine($"Quantity: {order.Quantity}");
                Debug.WriteLine($"Total Price: ₱{order.TotalPrice:F2}");
                Debug.WriteLine($"Total Discount Price: ₱{order.TotalDiscountPrice:F2}");
                Debug.WriteLine($"Has Current Order: {order.HasCurrentOrder}");
                Debug.WriteLine("Sub-Orders:");

                if (order.SubOrders.Count == 0)
                {
                    Debug.WriteLine("  No sub-orders.");
                }
                else
                {
                    foreach (var subOrder in order.SubOrders)
                    {
                        Debug.WriteLine($"  - Name: {subOrder.Name}");
                        Debug.WriteLine($"    Menu ID: {subOrder.MenuId}");
                        Debug.WriteLine($"    Drink ID: {subOrder.DrinkId}");
                        Debug.WriteLine($"    AddOn ID: {subOrder.AddOnId}");
                        Debug.WriteLine($"    Price: ₱{subOrder.ItemPrice:F2}");
                        Debug.WriteLine($"    Quantity: {subOrder.Quantity}");
                        Debug.WriteLine($"    SubTotal: ₱{subOrder.ItemSubTotal:F2}");
                        Debug.WriteLine($"    Size: {subOrder.Size}");
                        Debug.WriteLine($"    Is First Item: {subOrder.IsFirstItem}");
                        Debug.WriteLine("------------------------------------");
                    }
                }

                Debug.WriteLine("===================================");
            }
        }
    }
}
