using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;


namespace LibCommon.Structs.DBModels
{
    [Serializable]
    [Table(Name = "deviceplus")]
    public class DevicePlus
    {
        [Column(IsPrimary = true, DbType = "varchar(100)")]
        public string id { get; set; }
        [Column(DbType = "varchar(50)")]
        public string type { get; set; }
        [Column(DbType = "varchar(1024)")]
        public string info { get; set; }

    }
}
