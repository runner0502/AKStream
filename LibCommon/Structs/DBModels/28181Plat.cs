using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCommon.Structs.DBModels
{
    [Serializable]
    [Table(Name = "28181Plat")]
    public class Device281Plat
    {

        public string platid { get; set; }

        public string platname { get; set; }
        public string ipaddr { get; set; }

        public int port { get; set; }

        public string username { get; set; }

        public string userpwd { get; set; }

        public int manufacturer { get; set; }
        public int sipprotocol { get; set; }
        public int registemode { get; set; }
        public int registestate { get; set; }
        public int type { get; set; }
        public int parent_id_get { get; set; }
        public int not_analyze_way { get; set; }
        public int isdynamic_ip { get; set; }
        public int plat_type { get; set; }

        //public int 

    }
}
