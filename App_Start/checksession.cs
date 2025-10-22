using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Jig_system_control.App_Start
{
    public class checksession : AuthorizeAttribute
    {

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var user = HttpContext.Current.Session["ten"];

            if (user == null)
            {
                // Lấy đường dẫn hiện tại (vd: /Home/Info/5)
                var returnUrl = filterContext.HttpContext.Request.RawUrl;

                // Chuyển hướng sang Login và truyền ReturnUrl
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        Controller = "Home",
                        Action = "Login",
                        ReturnUrl = returnUrl
                    })
                );
            }
            else { return; }
        }
    }
}