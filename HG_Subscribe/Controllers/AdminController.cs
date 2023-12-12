using HG_Subscribe.Models;
using HGCryptor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Http.Results;
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
            //驗證交易金鑰
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

            return JsonConvert.SerializeObject(result);
        }

        [HttpPost]
        public string getAdminMenu(int mid, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            //int mid = int.Parse(midStr);
            StringBuilder SB = new StringBuilder();

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            string authStr = "";
            var menuList = new ExpandoObject() as IDictionary<string, Object>;
            SB.Append("[");

            if (mid > 0)
            {
                List<administrator> admin = db.administrator.Where(a => a.admID == mid).ToList();

                if (admin.Count > 0)
                {
                    authStr = admin[0].admAuthority;
                }
            }
            else
            {
                authStr = "all";
            }

            var authArr = new List<int>();
            List<adminMenu> menu = new List<adminMenu>();

            if (authStr != "" && authStr != "all")
            {
                authArr = authStr?.Split(',')?.Select(Int32.Parse)?.ToList();
                menu = db.adminMenu.Where(m => authArr.Contains(m.menuID) && m.menuParent == 0).OrderBy(m => m.menuOrder).ToList();
            }
            else
            {
                if (authStr == "all") menu = db.adminMenu.Where(m => m.menuParent == 0).OrderBy(m => m.menuOrder).ToList();
            }

            foreach (var adminMenu in menu)
            {
                SB.Append(SB.ToString() != "[" ? "," : "");
                SB.Append("{");
                SB.Append("'Action':'" + adminMenu.menuName + "'");
                SB.Append(",'Text':'" + adminMenu.menuText + "'");

                List<adminMenu> subMenu = db.adminMenu.Where(m => m.menuParent == adminMenu.menuID).OrderBy(m => m.menuOrder).ToList();
                if (authStr != "" && authStr != "all") subMenu = subMenu.Where(m => authArr.Contains(m.menuID)).ToList();

                    if (subMenu.Count > 0)
                {
                    int subMenuItems = 0;

                    SB.Append(",'SubMenu':[");

                    foreach (var sMenu in subMenu)
                    {
                        SB.Append(subMenuItems > 0 ? "," : "");
                        SB.Append("{");
                        SB.Append("'Action':'" + sMenu.menuName + "'");
                        SB.Append(",'Text':'" + sMenu.menuText + "'");
                        SB.Append("}");
                        subMenuItems++;
                    }
                    SB.Append("]");
                }
                SB.Append("}");
            }

            SB.Append("]");

            result.message = JsonConvert.DeserializeObject(SB.ToString());

            return JsonConvert.SerializeObject(result);
        }
    }
}