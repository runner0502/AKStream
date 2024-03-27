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
	/// 高级配置表
	/// </summary>
	[JsonObject(MemberSerialization.OptIn), Table(Name = "sys_advanced_config")]
	public partial class SysAdvancedConfig {

		/// <summary>
		/// 主键
		/// </summary>
		[JsonProperty, Column(Name = "id", StringLength = 20, IsPrimary = true, IsNullable = false)]
		public string Id { get; set; }

		/// <summary>
		/// 是否启用鉴权功能：0否 1是
		/// </summary>
		[JsonProperty, Column(Name = "auth_enable", DbType = "int")]
		public int AuthEnable { get; set; } = 1;

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
		/// 是否启用GPS位置上报：0否 1是
		/// </summary>
		[JsonProperty, Column(Name = "gps_enable", DbType = "int")]
		public int GpsEnable { get; set; } = 1;

		/// <summary>
		/// 是否启用语音对讲：0否 1是
		/// </summary>
		[JsonProperty, Column(Name = "intercom_enable", DbType = "int")]
		public int IntercomEnable { get; set; } = 1;

        /// <summary>
        /// 是否已删除
        /// </summary>
        [JsonProperty, Column(Name = "is_deleted", DbType = "int", IsNullable = true)]
        public int IsDeleted { get; set; } = 0;

		/// <summary>
		/// 是否启用转码功能：0否 1是
		/// </summary>
		[JsonProperty, Column(Name = "transcode_enable", DbType = "int")]
		public int TranscodeEnable { get; set; } = 1;

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

	}

}
