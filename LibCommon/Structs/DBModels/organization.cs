using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;

namespace LibCommon.Structs.DBModels
{
    [Serializable]
    [Table(Name = "organization")]
    public class organization
    {
        public string id { get; set; }
        public string name { get; set; }
        public string super_id { get; set; }

        public string ldap { get; set; }
        public string domain { get; set; }
        public int order_num { get; set; }

    }
}
