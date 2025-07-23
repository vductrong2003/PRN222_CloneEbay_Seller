using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendDisputeResolvedEmailAsync(string toEmail, string buyerName, int orderId, string resolution);
    }
}