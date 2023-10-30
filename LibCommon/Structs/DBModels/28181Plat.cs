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

        public int port { get; set; }

        public string username { get; set; }

        public string userpwd { get; set; }
        //public int 

    }
}
