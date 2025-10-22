using Jig_system_control.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Jig_system_control.Models;
using Microsoft.Ajax.Utilities;

namespace Jig_system_control.Controllers
{
    public class RepairMaintenanceController : Controller
    {
        // GET: RepairMaintenance
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult LoadTabPartial(string tab)
        {
            switch (tab.ToLower())
            {
                case "repair":
                    return PartialView("_Repair");
                case "maintenance":
                    return PartialView("_Maintenance");
                case "document":
                    return PartialView("_Rquest_document");
                case "design":
                    return PartialView("_Rquest_design");
                default:
                    return Content("<div class='p-3 text-danger'>Không tìm thấy tab phù hợp.</div>");
            }
        }
        public JsonResult GetRepair()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var q = db.JigRepairHistory
                    .Include("Jig")
                    .Select(r => new
                    {
                        r.RepairID,
                        JigCode = r.Jig.JigCode,
                        JigName = r.Jig.JigName,
                        Model=r.Jig.Model,
                        Version=r.Jig.Version,
                        Type=r.Jig.Type,
                        r.Problem,
                        r.RepairDate,
                        r.Status,
                    })
                    .Where(r => r.Status == "Pending")
                    .ToList();

                return Json(new { data = q }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult AddRepair(JigRepairHistory data)
        {
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    var entity = new JigRepairHistory
                    {
                        JigID = data.JigID,
                        RepairDate = DateTime.Now,
                        Problem = data.Problem,
                      
                        FilePath = data.FilePath,
                        Status = "Pending",
                        Remark = data.Remark
                    };

                    db.JigRepairHistory.Add(entity);
                    db.SaveChanges();

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public ActionResult RepairDetail(int id)
        {
            if (id == 0)
            {
                var model = new RepariJigDetail
                {
                    Jig=new Jig(),
                    Repair = new JigRepairHistory(),  // đối tượng rỗng
                    History = new List<JigRepairHistory>(),
                    PartList = new List<danhsachpartlist>()
                };
                ViewBag.IsNew = true;
                return View(model);

            }   
            else  { 
            using (var db = new Jig_system_controlEntities())
            {
                var repair = db.JigRepairHistory.FirstOrDefault(r => r.RepairID == id);
                if (repair == null)
                    return HttpNotFound();

                var jig = db.Jig.FirstOrDefault(j => j.JigID == repair.JigID);

                var history = db.JigRepairHistory
                    .Where(r => r.JigID == jig.JigID)
                    .OrderByDescending(r => r.RepairDate)
                    .Take(10)
                    .ToList();

              var danhsachlinhkien = db.JigPartList
                    . Include("SparePartStocks")
                  .Where(p => p.JigID == repair.JigID)
                  .Select(p => new danhsachpartlist
                  {
                     ID=p.SparePartStocks.SpareID,
                    Code=p.SparePartStocks.Code,
                    Name=p.SparePartStocks.Name,
                    Ghichu=p.Note,

                  }
                  
                  )
                  .ToList();

                var vm = new RepariJigDetail
                {
                    Jig = jig,
                    Repair = repair,
                    History = history,
                   PartList = danhsachlinhkien
                };
                return View(vm);
            }
           }
        }
        [HttpPost]
        public ActionResult SaveRepair(JigRepairHistory data, HttpPostedFileBase FileUpload)
        {
            var user = Session["UserName"]?.ToString() ?? "Unknown";
            try
            {
                if (data == null)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                using (var db = new Jig_system_controlEntities()) // Đổi 'tx' thành 'db'
                {
                    var req = db.JigRepairHistory.Find(data.RepairID); // ← Giờ consistent
                    if (req == null)
                        return Json(new { success = false, message = "Lỗi không tồn tại." });

                    if (req.Status == "Done")
                        return Json(new { success = false, message = "Lỗi đã được Sửa." });

                    // Lưu file upload nếu có
                    if (FileUpload != null && FileUpload.ContentLength > 0)
                    {
                        req.FilePath = FileUploadHelper.UploadFile(FileUpload, "Repair", req.RepairID.ToString(), "OK", this);
                    }

                    // Cập nhật ngày & trạng thái
                    req.RepairDate = DateTime.Now;
                    req.Action = data.Action;
                    req.Status = "Done";
                    req.Technician = 1;


                    db.SaveChanges();


                    return Json(new { success = true, message = "Đã sửa thành công" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // request kaizen
        public ActionResult AddPartPartial(int jigId)
        {
            ViewBag.JigID = jigId;
            return PartialView("Request_kaizen");
        }
        public ActionResult AddPartPartial_design(int jigId)
        {
            ViewBag.JigID = jigId;
            return PartialView("Request_kaizenDesign");
        }

        

        [HttpPost]
        public ActionResult SendRequest(int JigID, string Note,string type, HttpPostedFileBase File)
        {
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    string jigcode = db.Jig.FirstOrDefault(x => x.JigID == JigID).JigCode;

                    string doc_type = FileUploadHelper.UploadFile(File, "Request", jigcode, type, this);
                    string fileName = Path.GetFileName(File.FileName);
                    var doc = new JigRequest
                    {
                        JigID = JigID,
                        RequestType = type,
                        RequestBy = 1,
                        RequestDate = DateTime.Now,
                        Description = Note,
                        Status = "Pending",
                        Filename = fileName,
                        FilePath=doc_type                        
                    };
                    db.JigRequest.Add(doc);
                    db.SaveChanges();

                    return Json(new { success = true, message="Gửi request thành công" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult GetRequestDoc()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var q = db.JigRequest
                    .Include("Jig")
                    .Include("UserAccount")
                    .Where(r => r.Status == "Pending" && r.RequestType == "Document")
                    .Select(r => new
                    {
                        r.RequestID,
                        JigCode = r.Jig.JigCode,
                        JigName = r.Jig.JigName,
                        Model = r.Jig.Model,
                        Version = r.Jig.Version,
                         r.Description,
                        requestby=r.UserAccount.FullName,
                        r.RequestDate,
                        r.FilePath, 
                        r.Status,
                        
                    })
                    .ToList();

                return Json(new { data = q }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetRequestDesign()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var q = db.JigRequest
                    .Include("Jig")
                    .Include("UserAccount")
                    .Where(r => r.Status == "Pending" && r.RequestType == "Design")
                    .Select(r => new
                    {
                        r.RequestID,
                        JigCode = r.Jig.JigCode,
                        JigName = r.Jig.JigName,
                        Model = r.Jig.Model,
                        Version = r.Jig.Version,
                        r.Description,
                        requestby = r.UserAccount.FullName,
                        r.RequestDate,
                        r.FilePath,
                        r.Status,

                    })
                    .ToList();

                return Json(new { data = q }, JsonRequestBehavior.AllowGet);
            }
        }

    }

}