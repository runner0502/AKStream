using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using Newtonsoft.Json;

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

        /// <summary>
        /// 修改人
        /// </summary>
        [JsonProperty, Column(Name = "update_by", DbType = "bigint")]
        public long? UpdateBy { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonProperty, Column(Name = "update_time", DbType = "datetime")]
        public DateTime? UpdateTime { get; set; }
        /// <summary>
        /// 创建人
        /// </summary>
        [JsonProperty, Column(Name = "create_by", DbType = "bigint")]
        public long? CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonProperty, Column(Name = "create_time", DbType = "datetime")]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 是否已删除
        /// </summary>
        [JsonProperty, Column(Name = "is_deleted", DbType = "int")]
        public int IsDeleted { get; set; } = 0;

    }
}
