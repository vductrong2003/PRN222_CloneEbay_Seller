namespace PRN222_CloneEbay_Seller.Models
{
    public class DisputeViewModel
    {
        public int DisputeId { get; set; }
        public int OrderId { get; set; }
        public string? BuyerName { get; set; }
        public string? DescriptionSnippet { get; set; }
        public string? Status { get; set; }
        // Lưu ý: Bảng Dispute của bạn không có ngày tháng. Nếu có, chúng ta sẽ thêm vào đây.
    }
}