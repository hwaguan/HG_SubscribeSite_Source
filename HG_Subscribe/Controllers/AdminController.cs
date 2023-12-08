using HG_Subscribe.Models;
using HGCryptor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace HG_Subscribe.Controllers
{
    public class AdminController : Controller
    {
        private static Cryptor cryptor = new Cryptor();
        private static Models.ClikGoEntities db = new Models.ClikGoEntities();

        [HttpPost]
        public string greeting(string name)
        {
            return "Hellow " + name;
        }

        // GET: Admin
        [HttpPost]
        public string login(string account, string password, string token)
        {
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            string acc = cryptor.encryptData(account);
            string pwd = cryptor.encryptData(password);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            List<administrator> admin = db.administrator.Where(a => a.admAccount == acc && a.admPassword == pwd).ToList();

            if (admin.Count > 0)
            {
                result.result = true;
                result.code = 200;
                result.message = admin;
            }

            //string resultStr = JsonConvert.SerializeObject(result);
            //resultStr = ((resultStr.Replace("\\\"", "\"")).Replace("\"[", "[")).Replace("]\"", "]");

            return JsonConvert.SerializeObject(result);
        }
    }
}