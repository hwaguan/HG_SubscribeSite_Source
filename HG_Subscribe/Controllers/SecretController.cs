using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HGCryptor;

namespace HG_Subscribe.Controllers
{
    public class SecretController : Controller
    {
        private static Cryptor cryptor = new Cryptor();
        [HttpPost]
        public string getTransferToken()
        {
            return cryptor.getAPISecret();
        }
    }
}