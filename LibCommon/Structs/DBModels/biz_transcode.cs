using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCommon.Structs.DBModels
{
    /// <summary>
    /// 转码表
    /// </summary>
    [Serializable]
    [Table(Name = "biz_transcode")]
    public class biz_transcode
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
        /// 名称
        /// </summary>
        [Column(DbType = "varchar(30)")]
        public string name { get; set; }
        /// <summary>
        /// 主叫号码
        /// </summary>
        [Column(DbType = "varchar(30) NOT NULL")]
        public string caller_number { get; set; }
        /// <summary>
        /// 分辨率
        /// </summary>
        [Column(DbType = "varchar(30) NOT NULL")]
        public string reslution { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [Column(DbType = "varchar(30) NOT NULL")]
        public string state{ get; set; }


    }
}
