using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace PRN222_CloneEbay_Seller.Models
{
    public class CouponViewModel
    {
        public Coupon Coupon { get; set; }
        public SelectList Products { get; set; }
    }
}