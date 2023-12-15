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
using System.Xml;

namespace HG_Subscribe.Controllers
{
    public class AdminController : Controller
    {
        private static Cryptor cryptor = new Cryptor();
        private static Models.ClikGoEntities db = new Models.ClikGoEntities();
        private static Models.HGEntities dbHG = new Models.HGEntities();

        [HttpPost]
        public string greeting(string name)
        {
            return "Hellow " + name;
        }

        #region 取得組織結構
        /// <summary>
        /// 取得組織結構
        /// </summary>
        /// <param name="token"></param>
        /// <returns>交易金鑰</returns>
        [HttpPost]
        public string getDepartmentTree(string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            List<MITEM> depList = dbHG.MITEM.Where(c => c.mitcode == "DEPAR" && c.del_tag == "0" && c.ditcode != "0000").ToList();
            List<depObj> resultList = new List<depObj>();

            foreach (var dp in depList)
            {
                depObj dep = new depObj();

                dep.depNo = dp.ditcode;
                dep.depText = dp.ddesc;

                resultList.Add(dep);
            }

            result.result = true;
            result.code = 200;
            result.message = resultList;

            return JsonConvert.SerializeObject(result);
        }

        private class depObj
        {
            public string depNo { get; set; }
            public string depText { get; set; }
        }
        #endregion

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
            List<managerEngity> mList = new List<managerEngity>();

            List<administrator> admList = db.administrator.OrderBy(m => m.admID).Skip((page - 1) * rows).Take(rows).ToList();

            admList.ForEach(adm =>
            {
                managerEngity mEntity = new managerEngity();

                MUSER hgUser = dbHG.MUSER.Where(u => u.U_Num == adm.admNo && u.LeaveDate == null).OrderByDescending(u => u.add_Date).FirstOrDefault();
                string Com = dbHG.MITEM.Where(i => i.mitcode == "COMID" && i.ditcode == hgUser.ComID).FirstOrDefault().ddesc;
                string Dep = dbHG.MITEM.Where(i => i.mitcode == "DEPAR" && i.ditcode == hgUser.U_MDEP).FirstOrDefault().ddesc;
                string Title = dbHG.MITEM.Where(i => i.mitcode == "POSIT" && i.ditcode == hgUser.U_POSITION).FirstOrDefault().ddesc;
                adminGroup AG = db.adminGroup.Where(g => g.authCode == adm.admGroup).FirstOrDefault();
                string GroupName = AG.authText;
                string menuAuth = AG.authMenuAuth;
                string funcAuth = AG.authFuncAuth;

                mEntity.ID = adm.admID;
                mEntity.name = adm.admName;
                mEntity.No = adm.admNo;
                mEntity.Title = Title;
                mEntity.Co = Com;
                mEntity.Dep = Dep;
                mEntity.Account = cryptor.decryptData(adm.admAccount);
                mEntity.AccEncrypt = adm.admAccount;
                mEntity.Password = cryptor.decryptData(adm.admPassword);
                mEntity.PswEncrypt = adm.admPassword;
                mEntity.Email = adm.admMail;
                mEntity.Ext = adm.admExt;
                mEntity.AuthGroup = adm.admGroup;
                mEntity.AuthGroupName = GroupName;
                //mEntity.MenuAuth = adm.admGroup > 0 ? adm.admAuthority?.Split(',')?.Select(Int32.Parse)?.ToList() : null;
                mEntity.MenuAuth = menuAuth != null ? menuAuth.Split(',').ToList() : null;
                mEntity.FuncAuth = funcAuth != null ? funcAuth.Split(',').ToList() : null;

                mList.Add(mEntity);
            });

            result.result = true;
            result.code = 200;
            result.message = mList;

            return JsonConvert.SerializeObject(result);
        }

        private class managerEngity
        {
            public int ID { get; set; }
            public string name { get; set; }
            public string No { get; set; }
            public string Title { get; set; }
            public string Co { get; set; }
            public string Dep { get; set; }
            public string Account { get; set; }
            public string AccEncrypt { get; set; }
            public string Password { get; set; }
            public string PswEncrypt { get; set; }
            public string Email { get; set; }
            public string Ext { get; set; }
            public List<string> MenuAuth { get; set; }
            public List<string> FuncAuth { get; set; }
            public int AuthGroup { get; set; }
            public string AuthGroupName { get; set; }

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