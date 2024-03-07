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
    /// 转码表
    /// </summary>
    [Serializable]
    [Table(Name = "biz_transcode")]
    public class biz_transcode
    {
        [Column(DbType = "varchar(20) NOT NULL")]
        public string id { get; set; }
        //[Column(DbType = "varchar(20) NOT NULL")]
        //public string create_by { get; set; }
        //[Column(DbType = "varchar(20) NOT NULL")]
        //public string create_time { get; set; }
        //[Column(DbType = "varchar(20) NOT NULL")]
        //public string update_by { get; set; }
        //[Column(DbType = "varchar(20) NOT NULL")]
        //public string update_time { get; set; }
        //[Column(DbType = "varchar(20) NOT NULL")]
        //public int is_deleted { get; set; }
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
        /// <summary>
        /// 编码格式（0 为H264, 1 为H265）
        /// </summary>
        public int EncoderType { get; set; }


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
