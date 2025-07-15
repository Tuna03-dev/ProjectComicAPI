using System.Net;
using System.Net.Mail;

namespace DotNetTruyen.Services
{

    public class EmailService
    {
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendEmailAsync(string receptor, string subject, string body)
        {
            var displayName = configuration.GetValue<string>("EMAIL_CONFIGURATION:DISPLAYNAME");
            var email = configuration.GetValue<string>("EMAIL_CONFIGURATION:EMAIL");
            var password = configuration.GetValue<string>("EMAIL_CONFIGURATION:PASSWORD");
            var host = configuration.GetValue<string>("EMAIL_CONFIGURATION:HOST");
            var port = configuration.GetValue<int>("EMAIL_CONFIGURATION:PORT");

            var smtpClient = new SmtpClient(host, port);
            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;

            smtpClient.Credentials = new NetworkCredential(email, password);

            var message = new MailMessage();
            message.From = new MailAddress(email!, displayName);
            message.To.Add(receptor);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = true;

            try
            {
                await smtpClient.SendMailAsync(message);
            }
            finally
            {
                message.Dispose();
                smtpClient.Dispose();
            }
        }


    }
}
