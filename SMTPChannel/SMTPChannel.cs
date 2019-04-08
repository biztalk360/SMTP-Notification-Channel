#region Using Directives

using B360.Common.EntityObjects.Notifier;
using B360.Notifier.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Xml.Linq;
using System.Xml.XPath;


#endregion
namespace B360.Notifier.SMTP
{
    /// <summary>
    /// Notification channel for Teams
    /// </summary>
    /// <seealso cref="B360.Notifier.Common.IChannelNotification" />"System.Runtime.Serialization" -- This Assembly need to be added as a reference to this Project.
    public class SMTPChannel : IChannelNotification
    {
        /// <summary>
        /// Gets the global properties schema.
        /// </summary>
        /// <returns></returns>
        public string GetGlobalPropertiesSchema()
        {
            return Helper.GetResourceFileContent("GlobalProperties.xml");
        }

        /// <summary>
        /// Gets the alarm properties schema.
        /// </summary>
        /// <returns></returns>
        public string GetAlarmPropertiesSchema()
        {
            return Helper.GetResourceFileContent("AlarmProperties.xml");
        }

        /// <summary>
        /// Sends the notification.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="alarm">The alarm.</param>
        /// <param name="globalProperties">The global properties.</param>
        /// <param name="notifications">The notifications.</param>
        /// <returns></returns>
        public bool SendNotification(BizTalkEnvironment environment, Common.Alarm alarm, string globalProperties, Dictionary<MonitorGroupTypeName, MonitorGroupData> notifications)
        {
            try
            {
                LoggingHelper.Info("DESCRIPTION " + alarm.Description);
                XDocument globalDocument = XDocument.Parse(globalProperties);
                XDocument alarmDocument = XDocument.Parse(alarm.AlarmProperties);

                //Read configured properties
                XNamespace bsd = XNamespace.Get("http://www.biztalk360.com/alarms/notification/basetypes");
                string email = string.Empty;
                string emailTo = string.Empty;
                string ccEmail = string.Empty;
                string upAlert = string.Empty;
                string autoCorrectAlert = string.Empty;

                //Global Properties
                XDocument globalProps = XDocument.Load(new StringReader(globalProperties));

                foreach (XElement element in globalProps.Descendants(bsd + "TextArea"))
                {
                    XAttribute xAttribute = element.Attribute("Name");
                    if (element.Attribute("Name").Value == "Email-To")
                    {
                        emailTo = element.Attribute("Value").Value;
                    }
                    if (element.Attribute("Name").Value == "C-C")
                    {
                        ccEmail = element.Attribute("Value").Value;
                    }
                    if (element.Attribute("Name").Value == "Up-Alert")
                    {
                        upAlert = element.Attribute("Value").Value;
                    }
                    if (element.Attribute("Name").Value == "AutoCorrect-Alert")
                    {
                        autoCorrectAlert = element.Attribute("Value").Value;
                    }
                }
                bool useEmailTo = Convert.ToBoolean(globalDocument.XPathSelectElement(
                            "/*[local-name() = 'GlobalProperties']/*[local-name() = 'Section']/*[local-name() = 'CheckBox' and @Name = 'use-Email-To']")
                        ?.Attribute("Value")?.Value);

                BT360Helper helper = new BT360Helper(notifications, environment, alarm, MessageType.ConsolidatedMessage, MessageFormat.Text);

                EmailMessageBody emailMessageBody = (EmailMessageBody)helper.ConvertDictionaryToObject<EmailMessageBody>(alarm.AdditionalProperties, SMTPConfigurationInfo.EmailMessageBody);
                EmailAttachments emailAttachments = (EmailAttachments)helper.ConvertDictionaryToObject<EmailAttachments>(alarm.AdditionalProperties, SMTPConfigurationInfo.EmailAttachments);
                SMTPSetting smtpSetting = (SMTPSetting)helper.ConvertDictionaryToObject<SMTPSetting>(alarm.AdditionalProperties, SMTPConfigurationInfo.SMTPSetting);
                if (smtpSetting == null)
                    throw new Exception("SMTP Setting is not present in the database. Please use the UI to update the correct setting.");

                //When installed first time, these values will be blank
                if (smtpSetting.serverName == "" || smtpSetting.adminEmailAddress == "")
                {
                    //SK: 4th Feb 2012, replaced returning null with exception, since we can't proceed further.
                    throw new Exception("SMTP Setting is not configured (admin email or server name is blank). Please use the UI to update the correct setting.");
                    //return null;
                }
                switch (emailMessageBody.notificationType)
                {
                    case NotificationType.UpAlert:
                        if (upAlert != "" && useEmailTo)
                            email = upAlert;
                        else
                            email = emailTo;
                        break;
                    case NotificationType.AutoCorrectAlert:
                        if (autoCorrectAlert != "" && useEmailTo)
                            email = autoCorrectAlert;
                        else
                            email = emailTo;
                        break;
                    default:
                        email = emailTo;
                        break;
                }

                string strFrom = emailMessageBody.fromEmailAddress;
                string strName = emailMessageBody.fromDisplayName;
                string strTo = email;
                string strCC = ccEmail;
                string strSubject = emailMessageBody.subject;
                string strBody = emailMessageBody.xmlData;               
                LoggingHelper.Info("SUBJECT " + strSubject);
                try
                {

                    SmtpClient smtp = new SmtpClient();

                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Host = smtpSetting.serverName;
                    smtp.Port = Convert.ToInt32(smtpSetting.port);
                    smtp.EnableSsl = (smtpSetting.sslMode == SMTPSSLMode.DedicatedSSLPort) ? true : false;
                    smtp.UseDefaultCredentials = (smtpSetting.authentication == SMTPAuthentication.IntegratedWindowsAuthOverNtlm) ? true : false;
                    smtp.Credentials = (smtpSetting.authentication == SMTPAuthentication.Anonymous) ? null : new NetworkCredential(smtpSetting.userName, smtpSetting.password);

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    string htmlData = Helper.TransformXMLToHTML(emailMessageBody.xslt, emailMessageBody.xmlData);
                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(strFrom, strName);
                    mailMessage.Subject = strSubject;
                    mailMessage.Body = htmlData;
                    mailMessage.IsBodyHtml = true;
                    //Adding multiple To Addresses
                    foreach (string sTo in strTo.Split(";".ToCharArray()))
                        mailMessage.To.Add(sTo);

                    //Adding multiple CC Addresses
                    if (strCC != string.Empty)
                    {
                        foreach (string sCC in strCC.Split(";".ToCharArray()))
                            mailMessage.CC.Add(sCC);
                    }
                    int attachementCount = 0;
                    double totalSizeInMB = 0;
                    var attachmentsOrderedByImportance = emailAttachments.OrderByDescending(issue => issue.importance);

                    foreach (EmailAttachment attachment in attachmentsOrderedByImportance)
                    {
                        string fileName = Path.Combine(emailMessageBody.attachmentFolder, attachment.name);
                        if (File.Exists(fileName))
                        {

                            //Apply maximum size restriction rule
                            FileInfo f = new FileInfo(fileName);
                            long filesize = f.Length;
                            double sizeInMB = (filesize / 1024f) / 1024f;

                            if ((totalSizeInMB + sizeInMB) >= emailMessageBody.maxAttachmentsSizeInMb)
                                LoggingHelper.Warn(string.Format("The size of the attachment is above the configured limit. Alarm {0}. FileName :{1}. Configured Value :{2}. Current Limit: {3}", alarm.Name, attachment.name, emailMessageBody.maxAttachmentsSizeInMb, totalSizeInMB));
                            else if (attachementCount >= emailMessageBody.maxAttachments)  //Apply Maximum attachments/email rule
                                LoggingHelper.Warn(string.Format("Maximum attachments count reached for alarm {0}. Configured Value :{1}. Current Limit: {2}", alarm.Name, emailMessageBody.maxAttachments, attachementCount));
                            else
                            {
                                mailMessage.Attachments.Add(new Attachment(fileName));
                                totalSizeInMB += sizeInMB; // Attachment Size
                                attachementCount++; // Attachement Count
                            }
                        }
                        else
                            LoggingHelper.Warn(string.Format(" Attachment file {0} cannot be found.", attachment));
                    }


                    MailPriority mailPriority = MailPriority.Normal;
                    switch (alarm.EmailPriority)
                    {
                        case "1":
                            mailPriority = MailPriority.High;
                            break;
                        case "0":
                            mailPriority = MailPriority.Low;
                            break;
                    }
                    mailMessage.Priority = mailPriority;
                    smtp.Send(mailMessage);
                    LoggingHelper.Info("SMTP Notification channel posted the message successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    LoggingHelper.Error("SMTP Notification channel failed. Error " + ex.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.Error("SMTP Notification channel failed. Error " + ex.Message);
                return false;
            }
        }
    }
}
