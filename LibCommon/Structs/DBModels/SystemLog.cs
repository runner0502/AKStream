using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCommon.Structs.DBModels
{
    [Serializable]
    [Table(Name = "SystemLog")]
    public class SystemLog
    {
        [Column(IsPrimary = true, IsIdentity = true)]
        public long Id { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
