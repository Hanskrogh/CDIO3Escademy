using System;
using System.Net;
using System.Net.Mail;

namespace Escademy.Models
{
    public class MailFactory : IDisposable
    {
        private SmtpClient client;
        
        public MailFactory()
        {
            client = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = GetCredentials(ConnectionString.Get("supportMail"))
            };    
            
        }
        

        public void SendMail(string subject, string body, MailAddress toAddress)
        {
            using (var message = new MailMessage(new MailAddress("noreply@escademy.com", "E-Sport Academy"), toAddress) { Subject = subject, Body = body, IsBodyHtml = true })
            {
                client.Send(message);
            }
        }

        private NetworkCredential GetCredentials(string mailConnString)
        {
            //                                             parts[0]   parts[1]
            //                                               |           |      
            //                                               V           V
            string[] parts = mailConnString.Split(',');   //username,password
            return new NetworkCredential(parts[0], parts[1]);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}