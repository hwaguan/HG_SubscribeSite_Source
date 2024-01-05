using HG_Subscribe.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.HtmlControls;

namespace HG_Subscribe.Controllers
{
    public class MailController : Controller
    {
        private static Models.ClikGoEntities db = new Models.ClikGoEntities();
        private static Models.HGEntities dbHG = new Models.HGEntities();
        public class mailSender
        {
            static mailObj mailBody;
            private string serverURL = "https://mail.surenotifyapi.com/v1/messages";
            private string clientKey = "NDAyODgxM2I4NmMyMjdkNzAxODZjNDU1ZjIyNjA2NzktMTY4MDA2MDkyNS0x";
            public mailSender(string senderName, string senderMail, List<mailReceiver> receiver, string subject, string content)
            {
                mailBody = new mailObj();
                mailBody.fromName = senderName;
                mailBody.fromAddress = senderMail;
                mailBody.subject = subject;
                mailBody.content = content;
                mailBody.recipients = receiver;
            }

            public void addReceiver(string receieverName, string receieverMail)
            {
                mailBody.recipients.Add(new mailReceiver(receieverName, receieverMail));
            }

            public async Task<string> send()
            {
                string data = JsonConvert.SerializeObject(mailBody);

                using (HttpClient client = new HttpClient())
                {
                    using (ClikGoEntities db = new ClikGoEntities())
                    {
                        mailSendingLog MSL = new mailSendingLog();
                        MSL.slSenderMail = mailBody.fromAddress;
                        MSL.slReceiverMail = mailBody.recipients[0].address;
                        MSL.slSubject = mailBody.subject;
                        MSL.slContent = mailBody.content;
                        MSL.slSendingTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        MSL.slStatus = 0;
                        MSL.slType = "註冊完成通知";
                        db.mailSendingLog.Add(MSL);
                        db.SaveChanges();

                        var request = new HttpRequestMessage(HttpMethod.Post, serverURL);
                        request.Headers.Add("X-API-KEY", clientKey);
                        request.Headers.Add("Accept", "application/json");
                        request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                        var response = await client.SendAsync(request);
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }

            public class mailReceiver
            {
                public string name { get; set; }
                public string address { get; set; }
                public class variables
                {
                    public string UUID = "1234-56-78-9012";
                }

                public mailReceiver(string name, string mail)
                {
                    this.name = name;
                    this.address = mail;
                }
            }

            public class mailObj
            {
                public string fromName { get; set; }
                public string fromAddress { get; set; }
                public string subject { get; set; }
                public string content { get; set; }
                public List<mailReceiver> recipients { get; set; }
            }
        }
    }
}