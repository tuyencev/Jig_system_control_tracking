using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Jig_system_control.Models
{
    public class addspare
    {
      
            public string Code { get; set; }
            public string Name { get; set; }
            public string Maker { get; set; }
            public string Model { get; set; }
            public string Unit { get; set; }
            public int? CurrentQty { get; set; }
            public string Location { get; set; }
            public string Remark { get; set; }
        }
    
}