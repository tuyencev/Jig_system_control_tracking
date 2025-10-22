using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Jig_system_control.Models
{
    public class RepariJigDetail
    {
    
            public Jig Jig { get; set; }
            public JigRepairHistory Repair { get; set; }
            public List<JigRepairHistory> History { get; set; }
            public List<danhsachpartlist> PartList { get; set; }
       

    }

    public class danhsachpartlist
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Ghichu { get; set; }
    }
}