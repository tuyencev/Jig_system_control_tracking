using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Jig_system_control.Models
{
    public static class FileUploadHelper
    {
        /// <summary>
        /// Upload file vào thư mục theo JigCode (hoặc mã khác),
        /// tự động tạo đường dẫn và tên file duy nhất.
        /// </summary>
        /// <param name="file">HttpPostedFileBase từ form</param>
        /// <param name="subFolder">Ví dụ: "JigDocs"</param>
        /// <param name="code">Ví dụ: JigCode hoặc ECNCode</param>
        /// <param name="controller">Dùng để gọi Url.Content()</param>
        /// <returns>Đường dẫn ảo (virtual path) của file đã lưu</returns>
        public static string UploadFile(HttpPostedFileBase file, string subFolder, string code, string doctype,Controller controller)
        {
            if (file == null || file.ContentLength == 0)
                throw new ArgumentException("File không hợp lệ hoặc rỗng.");

            // 🔹 Đường dẫn vật lý gốc
            string basePath = controller.Server.MapPath($"~/Uploads/{subFolder}/{code}/");
            Directory.CreateDirectory(basePath);

            // 🔹 Thư mục con Image/
            string imagePath = Path.Combine(basePath, doctype);
            Directory.CreateDirectory(imagePath);

            // 🔹 Tạo tên file duy nhất
            string extension = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{doctype}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            string fullSavePath = Path.Combine(imagePath, uniqueFileName);

            // 🔹 Lưu file thật
            file.SaveAs(fullSavePath);

            // 🔹 Trả về đường dẫn ảo để hiển thị
            string virtualPath = controller.Url.Content($"~/Uploads/{subFolder}/{code}/{doctype}/{uniqueFileName}");

            return virtualPath;
        }
    }
}