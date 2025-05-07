using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace EBISX_POS.Models
{
    public partial class OrderItemState : ObservableObject
    {
        private static long _counter = 0;
        public string ID { get; set; }  // ID is set in constructor, no change notification needed

        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private string? orderType;

        [ObservableProperty]
        private bool hasCurrentOrder;

        [ObservableProperty]
        private decimal totalPrice;

        public decimal TotalDiscountPrice { get; set; }
        public bool HasDiscount { get; set; }
        public bool HasPwdScDiscount { get; set; }
        public bool IsEnableEdit { get; set; } = true;
        public bool IsPwdDiscounted { get; set; } = false;
        public bool IsSeniorDiscounted { get; set; } = false;
        public decimal? PromoDiscountAmount { get; set; }
        public string? CouponCode { get; set; }

        // Using ObservableCollection so UI is notified on add/remove.
        [ObservableProperty]
        private ObservableCollection<SubOrderItem> subOrders = new ObservableCollection<SubOrderItem>();

        // Computed property: UI must be notified manually if you need it to update dynamically.
        public ObservableCollection<SubOrderItem> DisplaySubOrders =>
            new ObservableCollection<SubOrderItem>(subOrders
                .Select((s, index) => new SubOrderItem
                {
                    MenuId = s.MenuId,
                    DrinkId = s.DrinkId,
                    AddOnId = s.AddOnId,
                    Name = s.Name,
                    ItemPrice = s.ItemPrice,
                    Size = s.Size,
                    IsFirstItem = index == 0, // True for the first item
                    Quantity = Quantity,  // Only show Quantity for the first item
                    IsOtherDisc = s.IsOtherDisc
                }));

        public OrderItemState()
        {
            RegenerateID();
            subOrders.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(DisplaySubOrders));
                UpdateTotalPrice();
            };
        }

        // Call this to generate a new unique ID.
        public void RegenerateID()
        {
            long ticks = DateTime.UtcNow.Ticks; // high resolution
            long count = Interlocked.Increment(ref _counter);
            ID = $"{ticks}-{Guid.NewGuid().ToString()}-{count}";
        }

        partial void OnQuantityChanged(int oldValue, int newValue)
        {
            OnPropertyChanged(nameof(DisplaySubOrders));
            OnPropertyChanged(nameof(Quantity));
            UpdateTotalPrice();
        }

        public void RefreshDisplaySubOrders(bool regenerateId = false)
        {
            if (regenerateId)
            {
                RegenerateID();
            }

            OnPropertyChanged(nameof(DisplaySubOrders));
            UpdateTotalPrice();
            UpdateHasCurrentOrder();
        }

        private void UpdateHasCurrentOrder()
        {
            HasCurrentOrder = SubOrders.Any();
        }
        private void UpdateTotalPrice()
        {
            TotalPrice = DisplaySubOrders
            .Where(i => !(i.AddOnId == null && i.MenuId == null && i.DrinkId == null))
            .Sum(p => p.ItemSubTotal);

        }

    }

    public class SubOrderItem
    {
        public int? MenuId { get; set; }
        public int? DrinkId { get; set; }
        public int? AddOnId { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal ItemPrice { get; set; } = 0;
        public decimal ItemSubTotal => AddOnId == null && MenuId == null && DrinkId == null
            ? ItemPrice :
            ItemPrice * Quantity;
        public string? Size { get; set; }

        public bool IsFirstItem { get; set; } = false;
        public bool IsOtherDisc { get; set; }
        public int Quantity { get; set; } = 0; // Store Quantity for first item

        public string DisplayName
        {
            get
            {
                // If size is null or whitespace, safeSize will be null.
                var safeSize = string.IsNullOrWhiteSpace(Size) ? null : Size.Trim();

                // If no IDs are set, return just the name.
                if (MenuId == null && DrinkId == null && AddOnId == null)
                {
                    return Name;
                }

                // If item has a positive price:
                if (ItemPrice > 0)
                {
                    // If there's a valid size, include it; otherwise, omit the size.
                    return string.IsNullOrEmpty(safeSize)
                        ? $"{Name} @{ItemPrice:G29}"
                        : $"{Name} ({safeSize}) @{ItemPrice:G29}";
                }

                // Fallback: if no price is present, return name with size (if available).
                return string.IsNullOrEmpty(safeSize)
                    ? Name
                    : $"{Name} ({safeSize})";
            }
        }



        // Opacity Property (replaces a converter)
        public double Opacity => IsFirstItem ? 1.0 : 0.0;

        public bool IsUpgradeMeal => ItemPrice > 0;

        public string ItemPriceString =>
        IsOtherDisc
            ? $"{ItemPrice:0}%"                                                   // 1) explicit “other” → percent
            : (MenuId == null && DrinkId == null && AddOnId == null)
                ? $" ₱{ItemSubTotal:G29}"                                         // 2) pure‑discount & not other → negative ₱
                : IsFirstItem
                    ? $"₱{ItemSubTotal:G29}"                                      // 3) first item → positive ₱
                    : IsUpgradeMeal
                        ? $"+ ₱{ItemSubTotal:G29}"                                // 4) upgrade → +₱
                        : $"- ₱{ItemSubTotal:G29}";
    }
}
