using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace UPCOR.Core
{
    public class Support
    {
        public static void SendMail(string to, string subject, string body) {
            MailMessage mail = new MailMessage(new MailAddress("support@tillsynen.se"), new MailAddress(to));
            SmtpClient client = new SmtpClient();
            client.Port = 24;
            client.Host = "secure.waspmail.com";
            client.Timeout = 10000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential("support@jerntorget.net", "51bY2la");
            mail.Subject = subject;
            mail.Body = body;
            client.Send(mail);
        }
    }
}
