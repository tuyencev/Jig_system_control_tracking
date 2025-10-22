using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Jig_system_control.Models
{
    public class ModelBindingLoggerAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var contentLength = request.ContentLength;
            var filesCount = request.Files.Count;
            var isMultipart = request.ContentType?.Contains("multipart/form-data") ?? false;

            Debug.WriteLine("🧭 ===== Model Binding Diagnostic =====");
            Debug.WriteLine($"📦 Content Length: {contentLength} bytes");
            Debug.WriteLine($"📎 Files count: {filesCount}");
            Debug.WriteLine($"🧩 Multipart: {isMultipart}");
            Debug.WriteLine($"🔑 Form Keys: {string.Join(", ", request.Form.AllKeys)}");

            // Check potential issues
            if (contentLength > (50 * 1024 * 1024))
                Debug.WriteLine("⚠️ File upload > 50MB — may exceed maxRequestLength.");

            if (!isMultipart)
                Debug.WriteLine("⚠️ Form missing enctype='multipart/form-data' — files won't bind.");

            if (filesCount == 0 && request.Form.Count == 0)
                Debug.WriteLine("⚠️ No form data received — wizard or AJAX post issue?");
          
            base.OnActionExecuting(filterContext);

        }
    }
}