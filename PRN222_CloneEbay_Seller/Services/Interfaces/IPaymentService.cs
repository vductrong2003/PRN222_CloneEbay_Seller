using PRN222_CloneEbay_Seller.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IPaymentService
    {
        // Sửa phương thức cũ để trả về ViewModel mới
        Task<List<PaymentTransactionViewModel>> GetPaymentTransactionsForSellerAsync(int sellerId);
    }
}