using Jig_system_control.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Jig_system_control.Controllers
{
    public class SparePartController : Controller
    {


        // GET: SparePart
        public ActionResult Index()
        {
            using (var db = new Jig_system_controlEntities())
            {
                ViewBag.reqCount=db.SparePartRequests.Where(r=>r.Status=="Pending").ToList().Count();
                ViewBag.approveCount=db.vw_SparePart_PendingRequests.ToList().Count();
                ViewBag.Outstock=db.SparePartStocks.Where(s=>s.CurrentQty<=0).ToList().Count();
                ViewBag.Totallist=db.SparePartStocks.ToList().Count();
            }
            return View();
        }
        [HttpPost]
        public JsonResult CreateNewSpareAndRequest(string Code, string Name, string Maker, int QtyRequested, string Remark)
        {
            using (var db = new Jig_system_controlEntities())
            {
                var spare = new SparePartStocks
                {
                    Code = Code,
                    Name = Name,
                    Maker = Maker,
                    CurrentQty = 0
                };
                db.SparePartStocks.Add(spare);
                db.SaveChanges();

                var request = new SparePartRequests
                {
                    SpareID = spare.SpareID,
                    QtyRequested = QtyRequested,
                    RequestedBy = User.Identity.Name ?? "Unknown",
                    RequestDate = DateTime.Now,
                    Status = "Pending",
                    Remark = Remark
                };
                db.SparePartRequests.Add(request);
                db.SaveChanges();

                return Json(new { success = true });
            }
        }

        public JsonResult GetOutStock()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var list = db.SparePartStocks
                
                .Select( s=>new
                {
                    SpareID = s.SpareID,
                    Code = s.Code, Name = s.Name,Maker= s.Maker,CurrentQty= s.CurrentQty,
                    Location = s.Location,
                    Remark = s.Remark


                }
                ).Where(r => r.CurrentQty <= 0)
                 .ToList();
               
                return Json(new { data = list }, JsonRequestBehavior.AllowGet);
            
            }
        }
        public JsonResult GetAlllist()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var list = db.SparePartStocks.
                    Select( s => new
                    {
                        SpareID = s.SpareID,
                        Code = s.Code,
                        Name = s.Name,
                        Maker = s.Maker,
                        CurrentQty = s.CurrentQty,
                        Location = s.Location,
                        Remark = s.Remark
                    })
                 .ToList();

                return Json(new { data = list }, JsonRequestBehavior.AllowGet);

            }
        }
        public ActionResult LoadTabPartial(string tab)
        {
            switch (tab.ToLower())
            {
                case "request":
                    return PartialView("_SpareRequest");
                case "approve":
                    return PartialView("_SpareApprove");
                case "outstock":
                    return PartialView("_SpareoutStock");
                case "alllist":
                    return PartialView("_SpareExport");
                case "history":
                    return PartialView("_SpareHistory");
                default:
                    return Content("<div class='p-3 text-danger'>Không tìm thấy tab phù hợp.</div>");
            }
        }


        public JsonResult GetRequestList()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var q = db.SparePartRequests
                    .Include("SparePartStocks")
                    .Select(r => new
                    {
                        r.RequestID,
                        Code = r.SparePartStocks.Code,
                        Name = r.SparePartStocks.Name,
                        r.QtyRequested,
                        r.SparePartStocks.CurrentQty,
                        r.RequestedBy,
                        r.RequestDate,
                        r.Status,
                    })
                    .Where(r => r.Status =="Pending")
                    .ToList();

                return Json(new { data = q }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult CreateRequest(string Remark, string Details)
        {
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    var user = Session["UserName"]?.ToString() ?? "Unknown";

                    // Parse danh sách chi tiết gửi từ client
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SpareRequestDetail>>(Details);

                    if (list == null || list.Count == 0)
                        return Json(new { success = false, message = "Không có chi tiết nào trong phiếu." });

                    foreach (var item in list)
                    {
                        if (item.Qty <= 0)
                            continue;

                        var request = new SparePartRequests
                        {
                            SpareID = item.SpareID,
                            RequestedBy = user,
                            RequestDate = DateTime.Now,
                            QtyRequested = item.Qty,
                            Remark = Remark,
                            Status="Pending"
                           
                        };

                        db.SparePartRequests.Add(request);
                    }

                    db.SaveChanges();

                    return Json(new { success = true, message = "Tạo phiếu yêu cầu thành công!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lưu phiếu: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult ApproveRequest(int id_request, int approvedQty)
        {
            using (var db = new Jig_system_controlEntities()) // Đổi 'tx' thành 'db'
            {
                try
                {
                    var user = Session["UserName"]?.ToString() ?? "Unknown";
                    var req = db.SparePartRequests.Find(id_request); // ← Giờ consistent
                    if (req == null)
                        return Json(new { success = false, message = "Request không tồn tại." });

                    if (req.Status == "Approved")
                        return Json(new { success = false, message = "Request đã được duyệt." });


                    var request = new SparePartApprovals
                    {
                        RequestID = req.RequestID,
                        ApprovedBy = user,
                        ApprovedDate = DateTime.Now,
                        ApprovedQty = approvedQty,
                        Remark = "Approved",
                    };
                    db.SparePartApprovals.Add(request);

                    db.SaveChanges();

                    return Json(new { success = true ,message="Đã duyệt thành công!", requestId = id_request });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult CancelRequest(int id_request, string comment_request)
        {
            using (var db = new Jig_system_controlEntities()) // Đổi 'tx' thành 'db'
            {
                try
                {
                    var user = Session["UserName"]?.ToString() ?? "Unknown";
                    var req = db.SparePartRequests.Find(id_request); // ← Giờ consistent
                    if (req == null)
                        return Json(new { success = false, message = "Request không tồn tại." });

                    if (req.Status == "Approved")
                        return Json(new { success = false, message = "Request đã được duyệt." });
                    req.Status = "Cancel";
                    req.Remark += " -"+comment_request;
                    db.SaveChanges();

                    return Json(new { success = true, message = "Đã duyệt thành công!", requestId = id_request });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        public JsonResult GetApproveList()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var data = db.vw_SparePart_PendingRequests.ToList();
                return Json(new { data }, JsonRequestBehavior.AllowGet);
            }   
        }


        // nhận hàng
        [HttpPost]
        public JsonResult CreateImport(int SpareID,int Qyt)
        {
            var user = Session["UserName"]?.ToString() ?? "Unknown";
            using (var db = new Jig_system_controlEntities())
            {

                db.Database.ExecuteSqlCommand(
                    "EXEC sp_AutoDistributeReceipt @SpareID, @ReceivedQty, @CreatedBy",
                    new SqlParameter("@SpareID", SpareID),
                    new SqlParameter("@ReceivedQty", Qyt),
                    new SqlParameter("@CreatedBy", user)
                );
            }

            return Json(new { success = true, message = "Đã nhận hàng thành công!" });

        }



        [HttpPost]
        public ActionResult UseSpare(int spareId, int usedQty)
        {
            var user = Session["UserName"]?.ToString() ?? "Unknown";
            using (var db = new Jig_system_controlEntities())
            {
                var tran = new SparePartTransactions
                {
                    SpareID = spareId,
                    Type = "Use",
                    Qty = usedQty,
                    CreatedBy = user,
                    CreatedDate = DateTime.Now
                };

                db.SparePartTransactions.Add(tran);

                // Trừ tồn kho
                var spare = db.SparePartStocks.Find(spareId);
                if (spare != null)
                    spare.CurrentQty -= usedQty;

                db.SaveChanges();
                return Json(new { success = true,message="Đã Lấy thành công" });
            }
        }
        [HttpPost]
        public ActionResult AddSpare(addspare data)
        {
            using (var db = new Jig_system_controlEntities())
            {
                // Kiểm tra trùng mã
                if (db.SparePartStocks.Any(s => s.Code == data.Code))
                {
                    return Json(new { success = false, message = "Mã linh kiện đã tồn tại!" });
                }


                var tran = new SparePartStocks
                {
                    Code = data.Code,
                    Name = data.Name,
                    Maker = data.Maker,
                    Unit = data.Unit,
                    CurrentQty = data.CurrentQty,
                    Location = data.Location,
                    Remark = data.Remark,
                    MinStock = 0,
                    CreatedDate=DateTime.Now
                };

                db.SparePartStocks.Add(tran);
                db.SaveChanges();

                return Json(new { success = true });
            }
        }

        public JsonResult GetHistory()
        {
            using (var db = new Jig_system_controlEntities())
            {
                var q = db.SparePartTransactions
                    .Select(r => new
                    {
                        r.TranID,
                        Code = db.SparePartStocks.Where(x=>x.SpareID==r.SpareID).FirstOrDefault().Code,
                        Name = db.SparePartStocks.Where(x => x.SpareID == r.SpareID).FirstOrDefault().Name,
                      r.Type,
                        r.Qty,
                        r.CreatedBy,
                        r.CreatedDate,
                       
                    })
                    .ToList();

                return Json(new { data = q }, JsonRequestBehavior.AllowGet);
            }
        }


    }
}