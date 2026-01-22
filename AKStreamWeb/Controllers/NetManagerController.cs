using LibCommon.Structs.GB28181;
using LibCommon;
using LibGB28181SipServer;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using XyCallLayer;
using Newtonsoft.Json;
using AKStreamWeb.Misc;
using LibCommon.Structs.DBModels;
using System.Numerics;

namespace AKStreamWeb.Controllers
{
    public class NetManagerController
    {
        /// <summary>
        /// 获取服务运行状态
        /// </summary>
        /// <param name="AccessKey"></param>
        /// <returns>运行状态 （ 1： 正常状态，2： 未授权状态）</returns>
        [Route("GetServerStatus")]
        [HttpGet]
        public int GetServerStatus(
            [FromHeader(Name = "AccessKey")] string AccessKey)
        {
            if (!Common.s_licenceVaid)
            {
                return 2;
            }
            else
            {
                return 1;
            }
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
            List<CallInfo> infos = new List<CallInfo>();
            foreach (var item in Bridge.s_calls)
            {
                CallInfo info = new CallInfo();
                info.CallId = item.Key.ToString();
                //info.OtherNumber = item.Value.SipChannel.DeviceId;
                info.CameraId = item.Value.SipChannel.DeviceId;
                info.CameraName = item.Value.SipChannel.SipChannelDesc.Name;
                info.IsTranscode = item.Value.IsTranscode;
                info.CalledReslution = item.Value.Reslution;
                info.StartTime = item.Value.CreateTime;
                info.Caller = item.Value.caller;
                info.OtherNumber = item.Value.called;
                info.CalledPlat = item.Value.SipChannel.ParentId;
                info.CallerIP = item.Value.CallerIP;
                info.CalledDeviceId = item.Value.SipChannel.DeviceId;
                info.CalledDeviceName = item.Value.CameraName;
                info.CalledDeviceNumber = item.Value.calledDeviceNumber;

                infos.Add(info);
            }
            return infos;
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
            var call = Bridge.s_calls[int.Parse(callId)];
            if (call != null) 
            {
                SPhoneSDK.Hangup(int.Parse(callId));
                return true;
            }
            return false;
        }

        public void CountOrgDevice(string deviceid, SyncStateFull state)
        {
            state.State.DeviceCountBefore += (int)ORMHelper.Db.Select<DeviceNumber>().Where(a => a.fatherid == deviceid).Count();
            var orgs = ORMHelper.Db.Select<organization>().Where(a => a.super_id == deviceid).ToList();
            if (orgs  != null && orgs.Count > 0)
            {
                state.State.orgCountBefore += orgs.Count;
                foreach (var item in orgs)
                {
                    CountOrgDevice(item.id, state);
                }
            }
        }

        private bool StartSync(string deviceId, SyncMethod method, BigInteger startIndex)
        {
            GCommon.Logger.Info("starsync deviceid :" +deviceId);
            if (SipServerCallBack.SsyncState == null)
            {
                SipServerCallBack.SsyncState = new SyncStateFull();
            }
            if (SipServerCallBack.SsyncState.State.IsProcessing)
            {
                GCommon.Logger.Warn("sync catalog startsync isProcessing");
                GCommon.Logger.Warn("同步目录异常： 当前正在同步，请等待当前同步完成或者停止当前同步后，再发起同步");
                return false;
            }
            SipServerCallBack.SsyncState.PlatId = deviceId;
            SipServerCallBack.SsyncState.State.IsProcessing = true;

            SipServerCallBack.SsyncState.Method = method;
            SipServerCallBack.SsyncState.SyncStartIndex = startIndex;

            CountOrgDevice(deviceId, SipServerCallBack.SsyncState);
            //SipServerCallBack.SsyncState.State.orgCountBefore =  (int)ORMHelper.Db.Select<organization>().Where(a=> a.super_id == deviceId).Count();
            //SipServerCallBack.SsyncState.State.DeviceCountBefore =  (int)ORMHelper.Db.Select<DeviceNumber>().Where(a=> a.fatherid == deviceId).Count();
            SipServerCallBack.SsyncState.Devices.Clear();
            SipServerCallBack.SsyncState.Orgs.Clear();
            SipMethodProxy sipMethodProxy = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
            ResponseStruct rs;
            var sipDevice = LibGB28181SipServer.Common.SipDevices.FindLast(x => x.DeviceInfo!.DeviceID.Equals(deviceId));
            if (sipDevice == null)
            {
                SipServerCallBack.SsyncState.SyncStartIndex = 0;
                SipServerCallBack.SsyncState.State.IsProcessing = false;
                GCommon.Logger.Warn("同步目录异常： 设备不存在或者不在线 deviceid: " + deviceId);
                return false;
            }
            SipServerCallBack.SsyncState.StartTime = DateTime.Now;
            if (sipMethodProxy.DeviceCatalogQuery(sipDevice, out rs))
            {
                GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->设备目录获取成功()->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(sipDevice.SipChannels, Formatting.Indented)}");
            }
            else
            {
                StopSyncInternal();
                GCommon.Logger.Warn("同步目录异常： 设备没有发送目录信息");
                GCommon.Logger.Error(
                    $"[{Common.LoggerHead}]->设备目录获取失败()->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(rs, Formatting.Indented)}");
            }
            return true;
        }
        /// <summary>
        /// 开始同步（号码和ID一致）
        /// </summary>
        /// <param name="deviceId">设备或者网关ID</param>
        /// <returns></returns>
        [Route("StarSyncSameId")]
        [HttpPost]
        public bool StarSyncSameId([FromHeader(Name = "AccessKey")] string AccessKey, string deviceId)
        {
            return StartSync(deviceId, SyncMethod.SameAsId, 0);
        }
        /// <summary>
        /// 开始同步（保留原号码）
        /// </summary>
        /// <param name="deviceId">设备或者网关ID</param>
        /// <returns></returns>
        [Route("StarSyncKeepOrg")]
        [HttpPost]
        public bool StarSyncKeepOrg([FromHeader(Name = "AccessKey")] string AccessKey, string deviceId)
        {
            return StartSync(deviceId, SyncMethod.KeepOrg, 0);
        }
        /// <summary>
        /// 开始同步（指定起始号码自增）
        /// </summary>
        /// <param name="deviceId">设备或者网关ID</param>
        /// <param name="startIndexNumber">起始号码</param>
        /// <returns></returns>
        [Route("StarSyncIncreanWithIndex")]
        [HttpPost]
        public bool StarSyncIncreanWithIndex([FromHeader(Name = "AccessKey")] string AccessKey, string deviceId, string startIndexNumber)
        {
            var startIndex = BigInteger.Parse(startIndexNumber);
            return StartSync(deviceId, SyncMethod.StartFromIndex, startIndex);
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
            return StopSyncInternal();
        }

