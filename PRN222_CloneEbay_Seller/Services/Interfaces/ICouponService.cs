using PRN222_CloneEbay_Seller.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface ICouponService
    {
        Task<List<Coupon>> GetCouponsForSellerAsync(int sellerId);
        Task<Coupon> GetCouponByIdAsync(int couponId, int sellerId);
        Task<bool> CreateCouponAsync(Coupon coupon, int sellerId);
        Task<bool> UpdateCouponAsync(Coupon coupon, int sellerId);
        Task<bool> DeleteCouponAsync(int couponId, int sellerId);
        Task<List<Product>> GetProductsForSellerAsync(int sellerId);
    }
}