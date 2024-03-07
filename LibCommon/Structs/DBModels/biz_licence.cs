using FreeSql.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCommon.Structs.DBModels
{
    /// <summary>
    /// 授权信息表
    /// </summary>
    [Serializable]
    [Table(Name = "biz_licence")]
    public class biz_licence
    {
        [Column(DbType = "varchar(20) NOT NULL")]
        public string id { get; set; }

        //[Column(DbType = "varchar(20) NOT NULL")]
        //public string create_by { get; set; }
        //[Column(DbType = "varchar(20) NOT NULL")]
        //public string create_time { get; set; }
        //[Column(DbType = "varchar(20) NOT NULL")]
        //public string update_by { get; set; }
        //[Column(DbType = "varchar(20)")]
        //public string update_time { get; set; }

        //[Column(DbType = "varchar(20)")]
        //public int is_deleted { get; set; }
        /// <summary>
        /// 最大转码并发数量
        /// </summary>
        [Column(DbType = "int(32)")]
        public int max_transcode_number { get; set; }
        /// <summary>
        /// 最大设备接入数量
        /// </summary>
        [Column(DbType = "int(32)")]
        public int max_device_number { get; set; }
        /// <summary>
        /// 有效授权期
        /// </summary>
        public DateTime expire { get; set; }


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
