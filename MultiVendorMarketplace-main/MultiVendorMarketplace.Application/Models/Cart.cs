namespace MultiVendorMarketplace.Application.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
        public int TotalItems => Items.Sum(i => i.Quantity);
    }
}
