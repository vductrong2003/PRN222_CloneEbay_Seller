namespace PRN222_CloneEbay_Seller.Models
{
    public class OrderPaymentStatusViewModel
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? OrderStatus { get; set; }
        public string PaymentStatus { get; set; } // "Paid" hoặc "Awaiting Payment"
    }
}