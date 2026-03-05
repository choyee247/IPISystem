using ProjectManagementSystem.DBModels;
using System.Threading.Tasks;

namespace ProjectManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailAsync(Email emailPk, string v1, string v2);
    }
}
