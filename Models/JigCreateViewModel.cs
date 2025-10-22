using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Jig_system_control.Models
{
    public class JigCreateViewModel
    {

            public JigCreateViewModel()
            {
                Documents = new List<DocumentUploadVM>();
                Drawing = new List<DrawingUploadVM>();
                PartList = new List<PartItemVM>();
            }

            [Required]
            public string JigCode { get; set; }

            [Required]
            public string JigName { get; set; }

            public string Version { get; set; }
            public string Model { get; set; }
            public string Type { get; set; }
            public string Range { get; set; }
            public string Note { get; set; }
          public HttpPostedFileBase Image { get; set; }

        public List<DocumentUploadVM> Documents { get; set; }
            public List<DrawingUploadVM> Drawing { get; set; }
        public string PartListJson { get; set; }
            public List<PartItemVM> PartList { get; set; }
        }

        public class DocumentUploadVM
        {
            public string DocType { get; set; }
            public HttpPostedFileBase File { get; set; }  
            public string Version { get; set; }
            public string Note { get; set; }
        }

        public class DrawingUploadVM
        {
            public string DocType { get; set; }
            public HttpPostedFileBase File { get; set; }
            public string Version { get; set; }
            public string Note { get; set; }
        }

        public class PartItemVM
        {
            public int SpareID { get; set; }
            public int Qty { get; set; }
            public string Note { get; set; }
        }
    }
