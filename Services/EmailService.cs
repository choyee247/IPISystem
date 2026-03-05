using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ProjectManagementSystem.DBModels;

namespace ProjectManagementSystem.Services
{
    public enum EmailProvider
    {
        MailKit,
        SmtpClient
    }
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly EmailProvider _provider;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _provider = Enum.Parse<EmailProvider>(_config["EmailSettings:Provider"]);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (_provider == EmailProvider.MailKit)
            {
                await SendWithMailKitAsync(toEmail, subject, body);
            }
            else
            {
                await SendWithSmtpClientAsync(toEmail, subject, body);
            }
        }

        public Task SendEmailAsync(Email emailPk, string v1, string v2)
        {
            throw new NotImplementedException();
        }

        private async Task SendWithMailKitAsync(string toEmail, string subject, string body)
        {
            var email = _config["EmailSettings:From"];
            var password = _config["EmailSettings:Password"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Project System", email));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(email, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        private async Task SendWithSmtpClientAsync(string toEmail, string subject, string body)
        {
            var host = _config["EmailSettings:SmtpHost"];
            var port = int.Parse(_config["EmailSettings:SmtpPort"]);
            var user = _config["EmailSettings:From"];
            var pass = _config["EmailSettings:Password"];

            var message = new System.Net.Mail.MailMessage();
            message.From = new MailAddress(user);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = false;

            using (var client = new System.Net.Mail.SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(user, pass);
                client.EnableSsl = true;
                await client.SendMailAsync(message);
            }
        }
    }
}
