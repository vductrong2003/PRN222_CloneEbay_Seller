namespace PRN222_CloneEbay_Seller.Models
{
    public class PaymentTransactionViewModel
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string BuyerName { get; set; }
        public string Method { get; set; }
        public string Status { get; set; } // "Completed", "Refunded", etc.
        public decimal? Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
    }
}