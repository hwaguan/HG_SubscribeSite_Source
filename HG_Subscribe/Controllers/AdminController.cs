using HG_Subscribe.Models;
using HGCryptor;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace HG_Subscribe.Controllers
{
    public class AdminController : Controller
    {
        private static Cryptor cryptor = new Cryptor();
        private static Models.ClikGoEntities db = new Models.ClikGoEntities();
        private static Models.HGEntities dbHG = new Models.HGEntities();

        [HttpPost]
        public string greeting(string name, int mid)
        {
            return "Hellow " + name + " , " + mid;
        }

        #region 取得所有員工
        [HttpPost]
        public string getAllEmployees(string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            List<empObj> empList = new List<empObj>();
            var sAdmins = db.administrator.Select(a => a.admNo).ToList();

            var mUSERs = dbHG.MUSER.Where(u => u.LeaveDate == null && u.del_tag == "0" && u.U_Tel != "" && !sAdmins.Any(a => a == u.U_Num)).Select(u => new { u.U_Num, u.U_Name, u.U_Tel, u.ComID, u.U_MDEP, u.EMail }).Distinct().OrderBy(u => u.ComID).ToList();

            foreach (var item in mUSERs)
            {
                empObj emp = new empObj();

                var mCo = dbHG.MITEM.Where(i => i.ditcode == item.ComID && i.mitcode == "COMID").FirstOrDefault();
                var mDep = dbHG.MITEM.Where(i => i.ditcode == item.U_MDEP && i.mitcode == "DEPAR").FirstOrDefault();

                emp.empNo = item.U_Num;
                emp.empName = item.U_Name;
                emp.empBranch = item.ComID;
                emp.empCo = item.ComID;
                emp.empCoName = mCo.ddesc;
                emp.empDep = item.U_MDEP;
                emp.empDepName = mDep.ddesc;
                emp.empExt = item.U_Tel;
                emp.empMail = item.EMail;

                empList.Add(emp);
            }

            result.result = true;
            result.code = 200;
            result.message = empList;

            return JsonConvert.SerializeObject(result);
        }

        private class empObj
        {
            public int empID { get; set; }
            public string empNo { get; set; }
            public string empName { get; set; }
            public string empBranch { get; set; }
            public string empCo { get; set; }
            public string empCoName { get; set; }
            public string empDep { get; set; }
            public string empDepName { get; set; }
            public string empExt { get; set; }
            public string empMail { get; set; }
            public string[] empAuth { get; set; }
        }
        #endregion

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
                List<administrator> admin = db.administrator.Where(a => a.admID == mid && a.admEnabled > 0).ToList();

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
                menu = db.adminMenu.Where(m => authArr.Contains(m.menuID) && m.menuParent == 0 && m.menuEnabled > 0).OrderBy(m => m.menuOrder).ToList();
            }
            else
            {
                if (authStr == "all") menu = db.adminMenu.Where(m => m.menuParent == 0 && m.menuEnabled > 0).OrderBy(m => m.menuOrder).ToList();
            }

            foreach (var adminMenu in menu)
            {
                SB.Append(SB.ToString() != "[" ? "," : "");
                SB.Append("{");
                SB.Append("'ID':" + adminMenu.menuID);
                SB.Append(",'Action':'" + adminMenu.menuName + "'");
                SB.Append(",'Text':'" + adminMenu.menuText + "'");

                List<adminMenu> subMenu = db.adminMenu.Where(m => m.menuParent == adminMenu.menuID && m.menuEnabled > 0).OrderBy(m => m.menuOrder).ToList();
                if (authStr != "" && authStr != "all") subMenu = subMenu.Where(m => authArr.Contains(m.menuID)).ToList();

                if (subMenu.Count > 0)
                {
                    int subMenuItems = 0;

                    SB.Append(",'SubMenu':[");

                    foreach (var sMenu in subMenu)
                    {
                        SB.Append(subMenuItems > 0 ? "," : "");
                        SB.Append("{");
                        SB.Append("'ID':" + sMenu.menuID);
                        SB.Append(",'Action':'" + sMenu.menuName + "'");
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
        public string getManagerTotal(string token, bool showAll = false)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            result.result = true;
            result.code = 200;
            result.message = showAll ? db.administrator.Count() : db.administrator.Where(a => a.admEnabled > 0).Count();

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
        public string getManagerList(int page, int rows, string token, bool showAll = false)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();
            List<managerEngity> mList = new List<managerEngity>();

            List<administrator> admList = showAll ? db.administrator.OrderBy(m => m.admID).Skip((page - 1) * rows).Take(rows).ToList() : db.administrator.Where(m => m.admEnabled > 0).OrderBy(m => m.admID).Skip((page - 1) * rows).Take(rows).ToList();
            admList.ForEach(adm =>
            {
                managerEngity mEntity = new managerEngity();

                MUSER hgUser = dbHG.MUSER.Where(u => u.U_Num == adm.admNo && u.LeaveDate == null).OrderByDescending(u => u.add_Date).FirstOrDefault();
                string Com = dbHG.MITEM.Where(i => i.mitcode == "COMID" && i.ditcode == hgUser.ComID).FirstOrDefault().ddesc;
                string Dep = dbHG.MITEM.Where(i => i.mitcode == "DEPAR" && i.ditcode == hgUser.U_MDEP).FirstOrDefault().ddesc;
                string Title = dbHG.MITEM.Where(i => i.mitcode == "POSIT" && i.ditcode == hgUser.U_POSITION).FirstOrDefault().ddesc;

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
                mEntity.enabled = adm.admEnabled;

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
            public int AuthGroup { get; set; }
            public string AuthGroupName { get; set; }
            public string AuthContent { get; set; }
            public int enabled { get; set; }

        }
        #endregion

        #region 取得特定管理員
        [HttpPost]
        public string getAdminManager(int mid, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            empObj admObj = new empObj();

            administrator admData = db.administrator.Where(a => a.admID == mid).FirstOrDefault();

            string depName = dbHG.MITEM.Where(i => i.mitcode == "DEPAR" && i.ditcode == admData.admDep).Select(i => i.ddesc).FirstOrDefault();
            string[] authArr = admData.admAuthority == "" || admData.admAuthority == null ? new string[0] : admData.admAuthority.Split(',');

            admObj.empID = mid;
            admObj.empNo = admData.admNo;
            admObj.empName = admData.admName;
            admObj.empBranch = admData.admCorp;
            admObj.empDep = admData.admDep;
            admObj.empDepName = depName;
            admObj.empExt = admData.admExt;
            admObj.empMail = admData.admMail;
            admObj.empAuth = authArr;

            result.result = true;
            result.code = 200;
            result.message = admObj;

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
                    //db.Entry(managerModel).State = System.Data.Entity.EntityState.Modified;
                }

                //db.SaveChanges();

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

        #region 刪除管理員
        [HttpPost]
        public string removeManager(int aID, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            administrator adminData = db.administrator.Where(a => a.admID == aID).FirstOrDefault();

            if (adminData != null)
            {
                adminData.admEnabled = 0;

                db.Entry(adminData).State = System.Data.Entity.EntityState.Modified;

                db.SaveChanges();

                result.result = true;
                result.code = 200;
                result.message = "Ok";
            }
            else
            {
                result.result = true;
                result.code = 666;
                result.message = "record not found";
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 復原管理員
        [HttpPost]
        public string reviveManager(int aID, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            administrator adminData = db.administrator.Where(a => a.admID == aID).FirstOrDefault();

            if (adminData != null)
            {
                adminData.admEnabled = 1;

                db.Entry(adminData).State = System.Data.Entity.EntityState.Modified;

                db.SaveChanges();

                result.result = true;
                result.code = 200;
                result.message = "Ok";
            }
            else
            {
                result.result = true;
                result.code = 666;
                result.message = "record not found";
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 取得管理員群組
        /// <summary>
        /// 取得管理員群組
        /// </summary>
        /// <param name="page"></param>
        /// <param name="rows"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        public string getGroupList(int page, int rows, string token, bool showAll = false)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();
            List<adminAuthGroup> agList = showAll ? db.adminAuthGroup.OrderBy(g => g.agRank).Skip((page - 1) * rows).Take(rows).ToList() : db.adminAuthGroup.Where(g => g.agEnabled > 0).OrderBy(g => g.agRank).Skip((page - 1) * rows).Take(rows).ToList();

            result.result = true;
            result.code = 200;
            result.message = agList;

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 比對管理群組名稱是否重複
        [HttpPost]
        public string verifyGroupName(int groupid, string groupName, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            bool verifyResult = db.adminAuthGroup.Where(g => g.agID != groupid && g.agName == groupName).Count() > 0;

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            result.result = true;
            result.code = verifyResult ? 666 : 200;
            result.message = verifyResult ? "No" : "Ok";

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 更新管理群組(新增 / 更新
        [HttpPost]
        public string updateAuthGroup(int? groupID, string groupName, int groupRank, string groupAuth, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            adminAuthGroup AG = db.adminAuthGroup.Where(a => a.agID == groupID).FirstOrDefault();
            AG.agName = groupName;
            AG.agRank = groupRank;
            AG.agContent = groupAuth;

            if (groupID > 0)
            {
                AG.agID = (int)groupID;
                AG.agLastModify = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                AG.agCreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                AG.agEnabled = 1;
            }

            db.adminAuthGroup.AddOrUpdate(AG);

            db.SaveChanges();

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            result.result = true;
            result.code = 200;
            result.message = "success";

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 刪除管理群
        [HttpPost]
        public string removeAuthGroup(int agID, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            adminAuthGroup adminGroupData = db.adminAuthGroup.Where(g => g.agID == agID).FirstOrDefault();

            if (adminGroupData != null)
            {
                adminGroupData.agEnabled = 0;

                db.Entry(adminGroupData).State = System.Data.Entity.EntityState.Modified;

                db.SaveChanges();

                result.result = true;
                result.code = 200;
                result.message = "Ok";
            }
            else
            {
                result.result = true;
                result.code = 666;
                result.message = "record not found";
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 復原管理群
        [HttpPost]
        public string reviveAuthGroup(int agID, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);

            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            adminAuthGroup adminGroupData = db.adminAuthGroup.Where(g => g.agID == agID).FirstOrDefault();

            if (adminGroupData != null)
            {
                adminGroupData.agEnabled = 1;

                db.Entry(adminGroupData).State = System.Data.Entity.EntityState.Modified;

                db.SaveChanges();

                result.result = true;
                result.code = 200;
                result.message = "Ok";
            }
            else
            {
                result.result = true;
                result.code = 666;
                result.message = "record not found";
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion
    }
}