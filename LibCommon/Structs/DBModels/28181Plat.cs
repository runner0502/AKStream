using FreeSql.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCommon.Structs.DBModels
{
    [Serializable]
    [Table(Name = "biz_platform")]
    public class Device281Plat
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Column(IsPrimary = true, IsIdentity = true)]
        public long id { get; set; }
        /// <summary>
        /// 平台ID
        /// </summary>
        public string platid { get; set; }
        /// <summary>
        /// 平台名称
        /// </summary>
        public string platname { get; set; }
        /// <summary>
        /// IP
        /// </summary>
        public string ipaddr { get; set; }
        /// <summary>
        /// 端口
        /// </summary>
        public int port { get; set; }
        /// <summary>
        /// 注册用户名
        /// </summary>
        public string username { get; set; }
        /// <summary>
        /// 注册密码
        /// </summary>
        public string userpwd { get; set; }
        /// <summary>
        /// 厂商
        /// </summary>
        public int manufacturer { get; set; }
        /// <summary>
        /// SIP协议
        /// </summary>
        public int sipprotocol { get; set; }
        /// <summary>
        /// 注册模式
        /// </summary>
        public int registemode { get; set; }
        /// <summary>
        /// 注册状态{1 在线}
        /// </summary>
        public int registestate { get; set; }
        /// <summary>
        /// 类型（0 平台， 1 设备）
        /// </summary>
        public int type { get; set; }
        /// <summary>
        /// 同步数据逻辑（1 CivilCode优先， 2 deviceid 前6位）
        /// </summary>
        public int parent_id_get { get; set; }
        /// <summary>
        /// 未码流处理方式
        /// </summary>
        public int not_analyze_way { get; set; }
        /// <summary>
        /// 平台IP格式（0 非动态IP， 1 动态IP）
        /// </summary>
        public int isdynamic_ip { get; set; }
        /// <summary>
        /// 类型（0 平台， 1 设备）
        /// </summary>
        public int plat_type { get; set; }


    }
}
