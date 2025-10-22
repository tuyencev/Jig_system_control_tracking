using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Jig_system_control.App_Start;
using Jig_system_control.Models;

namespace Jig_system_control.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        private readonly Jig_system_controlEntities db;
        public HomeController()
        {
            db = new Jig_system_controlEntities();
        }


        [HttpGet]
        public JsonResult GetSuggestions(string query)
        {
            using (var db = new Jig_system_controlEntities())
            {
                var result = db.Jig
                    .Where(x => x.JigCode.Contains(query) || x.JigName.Contains(query))
                    .Select(x => new
                    {
                        x.JigID,
                        x.JigCode,
                        x.JigName,
                        x.Model
                    })
                    .Take(20)
                    .ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetPartial(string name)
        {
            switch (name)
            {
                case "_AddLinhKien": return PartialView("_AddLinhKien");
                case "_RepairJig": return PartialView("_RepairJig");
                default: return PartialView("_SharedPartial");
            }
        }
        public ActionResult Info(int id)
        {

            using (var db = new Jig_system_controlEntities())
            {
                var jig = db.Jig
             .Include("JigDocument")
             .Include("JigDrawing")
            .Include("JigPartList")
             .FirstOrDefault(x => x.JigID == id);


                if (jig == null)
                    return View(jig);

                // Collect spare ids
                var spareIds = jig.JigPartList.Select(p => p.SpareID).Where(x => x != 0).Distinct().ToList();

                // Load spare info into a dictionary (only simple fields)
                var spareDict = db.SparePartStocks
                    .Where(s => spareIds.Contains(s.SpareID))
                    .Select(s => new { s.SpareID, s.Code, s.Name, s.Maker })
                    .ToList()
                    .ToDictionary(s => s.SpareID, s => s);

                var partsDisplay = jig.JigPartList.Select(p => new PartDisplay
                {
                    PartID = p.ID,
                    SpareID = p.SpareID,
                    Code = spareDict.ContainsKey(p.SpareID) ? spareDict[p.SpareID].Code : "",
                    Name = spareDict.ContainsKey(p.SpareID) ? spareDict[p.SpareID].Name : "",
                    Maker = spareDict.ContainsKey(p.SpareID) ? spareDict[p.SpareID].Maker : "",
                    Qty = p.Qty ?? 0,
                    Note = p.Note
                }).ToList();

                ViewBag.PartDisplay = partsDisplay;


                return View(jig);
            }
        }


        [HttpGet]
        public ActionResult Login()
        {
         
          
            return View();
        }
        [HttpPost]
        public ActionResult Login(string Username, string Password, string ReturnUrl)
        {
            if (Username == "admin" && Password == "123")
            {
                FormsAuthentication.SetAuthCookie(Username, true);
                Session["ten"] = Username;
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }

                // Nếu không có (login trực tiếp) -> về trang mặc định
                return RedirectToAction("Create_jig", "Home");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
            return View();
        }


        public ActionResult Public()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }


        // tạo jig mới

        [checksession]
        public ActionResult Create_jig()
        {
            return View();
        }
        [HttpPost, checksession]
        [ModelBindingLoggerAttribute]
        public ActionResult Create_jig(JigCreateViewModel data)
        {
          
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    // 🧱 1️⃣ Tạo đối tượng Jig
                    string uploadFolder = Server.MapPath("~/Uploads/JigDocs/"+data.JigCode+"/");
                  
                    string fileUrl=  FileUploadHelper.UploadFile(Request.Files["Image"], "JigDocs", data.JigCode,"Image", this);

                    var jig = new Jig
                    {
                        JigCode = data.JigCode,
                        JigName = data.JigName,
                        Version = data.Version,
                        Model = data.Model,
                        Type = data.Type,
                        Range = data.Range,
                        Note = data.Note,
                        Status = "Running",
                        CreatedBy = 1,
                        CreatedDate = DateTime.Now,
                        Image= fileUrl
                    };

                    // 🧩 3️⃣ Xử lý các Document mặc định (PMCS, Manual...)
                    HandleSingleFileUpload(Request.Files["PMCS"], "PMCS", form: Request.Form, jig,uploadFolder);
                    HandleSingleFileUpload(Request.Files["Machine_checksheet"], "Machine_checksheet", form: Request.Form, jig,uploadFolder );
                    HandleSingleFileUpload(Request.Files["Manual"], "Manual", form: Request.Form, jig, uploadFolder);
                    HandleSingleFileUpload(Request.Files["Maintain"], "Maintain", form: Request.Form, jig, uploadFolder);

                    // 🧾 4️⃣ Tài liệu khác (nhiều file)
                   foreach (var doc in data.Documents ?? new List<DocumentUploadVM>())
                   {
                       if (doc.File != null)
                       {
                            string fileDocument = FileUploadHelper.UploadFile(doc.File, "JigDocs", data.JigCode, "Documents", this);

                            jig.JigDocument.Add(new JigDocument
                                   {
                                       FileName = doc.File.FileName,
                                       DocType = doc.DocType,
                                       Version = doc.Version,
                                       Note = doc.Note,
                                       FilePath = fileDocument
                            });
                       }
                   }

                    // 🧩 5️⃣ Drawing
                    foreach (var doc in data.Drawing ?? new List<DrawingUploadVM>())
                    {
                        if (doc.File != null)
                        {
                            string fileDrawing = FileUploadHelper.UploadFile(doc.File, "JigDocs", data.JigCode, "Drawing", this);

                            jig.JigDrawing.Add(new JigDrawing
                            {
                                DrawingName=doc.DocType,
                                FileName = doc.File.FileName,
                                Version = doc.Version,
                                Note = doc.Note,
                                FilePath = fileDrawing
                            });

                        }
                    }
                    //
                    // ⚙️ 6️⃣ Part list
                    if (!string.IsNullOrEmpty(data.PartListJson))
                    {
                        var partItems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PartItemVM>>(data.PartListJson);
                        foreach (var part in partItems)
                        {
                            db.JigPartList.Add(new JigPartList
                            {
                                JigID = jig.JigID,
                                SpareID = part.SpareID,
                                Qty = part.Qty,
                                Note = part.Note
                            });
                        }
                    }

                    // ✅ 7️⃣ Chỉ cần add Jig, EF sẽ tự cascade insert hết
                    db.Jig.Add(jig);
                    db.SaveChanges();
                    ViewBag.jigid = jig.JigID;
                    ViewBag.jigcode = jig.JigCode;
                }

                TempData["Success"] = "Tạo mới Jig thành công!";
                return RedirectToAction("Detail", new { id = ViewBag.jigid });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi lưu dữ liệu: " + ex.Message;
                return RedirectToAction("Create");
            }
        }
        private void HandleSingleFileUpload(HttpPostedFileBase file, string docType, NameValueCollection form, Jig jig, string uploadFolder)
        {
            if (file != null && file.ContentLength > 0)
            {
                string version = form[$"{docType}Version"];
                string note = form[$"{docType}Note"];

                string file_type = FileUploadHelper.UploadFile(file, "JigDocs", jig.JigCode, docType, this);

                jig.JigDocument.Add(new JigDocument
                {
                    DocType = docType,
                    FileName = file.FileName,
                    Version = version,
                    Note = note,
                    FilePath = file_type
                });
            }
        }

        // detail jig
        [checksession]
        public ActionResult Detail(int id)
        {
            using (var db = new Jig_system_controlEntities())
            {
                var jig = db.Jig
             .Include("JigDocument")
             .Include("JigDrawing")
            .Include("JigPartList")
             .FirstOrDefault(x => x.JigID == id);


                if (jig == null)
                    return View(jig);
        
                // Collect spare ids
                var spareIds = jig.JigPartList.Select(p => p.SpareID).Where(x => x != 0).Distinct().ToList();

                // Load spare info into a dictionary (only simple fields)
                var spareDict = db.SparePartStocks
                    .Where(s => spareIds.Contains(s.SpareID))
                    .Select(s => new { s.SpareID, s.Code, s.Name, s.Maker }) 
                    .ToList()
                    .ToDictionary(s => s.SpareID, s => s);

                var partsDisplay = jig.JigPartList.Select(p => new PartDisplay
                {
                    PartID = p.ID,
                    SpareID = p.SpareID,
                    Code = spareDict.ContainsKey(p.SpareID) ? spareDict[p.SpareID].Code : "",
                    Name = spareDict.ContainsKey(p.SpareID) ? spareDict[p.SpareID].Name : "",
                    Maker = spareDict.ContainsKey(p.SpareID) ? spareDict[p.SpareID].Maker : "",
                    Qty = p.Qty??0,
                    Note = p.Note
                }).ToList();

                ViewBag.PartDisplay = partsDisplay;


                return View(jig);
            }
        }

        public ActionResult AddDocumentPartial(int jigId)
        {
            ViewBag.JigID = jigId;
            return PartialView("Partials/_AddDocumentPartial");
        }
        public ActionResult AddDrawingPartial(int jigId)
        {
            ViewBag.JigID = jigId;
            return PartialView("Partials/_AddDrawingPartial");
        }

        public ActionResult AddPartPartial(int jigId)
        {
            ViewBag.JigID = jigId;
            return PartialView("Partials/_AddPartPartial");
        }

        public ActionResult EditInfoPartial(int jigId)
        {
            using (var db = new Jig_system_controlEntities())
            {
                var jig = db.Jig.FirstOrDefault(x => x.JigID == jigId);
                return PartialView("Partials/_EditInfoPartial", jig);
            }
        }


        [HttpPost]
        public ActionResult AddDocument(int JigID, string DocType, string Version, string Note, HttpPostedFileBase File)
        {
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    string jigcode=db.Jig.FirstOrDefault(x=>x.JigID == JigID).JigCode;

                    string doc_type = FileUploadHelper.UploadFile(File, "JigDocs", jigcode, "Drawing", this);
                    string fileName = Path.GetFileName(File.FileName);
                    var doc = new JigDocument
                    {
                        JigID = JigID,
                        DocType = DocType,
                        FileName = fileName,
                        Version = Version,
                        Note = Note,
                        FilePath = doc_type
                    };
                    db.JigDocument.Add(doc);
                    db.SaveChanges();

                    string html = $"<tr id='doc_{doc.DocID}'><td>{DocType}</td><td><a href='{doc.FilePath}' target='_blank'>{fileName}</a></td><td>{Version}</td><td>{Note}</td><td><button class='btn btn-sm btn-secondary' onclick='editDoc({doc.DocID})'>Sửa</button><button class='btn btn-sm btn-danger' onclick='deleteDoc({doc.DocID})'>Xóa</button></td></tr>";

                    return Json(new { success = true, html });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult AddDrawing(int JigID, string DocType, string Version, string Note, HttpPostedFileBase File)
        {
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    string jigcode = db.Jig.FirstOrDefault(x => x.JigID == JigID).JigCode;
                    string doc_type = FileUploadHelper.UploadFile(File, "JigDocs", jigcode, "Drawing", this);

                    string fileName = Path.GetFileName(File.FileName);
                  

                    var draw = new JigDrawing
                    {
                        JigID = JigID,
                        DrawingName = DocType,
                        FileName = fileName,
                        Version = Version,
                        Note = Note,
                        FilePath = doc_type
                    };
                    db.JigDrawing.Add(draw);
                    db.SaveChanges();

                    string html = $"<tr id='draw_{draw.DrawingID}'> <td>{DocType}</td> <td><a href='{draw.FilePath}' target='_blank'>{fileName}</a></td><td>{Version}</td><td>{Note}</td><td><button class='btn btn-sm btn-secondary' onclick='editDrawing({draw.DrawingID})'>Sửa</button><button class='btn btn-sm btn-danger' onclick='deleteDrawing({draw.DrawingID})'>Xóa</button></td></tr>";

                    return Json(new { success = true, html });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult AddPart(int JigID, int SpareID, int Qty, string Note)
        {
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    var spare = db.SparePartStocks.FirstOrDefault(x => x.SpareID == SpareID);
                    if (spare == null)
                        return Json(new { success = false, message = "Không tìm thấy linh kiện trong SparePartStocks." });

                    var part = new JigPartList
                    {
                        JigID = JigID,
                        SpareID = SpareID,
                        Qty = Qty,
                        Note = Note
                    };
                    db.JigPartList.Add(part);
                    db.SaveChanges();

                    string html = $@"
                <tr id='part_{part.ID}'>
                    <td>{spare.Code}</td>
                    <td>{spare.Name}</td>
                    <td>{spare.Maker}</td>
                    <td>{Qty}</td>
                    <td>{Note}</td>
                    <td>
                        <button class='btn btn-sm btn-secondary' onclick='editPart({part.ID})'>Sửa</button>
                        <button class='btn btn-sm btn-danger' onclick='deletePart({part.ID})'>Xóa</button>
                    </td>
                </tr>";

                    return Json(new { success = true, html });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpGet]
        public JsonResult SearchSparePart(string term)
       {
            using (var db = new Jig_system_controlEntities())
            {
                term = term ?? "";
                var result = db.SparePartStocks
                    .Where(x => x.Code.Contains(term) || x.Name.Contains(term) || x.Maker.Contains(term))
                    .OrderBy(x => x.Code)
                    .Take(20)
                    .Select(x => new
                    {
                        x.SpareID,
                        x.Code,
                        x.Name,
                        x.Maker
                    })
                    .ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditInfo(int JigID, string JigName, string Model, string Type, string Range, string Note)
        {
            try
            {
                using (var db = new Jig_system_controlEntities())
                {
                    var jig = db.Jig.FirstOrDefault(x => x.JigID == JigID);
                    if (jig == null) return Json(new { success = false, message = "Không tìm thấy Jig" });

                    jig.JigName = JigName;
                    jig.Model = Model;
                    jig.Type = Type;
                    jig.Range = Range;
                    jig.Note = Note;

                    db.SaveChanges();
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

  
       


    }
}