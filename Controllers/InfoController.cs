using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Jig_system_control.Controllers
{
    public class InfoController : Controller
    {
        // GET: Info
        [HttpGet]
        public ActionResult getinfo()
        {
            return View();
        }
        [HttpPost]
        public ActionResult getinfo(string majig)
        {
            if (majig == "admin" )
            {
              
                return RedirectToAction("getinfo", "Info");
            }

            ViewBag.Error = "Mã Jig không tồn tại";
            return View();
        }
    }
}