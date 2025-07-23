using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly CloneEbayDbContext _context;

        public PaymentService(CloneEbayDbContext context)
        {
            _context = context;
        }

        // Triển khai phương thức đã cập nhật
        public async Task<List<PaymentTransactionViewModel>> GetPaymentTransactionsForSellerAsync(int sellerId)
        {
            var transactions = await _context.Payments
                // Chỉ lấy các payment mà có đơn hàng chứa sản phẩm của người bán
                .Where(p => p.Order.OrderItems.Any(oi => oi.Product.SellerId == sellerId))
                // Include thông tin của đơn hàng và người mua để lấy tên
                .Include(p => p.Order)
                    .ThenInclude(o => o.Buyer)
                // Sắp xếp giao dịch mới nhất lên đầu
                .OrderByDescending(p => p.PaidAt)
                // Chọn và chuyển đổi dữ liệu sang ViewModel, đây là bước tối ưu hóa
                .Select(p => new PaymentTransactionViewModel
                {
                    PaymentId = p.Id,
                    OrderId = p.OrderId.Value,
                    // Lấy tên người mua từ thông tin đã include
                    BuyerName = p.Order.Buyer.Username,
                    Method = p.Method,
                    Status = p.Status,
                    Amount = p.Amount,
                    TransactionDate = p.PaidAt
                })
                .ToListAsync();

            return transactions;
        }
    }
}