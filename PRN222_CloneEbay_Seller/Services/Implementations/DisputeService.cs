using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class DisputeService : IDisputeService
    {
        private readonly CloneEbayDbContext _context;
        private readonly IEmailService _emailService;

        public DisputeService(CloneEbayDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<DisputeViewModel>> GetDisputesForSellerAsync(int sellerId)
        {
            return await _context.Disputes
                .Where(d => d.Order.OrderItems.Any(oi => oi.Product.SellerId == sellerId))
                .Include(d => d.Order.Buyer)
                .Select(d => new DisputeViewModel
                {
                    DisputeId = d.Id,
                    OrderId = d.OrderId.Value,
                    BuyerName = d.Order.Buyer.Username,
                    DescriptionSnippet = d.Description.Length > 100 ? d.Description.Substring(0, 100) + "..." : d.Description,
                    Status = d.Status
                })
                .ToListAsync();
        }

        public async Task<Dispute> GetDisputeDetailsAsync(int disputeId, int sellerId)
        {
            var dispute = await _context.Disputes
                .Include(d => d.RaisedByNavigation)
                .Include(d => d.Order)
                    .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(d => d.Order)
                    .ThenInclude(o => o.Buyer)
                .FirstOrDefaultAsync(m => m.Id == disputeId);

            // Kiểm tra quyền sở hữu
            if (dispute == null || !dispute.Order.OrderItems.Any(oi => oi.Product.SellerId == sellerId))
            {
                return null;
            }

            // TỰ ĐỘNG CHUYỂN TRẠNG THÁI KHI NGƯỜI BÁN XEM LẦN ĐẦU
            if (dispute.Status?.ToLower() == "open")
            {
                dispute.Status = "In Progress";
                _context.Update(dispute);
                await _context.SaveChangesAsync();
            }

            return dispute;
        }

        public async Task<bool> ResolveDisputeAsync(int disputeId, string resolution, int sellerId)
        {
            var dispute = await _context.Disputes
                                        .Include(d => d.Order.Buyer)
                                        .Include(d => d.Order.OrderItems)
                                        .ThenInclude(oi => oi.Product)
                                        .FirstOrDefaultAsync(d => d.Id == disputeId);

            // Kiểm tra quyền sở hữu trước khi cập nhật
            if (dispute == null || !dispute.Order.OrderItems.Any(oi => oi.Product.SellerId == sellerId))
            {
                return false;
            }

            // 1. Cập nhật database
            dispute.Resolution = resolution;
            dispute.Status = "Resolved";
            _context.Update(dispute);
            await _context.SaveChangesAsync();

            // 2. Gửi email thông báo
            try
            {
                await _emailService.SendDisputeResolvedEmailAsync(
                    dispute.Order.Buyer.Email,
                    dispute.Order.Buyer.Username,
                    dispute.Order.Id,
                    resolution
                );
            }
            catch (System.Exception ex)
            {
                // Ghi lại lỗi gửi email nếu cần (logging)
                // Không nên để lỗi gửi email làm hỏng toàn bộ thao tác
                // Ví dụ: Console.WriteLine($"Failed to send email: {ex.Message}");
            }

            return true;
        }
    }
}