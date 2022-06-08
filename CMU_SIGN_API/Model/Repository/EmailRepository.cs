
using CMU_SIGN_API.Model.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Threading.Tasks;

namespace CMU_SIGN_API.Model.Repository
{
    public class EmailRepository : IEmailRepository
    {
        public async Task SendEmailAsync(String nameSender, string email_To, string subject, string message, List<IFormFile> Attachment)
        {
            String _mailServer = Environment.GetEnvironmentVariable("MAIL_SERVER");
            Int32 _mailPort = Convert.ToInt32(Environment.GetEnvironmentVariable("MAIL_PORT"));
            String _mailSender = Environment.GetEnvironmentVariable("MAIL_SENDER");


            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(nameSender, _mailSender));
            emailMessage.To.Add(new MailboxAddress(" ", email_To));
            emailMessage.Subject = subject;
            var builder = new BodyBuilder { HtmlBody = message };
            if (Attachment != null)
            {
                foreach (IFormFile formFile in Attachment)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        await formFile.CopyToAsync(memoryStream);
                        builder.Attachments.Add(formFile.FileName, memoryStream.ToArray());
                    }
                }
            }
            emailMessage.Body = builder.ToMessageBody();
            await Task.Run(() =>
            {
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(_mailServer, _mailPort, SecureSocketOptions.Auto);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }
            });


        }

        public async Task SendEmailAsync(string nameSender, string reply, string email_To, string subject, string message, List<IFormFile> Attachment)
        {

            String _mailServer = Environment.GetEnvironmentVariable("MAIL_SERVER");
            Int32 _mailPort = Convert.ToInt32(Environment.GetEnvironmentVariable("MAIL_PORT"));
            String _mailSender = Environment.GetEnvironmentVariable("MAIL_SENDER");


            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(nameSender, _mailSender));
            emailMessage.ReplyTo.Add(new MailboxAddress("Reply-to", reply));
            emailMessage.To.Add(new MailboxAddress(" ", email_To));
            emailMessage.Subject = subject;
            var builder = new BodyBuilder { HtmlBody = message };
            if (Attachment != null)
            {
                foreach (IFormFile formFile in Attachment)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        await formFile.CopyToAsync(memoryStream);
                        builder.Attachments.Add(formFile.FileName, memoryStream.ToArray());
                    }
                }
            }
            emailMessage.Body = builder.ToMessageBody();
            await Task.Run(() =>
            {
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(_mailServer, _mailPort, SecureSocketOptions.Auto);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }
            });

        }
    }
}