        private static bool StopSyncInternal()
        {
            //SipServerCallBack.UpdateCatelogToDB();

            if (SipServerCallBack.SsyncState != null && SipServerCallBack.SsyncState.State.IsProcessing)
            {
                SipServerCallBack.SsyncState.State.IsProcessing = false;
                SipServerCallBack.SsyncState.PlatId = "";
                SipServerCallBack.SsyncState.Devices.Clear();
                SipServerCallBack.SsyncState.Orgs.Clear();
                SipServerCallBack.SsyncState.State.LastResult = false;
                SipServerCallBack.SsyncState.State.orgCountBefore = 0;
                SipServerCallBack.SsyncState.State.DeviceCountBefore = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取同步状态
        /// </summary>
        /// <param name="AccessKey"></param>
        /// <param name="deviceId"> 设备ID</param>
        /// <returns></returns>
        [Route("GetSyncState")]
        [HttpGet]
        public SyncState GetSyncState([FromHeader(Name = "AccessKey")] string AccessKey, string deviceId)
        {
            if (SipServerCallBack.SsyncState == null)
            {
                SipServerCallBack.SsyncState = new SyncStateFull();
            }

            SipServerCallBack.SsyncState.State.orgCount = SipServerCallBack.SsyncState.Orgs.Count;
            SipServerCallBack.SsyncState.State.DeviceCount= SipServerCallBack.SsyncState.Devices.Count;
            return SipServerCallBack.SsyncState.State;
        }
        [Route("GetSystemLog")]
        [HttpGet]
        public List<SystemLog> GetSystemLog([FromHeader(Name = "AccessKey")] string AccessKey)
        {
            var logs = ORMHelper.Db.Select<SystemLog>().OrderByDescending(a => a.Timestamp).Limit(100).ToList();
            return logs;
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
            /// <summary>
            /// 被叫设备号码
            /// </summary>
            public string CalledDeviceNumber { get; set; }
            /// <summary>
            /// 被叫设备名称
            /// </summary>
            public string CalledDeviceName { get; set; }
            /// <summary>
            /// 被叫设备ID
            /// </summary>
            public string CalledDeviceId { get; set; }
            /// <summary>
            /// 被叫所属平台
            /// </summary>
            public string CalledPlat { get; set; }
            /// <summary>
            /// 被叫分辨率
            /// </summary>
            public string CalledReslution { get; set; }
            /// <summary>
            /// 呼叫创建时间
            /// </summary>
            public DateTime StartTime { get; set; }
            /// <summary>
            /// 主叫IP
            /// </summary>
            public string CallerIP { get; set; }

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
            /// <summary>
            /// 是否正在同步
            /// </summary>
            public bool IsProcessing { get; set; }
            /// <summary>
            /// 上次同步是否成功
            /// </summary>
            public bool LastResult { get; set; }
        }

    }

}
