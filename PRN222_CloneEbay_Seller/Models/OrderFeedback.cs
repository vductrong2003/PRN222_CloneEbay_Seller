using System;
using System.ComponentModel.DataAnnotations;

namespace PRN222_CloneEbay_Seller.Models
{
    public partial class OrderFeedback
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int BuyerId { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual OrderTable Order { get; set; } = null!;
        public virtual User Buyer { get; set; } = null!;
        public virtual User Seller { get; set; } = null!;
    }
}