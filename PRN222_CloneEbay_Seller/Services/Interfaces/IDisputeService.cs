using PRN222_CloneEbay_Seller.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IDisputeService
    {
        /// <summary>
        /// Lấy danh sách tóm tắt các khiếu nại cho một người bán.
        /// </summary>
        Task<List<DisputeViewModel>> GetDisputesForSellerAsync(int sellerId);

        /// <summary>
        /// Lấy thông tin chi tiết của một khiếu nại, đồng thời kiểm tra quyền sở hữu.
        /// </summary>
        Task<Dispute> GetDisputeDetailsAsync(int disputeId, int sellerId);

        /// <summary>
        /// Cập nhật giải pháp cho một khiếu nại.
        /// </summary>
        Task<bool> ResolveDisputeAsync(int disputeId, string resolution, int sellerId);
    }
}