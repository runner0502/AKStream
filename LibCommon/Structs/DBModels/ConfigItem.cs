using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;

namespace LibCommon.Structs.DBModels
{
    [Serializable]
    [Table(Name = "ConfigItem")]
    public class ConfigItem
    {
        [Column(IsPrimary = true, IsIdentity = true)]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
