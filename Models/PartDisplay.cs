using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Jig_system_control.Models
{
    public class PartDisplay
    {
        public int PartID { get; set; }
        public int SpareID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Maker { get; set; }
        public int Qty { get; set; }
        public string Note { get; set; }
    }

}