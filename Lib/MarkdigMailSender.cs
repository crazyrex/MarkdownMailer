namespace MarkdownMailer
{
    using System;
    using System.IO;
    using System.Net.Mail;
    using System.Net.Mime;
    using Markdig;

    public class MarkdigMailSender : IMailSender
    {
        readonly ISmtpClient smtpClient;

        public MarkdigMailSender()
            : this(new SmtpClientWrapper(new SmtpClient()), null)
        {
        }
        
        public MarkdigMailSender(MailSenderConfiguration configuration)
            : this(new SmtpClientWrapper(new SmtpClient()), configuration)
        {
        }

        public MarkdigMailSender(SmtpClient smtpClient)
            : this(new SmtpClientWrapper(smtpClient), null)
        {
        }

        internal MarkdigMailSender(
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

        public void Send(MailMessage mailMessage, MarkdownPipeline pipeline) {
            if (smtpClient.DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory
                && !Directory.Exists(smtpClient.PickupDirectoryLocation))
                Directory.CreateDirectory(smtpClient.PickupDirectoryLocation);

            if (pipeline == null)
            {
                pipeline = new MarkdownPipelineBuilder()
                    .UseSoftlineBreakAsHardlineBreak()
                    .UseAdvancedExtensions()
                    .Build();
            }

            string markdownBody = mailMessage.Body;
            string htmlBody = Markdown.ToHtml(markdownBody, pipeline);

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