using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AKStreamWeb.Controllers
{
    public class NetManagerController
    {
        /// <summary>
        /// 获取服务运行状态， 运行状态正常返回true, 否则返回false
        /// </summary>
        /// <param name="AccessKey"></param>
        /// <returns>运行状态正常返回true, 否则返回false</returns>
        [Route("GetServerStatus")]
        [HttpGet]
        public bool GetServerStatus(
            [FromHeader(Name = "AccessKey")] string AccessKey)
        {
            return true;
        }
        /// <summary>
        /// 获取呼叫列表
        /// </summary>
        /// <param name="AccessKey"></param>
        /// <returns></returns>
        [Route("GetCallsInfo")]
        [HttpGet]
        public List<CallInfo> GetCallsInfo(
            [FromHeader(Name = "AccessKey")] string AccessKey)
        {
            return null;
        }
        /// <summary>
        /// 挂断呼叫
        /// </summary>
        /// <param name="AccessKey"></param>
        /// <param name="callId">呼叫ID </param>
        /// <returns></returns>
        [Route("HangupCall")]
        [HttpPost]
        public bool HangupCall(
            [FromHeader(Name = "AccessKey")] string AccessKey, string callId)
        {
            return true;
        }
        /// <summary>
        /// 开始同步
        /// </summary>
        /// <param name="deviceId">设备或者网关ID</param>
        /// <returns></returns>
        [Route("StartSync")]
        [HttpPost]
        public bool StarSync([FromHeader(Name = "AccessKey")] string AccessKey, string deviceId)
        {
            return true;
        }
        /// <summary>
        /// 结束同步
        /// </summary>
        /// <param name="deviceId">设备或者网关ID</param>
        /// <returns></returns>
        [Route("StopSync")]
        [HttpPost]
        public bool StopSync([FromHeader(Name = "AccessKey")] string AccessKey, string deviceId)
        {
            return true;
        }
        [Route("GetSyncState")]
        [HttpGet]
        public SyncState GetSyncState([FromHeader(Name = "AccessKey")] string AccessKey, string deviceId)
        {
            return null;
        }
        /// <summary>
        /// 呼叫信息
        /// </summary>
        public class CallInfo
        {
            /// <summary>
            /// 呼叫ID
            /// </summary>
            public string CallId { get; set; }
            /// <summary>
            /// 摄像头ID
            /// </summary>
            public string CameraId { get; set; }
            /// <summary>
            /// 摄像头名称
            /// </summary>
            public string CameraName { get; set; }
            /// <summary>
            /// 主叫号码
            /// </summary>
            public string Caller { get; set; }
            /// <summary>
            /// 被叫号码
            /// </summary>
            public string OtherNumber { get; set; }
            /// <summary>
            /// 是否转码
            /// </summary>
            public bool IsTranscode { get; set; }
        }
        /// <summary>
        /// 同步状态
        /// </summary>
        public class SyncState
        {
            /// <summary>
            /// 当前同步部门数量
            /// </summary>
            public int orgCount { get; set; }
            /// <summary>
            /// 当前同步设备数量
            /// </summary>
            public int DeviceCount { get; set; }
            /// <summary>
            /// 同步前部门数量
            /// </summary>
            public int orgCountBefore { get; set; }
            /// <summary>
            /// 同步前设备数量
            /// </summary>
            public int DeviceCountBefore { get; set; }
        }
    }

}
