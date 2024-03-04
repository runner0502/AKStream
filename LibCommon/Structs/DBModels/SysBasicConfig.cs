using FreeSql.DatabaseModel;using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FreeSql.DataAnnotations;

namespace LinCms.Core.Entities {

	/// <summary>
	/// 基础配置表
	/// </summary>
	[JsonObject(MemberSerialization.OptIn), Table(Name = "sys_basic_config", DisableSyncStructure = true)]
	public partial class SysBasicConfig {

		/// <summary>
		/// 主键
		/// </summary>
		[JsonProperty, Column(Name = "id", StringLength = 20, IsPrimary = true, IsNullable = false)]
		public string Id { get; set; }

		
		/// <summary>
		/// 网关ID
		/// </summary>
		[JsonProperty, Column(Name = "gateway_code", IsNullable = false)]
		public string GatewayCode { get; set; }

		/// <summary>
		/// 公网ip地址
		/// </summary>
		[JsonProperty, Column(Name = "gateway_ip", StringLength = 50)]
		public string GatewayIp { get; set; }

		/// <summary>
		/// 网关名称
		/// </summary>
		[JsonProperty, Column(Name = "gateway_name", IsNullable = false)]
		public string GatewayName { get; set; }


		/// <summary>
		/// 媒体端口（结束值）
		/// </summary>
		[JsonProperty, Column(Name = "media_port_end", DbType = "int")]
		public int MediaPortEnd { get; set; }

		/// <summary>
		/// 媒体端口（起始值）
		/// </summary>
		[JsonProperty, Column(Name = "media_port_start", DbType = "int")]
		public int MediaPortStart { get; set; }

		/// <summary>
		/// 信令端口
		/// </summary>
		[JsonProperty, Column(Name = "signal_port", DbType = "int")]
		public int SignalPort { get; set; }

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

