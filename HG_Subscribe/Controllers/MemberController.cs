﻿using HG_Subscribe.Models;
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

        #region 比對會員帳號是否存在
        /// <summary>
        /// 比對會員帳號是否存在
        /// </summary>
        /// <param name="mMail"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        public string verifyMember(string mMail, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            using (db = new ClikGoEntities())
            {
                string cryptedMail = cryptor.encryptData(mMail);
                member existMember = db.member.Where(m => m.mMail == cryptedMail || m.mGoogleMail == cryptedMail || m.mFacebookMail == cryptedMail || m.mLineMail == cryptedMail).FirstOrDefault();

                result.code = 200;
                result.result = true;
                result.message = existMember != null;
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 註冊會員
        /// <summary>
        /// 註冊會員
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        public string registerMember(string account, string password, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            using (db = new ClikGoEntities())
            {
                member newMember = new member();

                newMember.mName = "訪客";
                newMember.mMail = cryptor.encryptData(account);
                newMember.mPassword = cryptor.encryptData(password);
                newMember.mEnabled = 0;
                newMember.mRegisterToken = getOTP(0);
                newMember.mAddDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                db.member.Add(newMember);
                db.SaveChanges();

                result.result = true;
                result.code = 200;
                result.message = newMember;
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 啟用會員帳號
        /// <summary>
        /// 啟用會員帳號
        /// </summary>
        /// <param name="registerToken"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        public string initMember(string registerToken, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            try
            {
                string[] tokenArr = cryptor.decryptData(registerToken).Split('_');
                int tokenVal = int.Parse(tokenArr[0]) == 0 ? int.Parse(tokenArr[1]) : int.Parse(tokenArr[0]);

                using (db = new ClikGoEntities())
                {
                    member targetMember = db.member.Where(m => m.mRegisterToken == registerToken).FirstOrDefault();

                    if (targetMember != null)
                    {
                        if (targetMember.mEnabled == 0)
                        {
                            targetMember.mEnabled = 1;
                            db.Entry(targetMember).State = System.Data.Entity.EntityState.Modified;
                            db.SaveChanges();

                            result.result = true;
                            result.code = 200;
                            result.message = targetMember;
                        }
                        else
                        {
                            result.result = false;
                            result.code = 666;
                            result.message = "account already inited";
                        }
                    }
                    else
                    {
                        result.result = false;
                        result.code = -1;
                        result.message = registerToken + " init failed.";
                    }
                }
            }
            catch (Exception ex)
            {
                result.result = false;
                result.code = -1;
                result.message = registerToken + " init failed.";
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 一般會員登入
        /// <summary>
        /// 一般會員登入
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        public string login(string account, string password, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            using (db = new ClikGoEntities())
            {
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

                if (result.code > 0)
                {
                    string dbMail = user.mMail;
                    string oriMail = dbMail != null && dbMail != "" ? cryptor.decryptData(dbMail) : "";
                    user.mMail = oriMail;
                    result.message = user;
                }
            }
            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region google 會員登入
        /// <summary>
        /// google 會員登入
        /// </summary>
        /// <param name="CID"></param>
        /// <param name="CName"></param>
        /// <param name="CMail"></param>
        /// <param name="CPic"></param>
        /// <param name="token"></param>
        /// <returns></returns>
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
                    if (clientMail == "")
                    {

                        newUser.mName = CName;
                        newUser.mMail = "";
                        newUser.mEnabled = 1;
                        newUser.mGoogleAccount = cryptor.encryptData(cid);
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
                            newUser.mGoogleAccount = cryptor.encryptData(cid);
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
                            if (user.mEnabled > 0)//若以 email 比對尋獲使用者, 且使用者仍為有效狀態, 則更新使用者的 google 帳號相關資訊
                            {
                                user.mGoogleAccount = cid;
                                user.mGoogleMail = cryptor.encryptData(clientMail);
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
                    user.mGoogleAccount = cid;
                    user.mGoogleMail = cryptor.encryptData(CMail);
                    user.mGoogleName = CName;
                    user.mGoogleIcon = CPic;

                    db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                accessLog.malType = "Google";
                accessLog.malAction = "Login";
                accessLog.malData = string.Format("CID : {0}, Name : {1}", CID, CName);
                accessLog.malTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                accessLog.malResult = accessResult;
                db.memberAccessLog.Add(accessLog);
                db.SaveChanges();

                string dbMail = user.mMail;
                string oriMail = dbMail != null && dbMail != "" ? cryptor.decryptData(dbMail) : "";
                user.mMail = oriMail;

                result.code = resultCode;
                result.result = true;
                result.message = user;
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 申請重置密碼
        /// <summary>
        /// 申請重置密碼
        /// </summary>
        /// <param name="mMail"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        public string applyResetPassword(string mMail, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            try
            {
                using (db = new ClikGoEntities())
                {
                    string encryptMail = cryptor.encryptData(mMail);
                    member targetMember = db.member.Where(m => m.mMail == encryptMail).FirstOrDefault();

                    changePassLog CPL = new changePassLog();

                    CPL.cpMemberID = targetMember.mID;
                    CPL.cpMemberName = targetMember.mName;
                    CPL.cpMemberOldPassword = targetMember.mPassword;
                    CPL.cpToken = getOTP(1);
                    CPL.cpApplyDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    db.changePassLog.Add( CPL );
                    db.SaveChanges();

                    result.result = true;
                    result.code = 200;
                    result.message = true;
                }
            }catch (Exception e)
            {
                result.result = false;
                result.code = 500;
                result.message = e.Message;
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 重置會員登入密碼
        [HttpPost]
        public string resetMemberPassword(string logToken, string newPassword, string token)
        {
            //驗證交易金鑰
            Cryptor.apiResultObj RC = cryptor.verifyAPISecret(token);
            if (!RC.result) return JsonConvert.SerializeObject(RC);
            Cryptor.apiResultObj result = new Cryptor.apiResultObj();

            using (db = new ClikGoEntities())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        string encryptedPass = cryptor.encryptData(newPassword);
                        changePassLog CPL = db.changePassLog.Where(c => c.cpToken == logToken).FirstOrDefault();
                        CPL.cpMemberNewPassword = encryptedPass;
                        CPL.cpChangeDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        member targetMember = db.member.Where(m => m.mID == CPL.cpMemberID).FirstOrDefault();
                        targetMember.mPassword = encryptedPass;

                        db.SaveChanges();
                        dbContextTransaction.Commit();

                        result.result = true;
                        result.code = 200;
                        result.message = "success";
                    }
                    catch (Exception e)
                    {
                        dbContextTransaction.Rollback();

                        result.result = false;
                        result.code=500;
                        result.message = e.Message;
                    }
                }
            }

            return JsonConvert.SerializeObject(result);
        }
        #endregion

        #region 產生一次性交易金鑰
        /// <summary>
        /// 產生一次性交易金鑰
        /// </summary>
        /// <returns></returns>
        private string getOTP(int type)
        {
            int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            int applyCount = 0;

            using (var dbOTP = new ClikGoEntities())
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");

                if (type == 0)
                {
                    applyCount = dbOTP.member.Where(m => m.mAddDate.StartsWith(today)).Count();
                }
                else
                {
                    applyCount = dbOTP.changePassLog.Where(c => c.cpApplyDate.StartsWith(today)).Count();
                }
            }

            string OTPStr= unixTimestamp.ToString();
            if (unixTimestamp % 2 == 0)
            {
                OTPStr = string.Format("{0}_{1}", applyCount, OTPStr);
            }
            else
            {
                OTPStr = string.Format("{1}_{0}", applyCount, OTPStr);
            }

            return cryptor.encryptData(OTPStr);
        }
        #endregion
    }
}