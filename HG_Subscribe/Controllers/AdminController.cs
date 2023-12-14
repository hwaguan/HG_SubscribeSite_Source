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

        #region 管理者登入
        /// <summary>
        /// 管理者登入
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="token">交易金鑰</param>
        /// <returns></returns>
        /// <memo>2023-12-11 add by Blair</memo>
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
                result.message = admin[0];
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 取得管理介面左側選單
        /// <summary>
        /// 取得管理介面左側選單
        /// </summary>
        /// <param name="mid">管理者ID</param>
        /// <param name="token">交易金鑰</param>
        /// <returns></returns>
        /// <memo>2023-12-11 add by Blair</memo>
        [HttpPost]
        public string getAdminMenu(int mid, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            //int mid = int.Parse(midStr);
            StringBuilder SB = new StringBuilder();

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            string authStr = mid > 0 ? "" : "all";
            var menuList = new ExpandoObject() as IDictionary<string, Object>;
            SB.Append("[");

            if (mid > 0)
            {
                List<administrator> admin = db.administrator.Where(a => a.admID == mid).ToList();

                if (admin.Count > 0)
                {
                    if (admin[0].admGroup > 0)
                    {
                        authStr = admin[0].admAuthority;
                    }
                    else
                    {
                        authStr = "all";
                    }
                }
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

            result.result = SB.ToString().ToString() == "[]" ? false : true;
            result.message = JsonConvert.DeserializeObject(SB.ToString());
            result.code = SB.ToString().ToString() == "[]" ? 0 : 200;

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 取得管理員資料總數
        /// <summary>
        /// 取得管理員資料總數
        /// </summary>
        /// <param name="token">交易金鑰</param>
        /// <returns></returns>
        /// <memo>2023-12-11 add by Blair</memo>
        [HttpPost]
        public string getManagerTotal(string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            result.result = true;
            result.code = 200;
            result.message = db.administrator.Count();

            return JsonConvert.SerializeObject(result);
        }
        #endregion


        #region 取得管理員列表
        /// <summary>
        /// 取得管理員列表
        /// </summary>
        /// <param name="token">交易金鑰</param>
        /// <returns></returns>
        /// <memo>2023-12-14 add by Blair</memo>
        [HttpPost]
        public string getManagerList(int page, int rows, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            result.result = true;
            result.code = 200;
            result.message = db.administrator.Skip((page - 1) * rows).Take(rows);

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 更新管理員(新增 / 更新)
        /// <summary>
        /// 更新管理員(新增 / 更新)
        /// </summary>
        /// <param name="dataStr">管理員資料字串</param>
        /// <param name="token">交易金鑰</param>
        /// <returns></returns>
        /// <memo>2023-12-14 add by Blair</memo>
        [HttpPost]
        public string updateManager(string dataStr, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            try
            {
                administrator managerModel = JsonConvert.DeserializeObject<administrator>(dataStr);

                if (managerModel.admID == 0)
                {
                    db.administrator.Add(managerModel);
                }
                else
                {
                    db.Entry(managerModel).State = System.Data.Entity.EntityState.Modified;
                }

                db.SaveChanges();

                result.result = true;
                result.code = 200;
                result.message = managerModel;
            }
            catch (Exception ex)
            {
                result.result = false;
                result.code = -1;
                result.message = ex.Message;
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion
    }
}