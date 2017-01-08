using System;
using System.Net.Mail;
using MarkdownSharp;
using System.Net.Mime;
using System.IO;

namespace MarkdownMailer
{
    public class MarkdownSharpMailSender : IMailSender
    {
        readonly ISmtpClient smtpClient;

        public MarkdownSharpMailSender()
            : this(new SmtpClientWrapper(new SmtpClient()), null)
        {
        }
        
        public MarkdownSharpMailSender(MailSenderConfiguration configuration)
            : this(new SmtpClientWrapper(new SmtpClient()), configuration)
        {
        }

        public MarkdownSharpMailSender(SmtpClient smtpClient)
            : this(new SmtpClientWrapper(smtpClient), null)
        {
        }

        internal MarkdownSharpMailSender(
            ISmtpClient smtpClient,
            MailSenderConfiguration configuration)
        {
            if (smtpClient == null)
                throw new ArgumentNullException("smtpClient");
            
            if (configuration != null)
                ConfigureSmtpClient(smtpClient, configuration);

            this.smtpClient = smtpClient;
        }

        static internal void ConfigureSmtpClient(
            ISmtpClient smtpClient, 
            MailSenderConfiguration configuration)
        {
            if (configuration.Host != null)
                smtpClient.Host = configuration.Host;
            if (configuration.Port.HasValue)
                smtpClient.Port = configuration.Port.Value;
            if (configuration.EnableSsl.HasValue)
                smtpClient.EnableSsl = configuration.EnableSsl.Value;
            if (configuration.DeliveryMethod.HasValue)
                smtpClient.DeliveryMethod = configuration.DeliveryMethod.Value;
            if (configuration.UseDefaultCredentials.HasValue)
                smtpClient.UseDefaultCredentials = configuration.UseDefaultCredentials.Value;
            if (configuration.Credentials != null)
                smtpClient.Credentials = configuration.Credentials;
            if (configuration.PickupDirectoryLocation != null)
                smtpClient.PickupDirectoryLocation = configuration.PickupDirectoryLocation;
        }

        public void Send(
            string fromAddress,
            string toAddress,
            string subject,
            string markdownBody) {
                Send(
                    new MailAddress(fromAddress),
                    new MailAddress(toAddress),
                    subject,
                    markdownBody);
        }
        
        public void Send(
            MailAddress fromAddress, 
            MailAddress toAddress, 
            string subject, 
            string markdownBody)
        {
            var mailMessage = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject, 
                Body = markdownBody
            };

            Send(mailMessage);
        }

        public void Send(MailMessage mailMessage)
        {
            Send(mailMessage, null);
        }

        public void Send(MailMessage mailMessage, Markdown markdownGenerator) {
            if (smtpClient.DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory
                && !Directory.Exists(smtpClient.PickupDirectoryLocation))
                Directory.CreateDirectory(smtpClient.PickupDirectoryLocation);

            if (markdownGenerator == null)
            {
                markdownGenerator = new Markdown();
            }

            string markdownBody = mailMessage.Body;
            string htmlBody = markdownGenerator.Transform(markdownBody);

            AlternateView textView = AlternateView.CreateAlternateViewFromString(
                markdownBody, 
                null, 
                MediaTypeNames.Text.Plain);
            mailMessage.AlternateViews.Add(textView);

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(
                htmlBody, 
                null, 
                MediaTypeNames.Text.Html);
            mailMessage.AlternateViews.Add(htmlView);
            
            smtpClient.Send(mailMessage);
        }

        public void Dispose()
        {
            smtpClient.Dispose();
        }
    }
}
