﻿// -----------------------------------------------------------------------
// <copyright file="SmtpMailDeliveryService.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;

namespace BugNET.BLL.Notifications
{
    using System;
    using System.Net.Mail;
    using System.Net;
    using BugNET.Common;
    using System.Threading;
    using System.ComponentModel;
    using log4net;
    using MailboxSender;

    public class SmtpMailDeliveryService : IMailDeliveryService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SmtpMailDeliveryService));

        /// <summary>
        /// Sends the specified recipient email.
        /// </summary>
        /// <param name="recipientEmail">The recipient email.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public async Task Send(string recipientEmail, MailMessage message, int? relatedIssueId)
        {
            bool allowReplyTo = HostSettingManager.Get<bool>(HostSettingNames.Pop3AllowReplyToEmail, false);
            message.To.Clear();
            message.To.Add(recipientEmail);

            if (allowReplyTo && relatedIssueId.HasValue)
            {
                int at = HostSettingManager.HostEmailAddress.IndexOf("@");
                string issueCode = string.Format("+iid-{0}", relatedIssueId.Value);
                message.From = new MailAddress(HostSettingManager.HostEmailAddress.Insert(at, issueCode), HostSettingManager.ApplicationTitle);
            }
            else
            {
                message.From = new MailAddress(HostSettingManager.HostEmailAddress, HostSettingManager.ApplicationTitle);
            }


            var smtpServer = HostSettingManager.SmtpServer;
            var smtpPort = int.Parse(HostSettingManager.Get(HostSettingNames.SMTPPort));
            var smtpAuthentictation = Convert.ToBoolean(HostSettingManager.Get(HostSettingNames.SMTPAuthentication));
            var smtpUseSSL = Boolean.Parse(HostSettingManager.Get(HostSettingNames.SMTPUseSSL));

            // Only fetch the password if you need it
            var smtpUsername = string.Empty;
            var smtpPassword = string.Empty;
            var smtpDomain = string.Empty;

            if (smtpAuthentictation)
            {
                smtpUsername = HostSettingManager.Get(HostSettingNames.SMTPUsername, string.Empty);
                smtpPassword = HostSettingManager.Get(HostSettingNames.SMTPPassword, string.Empty);
                smtpDomain = HostSettingManager.Get(HostSettingNames.SMTPDomain, string.Empty);
            }
            await Task.Run(() =>
            {
                try
                {
                     Exception ex= EasySender.SendEmail(message.From.Address, message.To.ToString(),
                     smtpUseSSL, smtpServer, smtpPort,
                     smtpAuthentictation, smtpUsername, smtpPassword, smtpDomain, message.Subject, message.Body,
                     message.IsBodyHtml,
                     (s, e) =>
                     {
                         if (e.Error != null)
                         {
                             // log the error message
                             Log.Error(e.Error);
                         }
                     }
                     );
                    if (ex!=null)
                    {
                        throw ex;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            });

        }
    }
}
