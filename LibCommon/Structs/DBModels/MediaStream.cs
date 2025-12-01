using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LibCommon.Structs.DBModels
{
    /// <summary>
    /// 流媒体信息表
    /// </summary>
    [Serializable]
    [Table(Name = "media_stream")]
    public class MediaStream
    {
        /// <summary>
        /// id
        /// </summary>
        [Column(IsPrimary = true, IsIdentity = true)]
        public long id { get; set; }
        /// <summary>
        ///  设备名称
        /// </summary>
        [Column(DbType = "varchar(255)")]
        public string name { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        [Column(DbType = "varchar(255)")]
        public string type { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        [Column(DbType = "varchar(255)")]
        public string device_type { get; set; }

        /// <summary>
        /// 推流地址
        /// </summary>
        [Column(DbType = "varchar(255)")]
        public string stream_push_url { get; set; }
        /// <summary>
        /// 拉流地址
        /// </summary>
        [Column(DbType = "varchar(255)")]
        public string stream_pull_url { get; set; }

        /// <summary>
        /// 状态（0：停止， 1： 正在推流）
        /// </summary>
        //[Column(DbType = "int(11)", IsNullable = true)]
        public int state { get; set; }

        /// <summary>
        /// 推流IP
        /// </summary>
        [Column(DbType = "varchar(255)")]
        public string stream_push_ip { get; set; }


        /// <summary>
        /// 推流端口
        /// </summary>
        //[Column(DbType = "int(11)", IsNullable = true)]
        public int stream_push_port { get; set; }
        /// <summary>
        /// 推流端口
        /// </summary>
        //[Column(DbType = "int(11)", IsNullable = true)]
        public int stream_pull_port { get; set; }

        /// <summary>
        /// 拉流类型
        /// </summary>
        [Column(DbType = "varchar(255)")]
        public string stream_pull_type { get; set; }

    }
}
