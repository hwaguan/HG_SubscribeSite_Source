using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace HG_Subscribe.Controllers
{
    public class SystemController : Controller
    {
        public async void sendMail(string url, string data)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("X-API-KEY", "NDAyODgxM2I4NmMyMjdkNzAxODZjNDU1ZjIyNjA2NzktMTY4MDA2MDkyNS0x");
                request.Headers.Add("Accept", "application/json");
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
            }
        }
    }
}