using HG_Subscribe.Models;
using HGCryptor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace HG_Subscribe.Controllers
{
    public class MemberController : Controller
    {
        private static Cryptor cryptor = new Cryptor();
        private static Models.ClikGoEntities db = new Models.ClikGoEntities();
        private static Models.HGEntities dbHG = new Models.HGEntities();

        [HttpPost]
        public string login(string account, string password, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            string acc = cryptor.encryptData(account);
            string psw = cryptor.encryptData(password);

            member user = db.member.Where(m => m.mMail == acc || m.mMobile == acc && m.mPassword == psw).FirstOrDefault();

            if (user != null)
            {
                result.code = 200;
                result.result = true;
                result.message = user;
            }
            else
            {
                result.code = 0;
                result.result = false;
                result.message = string.Format("user with {0} not found!", account);
            }

            return JsonConvert.SerializeObject(result);
        }

        [HttpPost]
        public string googleLogin(string CID, string CName, string CMail, string CPic, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();
            string cid = cryptor.encryptData(CID);

            member user = db.member.Where(m => m.mGoogleAccount == cid).FirstOrDefault();

            if (user == null)
            {
                member newUser = new member();

                newUser.mName = CName;
                newUser.mMail = CMail;
                newUser.mEnabled = 1;
                newUser.mGoogleAccount = cid;
                newUser.mGoogleIcon = CPic;
                newUser.mPassword = "";
                newUser.mAddDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                db.member.Add(newUser);
                db.SaveChanges();

                user = newUser;
            }

            result.code = 200;
            result.result = true;
            result.message = user;

            return JsonConvert.SerializeObject(result);
        }
    }
}