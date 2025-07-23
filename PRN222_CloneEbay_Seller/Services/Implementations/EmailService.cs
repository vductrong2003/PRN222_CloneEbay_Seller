using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendDisputeResolvedEmailAsync(string toEmail, string buyerName, int orderId, string resolution)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress(buyerName, toEmail));
            message.Subject = $"Update on your dispute for Order #{orderId}";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <p>Hello {buyerName},</p>
                <p>We're writing to inform you about an update on your dispute regarding order <strong>#{orderId}</strong>.</p>
                <p>The seller has provided the following resolution:</p>
                <div style='padding: 15px; border-left: 4px solid #ccc; background-color: #f5f5f5; margin: 15px 0;'>
                    <p><em>""{resolution}""</em></p>
                </div>
                <p>Please check your account for more details. Thank you for using CloneEbay.</p>
                <p>Best regards,<br/>The CloneEbay Team</p>";

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}