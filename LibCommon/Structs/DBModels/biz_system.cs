using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCommon.Structs.DBModels
{
    /// <summary>
    /// 自定义键值表
    /// </summary>
    [Serializable]
    [Table(Name = "biz_system")]
    public class biz_system
    {
        [Column(DbType = "varchar(20) NOT NULL")]
        public string id { get; set; }
        [Column(DbType = "varchar(20) NOT NULL")]
        public string create_by { get; set; }
        [Column(DbType = "varchar(20) NOT NULL")]
        public string create_time { get; set; }
        [Column(DbType = "varchar(20) NOT NULL")]
        public string update_by { get; set; }
        [Column(DbType = "varchar(20) NOT NULL")]
        public string update_time { get; set; }
        [Column(DbType = "varchar(20) NOT NULL")]
        public int is_deleted { get; set; }
        /// <summary>
        /// 自定义键：gatewayName(网关名称)、gatewayId(网关ID)、sipIP(IP)、sipPort(端口)
        /// </summary>
        [Column(DbType = "varchar(200)")]
        public string thekey { get; set; }
        /// <summary>
        /// 自定义值
        /// </summary>
        [Column(DbType = "varchar(200) NOT NULL")]
        public string thevalue { get; set; }

    }
}
