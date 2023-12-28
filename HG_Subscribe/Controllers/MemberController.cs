using HG_Subscribe.Models;
using HGCryptor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

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

            memberAccessLog accessLog = new memberAccessLog();
            accessLog.malType = "Regular";
            accessLog.malAction = "Login";
            accessLog.malData = string.Format("Account : {0}, Password : {1}", account, password);
            accessLog.malTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            accessLog.malResult = "Begin";
            db.memberAccessLog.Add(accessLog);
            db.SaveChanges();

            string accessResult = "Granted";

            string acc = cryptor.encryptData(account);
            string psw = cryptor.encryptData(password);

            member user = db.member.Where(m => (m.mMail == acc || m.mGoogleMail == acc || m.mFacebookMail == acc || m.mLineMail == acc) && m.mPassword == psw && m.mEnabled > 0).FirstOrDefault();

            if (user != null)
            {
                result.code = 200;
                result.result = true;
                result.message = user;
            }
            else
            {
                accessResult = "Denied";
                result.code = -1;
                result.result = false;
                result.message = null;
            }

            string userName = user != null ? user.mName : string.Empty;
            accessLog.malType = "Regular";
            accessLog.malAction = "Login";
            accessLog.malData = string.Format("Account : {0}, Password : {1}, Name : {2}", account, password, userName);
            accessLog.malTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            accessLog.malResult = accessResult;
            db.memberAccessLog.Add(accessLog);
            db.SaveChanges();

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

            using (db = new ClikGoEntities())
            {
                memberAccessLog accessLog = new memberAccessLog();
                accessLog.malType = "Google";
                accessLog.malAction = "Login";
                accessLog.malData = string.Format("CID : {0}", CID);
                accessLog.malResult = "Begin";
                accessLog.malTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.memberAccessLog.Add(accessLog);
                db.SaveChanges();

                string accessResult = "Granted";
                int resultCode = 200;
                
                //以使用者的 google CID 找尋是否符合
                member user = db.member.Where(m => m.mGoogleAccount == cid).FirstOrDefault();
                member newUser = new member();

                if (user == null)//未尋獲 google CID 相符的使用者
                {
                    string clientMail = CMail != "" && CMail != null ? cryptor.encryptData(CMail) : "";

                    //若使用者資料中沒有 email 資訊, 則判定為新註冊的使用者, 直接將使用者資訊新增至資料庫
                    if (clientMail == "") {

                        newUser.mName = CName;
                        newUser.mMail = "";
                        newUser.mEnabled = 1;
                        newUser.mGoogleAccount = cid;
                        newUser.mGoogleName = CName;
                        newUser.mGoogleIcon = CPic;
                        newUser.mGoogleMail = clientMail;
                        newUser.mPassword = "";
                        newUser.mAddDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        db.member.Add(newUser);

                        db.SaveChanges();
                    }
                    else
                    {
                        //若以 google 的 CID 未尋獲使用者, 但使用者資料中有 email 資訊, 則以 email 再比對一次以 email 找尋

                        user = db.member.Where(m => (m.mMail == clientMail || m.mGoogleMail == clientMail || m.mFacebookMail == clientMail || m.mLineMail == clientMail)).FirstOrDefault();

                        //若以 email 比對仍未尋獲使用者, 則判定為新註冊的使用者, 直接將使用者資訊新增至資料庫
                        if (user == null)
                        {
                            newUser.mName = CName;
                            newUser.mMail = "";
                            newUser.mEnabled = 1;
                            newUser.mGoogleAccount = cid;
                            newUser.mGoogleName = CName;
                            newUser.mGoogleIcon = CPic;
                            newUser.mPassword = "";
                            newUser.mAddDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                            db.member.Add(newUser);
                            db.SaveChanges();
                        }
                        else 
                        {
                            if (user.mEnabled > 0)//若以 email 比對尋獲使用者, 且使用者仍為有效狀態, 則更新使用者的 google 帳號相關資訊
                            {
                                user.mGoogleAccount = cid;
                                user.mGoogleMail = clientMail;
                                user.mGoogleName = CName;
                                user.mGoogleIcon = CPic;

                                db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                                db.SaveChanges();
                            }
                            else //若以 email 比對仍未尋獲使用者, 則判定登入失敗
                            {
                                user = null;

                                resultCode = -1;
                                accessResult = "Denied";
                            }
                        }
                    }

                    newUser.mGoogleName = CName;
                    newUser.mMail = CMail;
                    user = newUser;
                }
                else
                {
                    string dbMail = user.mMail;
                    string oriMail = dbMail != null && dbMail != "" ? cryptor.decryptData(dbMail) : "";
                    user.mMail = oriMail;
                }

                accessLog.malType = "Google";
                accessLog.malAction = "Login";
                accessLog.malData = string.Format("CID : {0}, Name : {1}", CID, CName);
                accessLog.malTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                accessLog.malResult = accessResult;
                db.memberAccessLog.Add(accessLog);
                db.SaveChanges();

                result.code = resultCode;
                result.result = true;
                result.message = user;
            }

            return JsonConvert.SerializeObject(result);
        }
    }
}