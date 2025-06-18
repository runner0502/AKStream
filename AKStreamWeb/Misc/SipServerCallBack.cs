using Google.Protobuf.WellKnownTypes;
using LibCommon;
using LibCommon.Enums;
using LibCommon.Structs.DBModels;
using LibCommon.Structs.GB28181;
using LibCommon.Structs.GB28181.Sys;
using LibCommon.Structs.GB28181.XML;
using LibGB28181SipServer;
using LinCms.Core.Entities;
using NetTaste;
using NetTopologySuite.Triangulate;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace AKStreamWeb.Misc
{
    /// <summary>
    /// sip设备回调类
    /// </summary>
    public static class SipServerCallBack
    {
        /// <summary        /// 当设备注册需要鉴权时，用于获取外部的设备密钥
        /// </summary>
        /// <param name="sipDeviceId"></param>
        /// <returns>返回此设备的密钥</returns>
        public static string OnAuthentication(string sipDeviceId)
        {

            var obj1 = ORMHelper.Db.Select<Device281Plat>().Where(x =>
x.platid == sipDeviceId).First();
            if (obj1 != null)
            {
                return obj1.userpwd;
            }
            else
            {
                return "notau";
            }
        }

        public static void OnRegister(string sipDeviceJson)
        {
            //设备注册时
            var sipDevice = JsonHelper.FromJson<SipDevice>(sipDeviceJson);

            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->设备就绪(OnRegister)->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}");

            Bridge.GetInstance().Subcribe();

            ResponseStruct rs;
            SipMethodProxy sipMethodProxy2 = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
            if (sipMethodProxy2.GetSipDeviceInfo(sipDevice, out rs))
            {
                GCommon.Logger.Debug(
                    $"[{Common.LoggerHead}]->获取设备信息成功(OnRegister)->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(sipDevice.DeviceInfo, Formatting.Indented)}");
            }
            else
            {
                GCommon.Logger.Warn(
                    $"[{Common.LoggerHead}]->获取设备信息失败(OnRegister)->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(rs, Formatting.Indented)}");
            }

            SipMethodProxy sipMethodProxy3 = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
            if (sipMethodProxy3.GetSipDeviceStatus(sipDevice, out rs))
            {
                GCommon.Logger.Debug(
                    $"[{Common.LoggerHead}]->获取设备状态信息成功(OnRegister)->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(sipDevice.DeviceStatus, Formatting.Indented)}");
            }
            else
            {
                GCommon.Logger.Warn(
                    $"[{Common.LoggerHead}]->获取设备状态信息失败(OnRegister)->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(rs, Formatting.Indented)}");
            }

            var obj1 = ORMHelper.Db.Update<Device281Plat>().Where(x =>
            x.platid == sipDevice.DeviceId).Set(x => x.registestate, 1).ExecuteAffrows();
            var plate = ORMHelper.Db.Select<Device281Plat>().Where(x => x.platid == sipDevice.DeviceId).First();
            if (plate != null && plate.plat_type == 1)
            {
                ORMHelper.Db.Update<DeviceNumber>().Where(x => x.fatherid == sipDevice.DeviceId).Set(x => x.status, 1).ExecuteAffrows();
                
                var config1 = ORMHelper.Db.Select<SysAdvancedConfig>().First();
                if (config1 != null)
                {
                    var devices = ORMHelper.Db.Select<DeviceNumber>().Where(x => x.fatherid == sipDevice.DeviceId).ToList();
                    foreach (var item in devices)
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            var formData = new MultipartFormDataContent();
                            // 添加表单数据
                            formData.Add(new StringContent(config1.PushGisType), "mqType");

                            formData.Add(new StringContent(config1.PushRegistStateTopic), "topic");
                            info1 info1 = new info1();
                            info1.user = item.dev;
                            info1.name = "2";
                            info1.type = "3";
                            info1.subtype = "213";
                            info1.status = 1;

                            var deviceinfo = ORMHelper.Db.Select<DevicePlus>().Where(a => a.id == info1.user).First();
                            if (deviceinfo != null)
                            {
                                info1.DeviceType = deviceinfo.type;
                                info1.DeviceInfo = deviceinfo.info;
                            }
                            info1.sipnum = item.num;
                            var data = JsonHelper.ToJson(info1, Formatting.None, MissingMemberHandling.Error);
                            formData.Add(new StringContent(data), "body");

                            var httpRet = client.PostAsync(config1.PushGisUrl, formData).Result.Content.ReadAsStringAsync();
                            GCommon.Logger.Warn("catalognotify send http regist " + info1.ToJson() + ", " + httpRet);
                        }
                    }
                }
            }

        //SipMethodProxy sipMethodProxy = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
        //if (sipMethodProxy.DeviceCatalogQuery(sipDevice, out rs))
        //{
        //    GCommon.Logger.Debug(
        //        $"[{Common.LoggerHead}]->设备目录获取成功(OnRegister)->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(sipDevice.SipChannels, Formatting.Indented)}");
        //}
        //else
        //{
        //    GCommon.Logger.Error(
        //        $"[{Common.LoggerHead}]->设备目录获取失败(OnRegister)->{sipDevice.IpAddress.ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(rs, Formatting.Indented)}");
        //}
    }

        public static void OnUnRegister(string sipDeviceJson)
        {
            GCommon.Logger.Warn($"Unregister {sipDeviceJson}");
            //设备注销时，要清掉在线流
            var sipDevice = JsonHelper.FromJson<SipDevice>(sipDeviceJson);
            var obj1 = ORMHelper.Db.Update<Device281Plat>().Where(x =>
x.platid == sipDevice.DeviceId).Set(x => x.registestate, 0).ExecuteAffrows();

            ORMHelper.Db.Update<DeviceNumber>().Where(x =>
x.fatherid == sipDevice.DeviceId).Set(x => x.status, 0).ExecuteAffrows();

            lock (GCommon.Ldb.LiteDBLockObj)
            {
                GCommon.Ldb.VideoOnlineInfo.DeleteMany(x => x.DeviceId.Equals(sipDevice.DeviceId));
            }

            GCommon.Logger.Info(
                $"[{Common.LoggerHead}]->设备注销->{sipDevice.DeviceId}->所有通道-->注销成功");

            var config1 = ORMHelper.Db.Select<SysAdvancedConfig>().First();
            if (config1 != null)
            {
                var devices = ORMHelper.Db.Select<DeviceNumber>().Where(x=>x.fatherid == sipDevice.DeviceId).ToList();
                foreach (var item in devices)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var formData = new MultipartFormDataContent();
                        // 添加表单数据
                        formData.Add(new StringContent(config1.PushGisType), "mqType");

                        formData.Add(new StringContent(config1.PushRegistStateTopic), "topic");
                        info1 info1 = new info1();
                        info1.user = item.dev;
                        info1.name = "2";
                        info1.type = "3";
                        info1.subtype = "213";
                        info1.status = 0;

                        var deviceinfo = ORMHelper.Db.Select<DevicePlus>().Where(a => a.id == info1.user).First();
                        if (deviceinfo != null)
                        {
                            info1.DeviceType = deviceinfo.type;
                            info1.DeviceInfo = deviceinfo.info;
                        }
                        info1.sipnum = item.num;
                        var data = JsonHelper.ToJson(info1, Formatting.None, MissingMemberHandling.Error);
                        formData.Add(new StringContent(data), "body");

                        var httpRet = client.PostAsync(config1.PushGisUrl, formData).Result.Content.ReadAsStringAsync();
                        GCommon.Logger.Warn("catalognotify send http unregist " + info1.ToJson() + ", " + httpRet);
                    }
                }
            }
        }

        class info1
        {
            public string user { get; set; }
            public string name { get; set; }
            public string lat { get; set; }
            public string lon { get; set; }
            public string time { get; set; }
            public string type { get; set; }
            public string subtype { get; set; }
            public string DeviceType { get; set; }
            public string DeviceInfo { get; set; }
            public int status { get; set; }
            public string sipnum { get; set; }


        }

        public static void OnKeepalive(string deviceId, DateTime keepAliveTime, int lostTimes)
        {
            //设备有心跳时
        }

        public static void OnDeviceStatusReceived(SipDevice sipDevice, DeviceStatus deviceStatus)
        {
            //获取到设备状态时
            int state = 0;
            switch (deviceStatus.Status)
            {
                case "OK":
                    state = 1;
                    break;
                case "ON":
                    state = 1;
                    break;
                default:
                    break;
            }
            var obj1 = ORMHelper.Db.Update<Device281Plat>().Where(x =>
x.platid == sipDevice.DeviceId).Set(x=>x.registestate,state).ExecuteAffrowsAsync();
        }

        public static void OnInviteHistoryVideoFinished(RecordInfo.RecItem record)
        {
            //收到设备的录像文件列表时
        }

        public static void OnDeviceReadyReceived(SipDevice sipDevice)
        {
            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->设备就绪->{sipDevice.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipDevice.DeviceId}");
            ResponseStruct rs;
//            SipMethodProxy sipMethodProxy2 = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
//            if (sipMethodProxy2.GetSipDeviceInfo(sipDevice, out rs))
//            {
//                GCommon.Logger.Debug(
//                    $"[{Common.LoggerHead}]->获取设备信息成功->{sipDevice.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(sipDevice.DeviceInfo, Formatting.Indented)}");

//                var obj1 = ORMHelper.Db.Select<Device281Plat>().Where(x =>
//x.platid == sipDevice.DeviceId).First();
//                if (obj1 == null)
//                {
//                    Device281Plat plat = new Device281Plat();
//                    plat.platid = sipDevice.DeviceId;
//                    plat.platname = sipDevice.DeviceInfo.DeviceName;
//                    plat.ipaddr = sipDevice.IpAddress.ToString();
//                    plat.port = sipDevice.Port;
//                    plat.username = sipDevice.Username;
//                    plat.userpwd = sipDevice.Password;
//                    //plat.manufacturer = sipDevice.DeviceInfo.Manufacturer.ToString
//                    //if (sipDevice.DeviceStatus == DeviceStatus.)
//                    //{

//                    //}
//                    //plat.registestate = int.Parse(sipDevice.DeviceStatus.Status);
//                    ORMHelper.Db.Insert(plat).ExecuteAffrows();
//                }
//            }
//            else
//            {
//                GCommon.Logger.Warn(
//                    $"[{Common.LoggerHead}]->获取设备信息失败->{sipDevice.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(rs, Formatting.Indented)}");
//            }

            SipMethodProxy sipMethodProxy3 = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
            if (sipMethodProxy3.GetSipDeviceStatus(sipDevice, out rs))
            {
                GCommon.Logger.Debug(
                    $"[{Common.LoggerHead}]->获取设备状态信息成功->{sipDevice.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(sipDevice.DeviceStatus, Formatting.Indented)}");


                lock (sipDevice.SipChannelOptLock) //锁粒度在SipDevice中，不影响其他线程的效率
                {
                    var channels = ORMHelper.Db.Select<VideoChannel>().Where<VideoChannel>(a=>a.DeviceId == sipDevice.DeviceId).ToList<VideoChannel>();
                    foreach (var tmpChannelDev in channels)
                    {
                        if (!Common.s_licenceVaid)
                        {
                            GCommon.Logger.Warn("license fail 未授权");
                            break;    
                        }
                        //if (SipDevice.s_count > Common.License.MaxDeviceCount)
                        //{
                        //    GCommon.Logger.Warn("license fail 超过最大授权设备个数");
                        //    break;
                        //}
                        SipChannel sipChannelInList = sipDevice.SipChannels.FindLast(x =>
                            x.SipChannelDesc.DeviceID.Equals(tmpChannelDev.ChannelId));
                        if (sipChannelInList == null)
                        {
                            
                            var newSipChannel = new SipChannel()
                            {
                                LastUpdateTime = DateTime.Now,
                                LocalSipEndPoint = sipDevice.LocalSipEndPoint!,
                                PushStatus = PushStatus.IDLE,
                                RemoteEndPoint = sipDevice.RemoteEndPoint!,
                                SipChannelDesc = null,
                                ParentId = sipDevice.DeviceId,
                                DeviceId = tmpChannelDev.ChannelId,
                                TotalNumber = 10,
                            };
                            newSipChannel.SipChannelDesc = new Catalog.Item();
                            newSipChannel.SipChannelDesc.DeviceID = tmpChannelDev.ChannelId;
                            //if (tmpChannelDev.InfList != null)
                            //{
                            //    newSipChannel.SipChannelDesc.InfList = tmpChannelDev.InfList;
                            //}

                            newSipChannel.SipChannelStatus = DevStatus.ON;
                            newSipChannel.SipChannelType = LibGB28181SipServer.Common.GetSipChannelType(tmpChannelDev.ChannelId);
                            if (newSipChannel.SipChannelType == SipChannelType.AudioChannel ||
                                newSipChannel.SipChannelType == SipChannelType.VideoChannel)
                            {
                                var ret = UtilsHelper.GetSSRCInfo(sipDevice.DeviceId,
                                    newSipChannel.DeviceId);
                                newSipChannel.SsrcId = ret.Key;
                                newSipChannel.Stream = ret.Value;
                            }
                            
                            sipDevice.SipChannels.Add(newSipChannel);
                            SipDevice.s_count++;

                        }
                    }
                }
               // Bridge.GetInstance().Subcribe();
            }
            else
            {
                GCommon.Logger.Warn(
                    $"[{Common.LoggerHead}]->获取设备状态信息失败->{sipDevice.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(rs, Formatting.Indented)}");
            }

            //SipMethodProxy sipMethodProxy = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
            //if (sipMethodProxy.DeviceCatalogQuery(sipDevice, out rs))
            //{
            //    GCommon.Logger.Debug(
            //        $"[{Common.LoggerHead}]->设备目录获取成功->{sipDevice.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(sipDevice.SipChannels, Formatting.Indented)}");
            //}
            //else
            //{
            //    GCommon.Logger.Error(
            //        $"[{Common.LoggerHead}]->设备目录获取失败->{sipDevice.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipDevice.DeviceId}\r\n{JsonHelper.ToJson(rs, Formatting.Indented)}");
            //}
        }
        public static SyncStateFull SsyncState;
        private static object s_catelogLock = new object();
        /// <summary>
        /// 收到设备目录时
        /// </summary>
        /// <param name="sipChannel"></param>
        public static void OnCatalogReceived(SipChannel sipChannel)
        {
            lock (s_catelogLock)
            {
                GCommon.Logger.Debug(
                    $"[{Common.LoggerHead}]->收到一条设备目录通知->{sipChannel.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipChannel.ParentId}:{sipChannel.DeviceId}");


                if (sipChannel.SipChannelType.Equals(SipChannelType.VideoChannel))
                //&& sipChannel.SipChannelStatus != DevStatus.OFF) //只有视频设备并且是可用状态的进数据库
                {
                    #region debug sql output

                    if (Common.IsDebug)
                    {
                        var sql = ORMHelper.Db.Select<VideoChannel>().Where(x =>
                            x.ChannelId.Equals(sipChannel.DeviceId) && x.DeviceId.Equals(sipChannel.ParentId) &&
                            x.DeviceStreamType.Equals(DeviceStreamType.GB28181)).ToSql();

                        GCommon.Logger.Debug(
                            $"[{Common.LoggerHead}]->OnCatalogReceived->执行SQL:->{sql}");
                    }

                    #endregion

                    //ORMHelper.Db.Select<Device281Plat>().Where(x =>x.platid == )
                    //      ORMHelper.Db.Delete<DeviceNumber>().Where(x =>
                    //x.dev.Equals(sipChannel.DeviceId)).ExecuteAffrows();
                    GCommon.Logger.Debug("OnCatalogReceived: CreateDevice");
                    CreateDevice(sipChannel);
                    GCommon.Logger.Debug("OnCatalogReceived: CreateDevice1");

                    var obj = ORMHelper.Db.Select<VideoChannel>().Where(x =>
                        x.ChannelId.Equals(sipChannel.DeviceId) && x.DeviceId.Equals(sipChannel.ParentId) &&
                        x.DeviceStreamType.Equals(DeviceStreamType.GB28181)).First();
                    if (obj == null)
                    {
                        var videoChannel = new VideoChannel();
                        videoChannel.Enabled = true;
                        videoChannel.AutoRecord = false;
                        videoChannel.AutoVideo = false;
                        videoChannel.ChannelId = sipChannel.DeviceId;
                        if (sipChannel.SipChannelDesc != null && !string.IsNullOrEmpty(sipChannel.SipChannelDesc.Name))
                        {
                            videoChannel.ChannelName = sipChannel.SipChannelDesc.Name.Trim();
                        }
                        else
                        {
                            videoChannel.ChannelName = sipChannel.DeviceId;
                        }

                        videoChannel.CreateTime = DateTime.Now;
                        videoChannel.App = "rtp";
                        videoChannel.Vhost = "__defaultVhost__";
                        videoChannel.DepartmentId = "";
                        videoChannel.DepartmentName = "";
                        videoChannel.DeviceId = sipChannel.ParentId;
                        videoChannel.HasPtz = false;
                        videoChannel.UpdateTime = DateTime.Now;
                        videoChannel.DeviceNetworkType = DeviceNetworkType.Fixed;
                        videoChannel.DeviceStreamType = DeviceStreamType.GB28181;
                        videoChannel.DefaultRtpPort = false;
                        videoChannel.IpV4Address = sipChannel.RemoteEndPoint.Address.MapToIPv4().ToString();
                        videoChannel.IpV6Address = sipChannel.RemoteEndPoint.Address.MapToIPv6().ToString();
                        //videoChannel.MediaServerId = $"unknown_server_{DateTime.Now.Ticks}";
                        videoChannel.MediaServerId = "your_server_id";
                        videoChannel.NoPlayerBreak = true;
                        videoChannel.PDepartmentId = "";
                        videoChannel.PDepartmentName = "";
                        videoChannel.RtpWithTcp = false;
                        videoChannel.VideoSrcUrl = null;
                        videoChannel.RecordSecs = 0;
                        videoChannel.MethodByGetStream = MethodByGetStream.None;
                        videoChannel.MainId = sipChannel.Stream;
                        videoChannel.VideoDeviceType = VideoDeviceType.UNKNOW;
                        try
                        {
                            #region debug sql output

                            //if (Common.IsDebug)
                            //{
                                var sql = ORMHelper.Db.Insert(videoChannel).ToSql();

                                GCommon.Logger.Debug(
                                    $"[{Common.LoggerHead}]->OnCatalogReceived->执行SQL:->{sql}");
                            //}

                            #endregion
                            var ret = 0;
                            if (Common.AkStreamWebConfig.DbType == "MySql")
                            {
                                ret = ORMHelper.Db.Insert(videoChannel).ExecuteAffrows();
                            }
                            else
                            {
                                sql = sql.Replace("'f'", "0");
                                sql = sql.Replace("'t'", "1");

                                ret = ORMHelper.Db.Ado.ExecuteNonQuery(sql);
                            }
                            
                            if (ret > 0)
                            {
                                GCommon.Logger.Debug(
                                    $"[{Common.LoggerHead}]->写入一条新的设备目录到数据库，需激活后使用->{sipChannel.RemoteEndPoint.Address.MapToIPv4().ToString()}-{sipChannel.ParentId}:{sipChannel.DeviceId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            GCommon.Logger.Error($"[{Common.LoggerHead}]->数据库写入异常->{ex.Message}\r\n{ex.StackTrace}");
                        }
                    }
                }
                else
                {
                    GCommon.Logger.Debug("OnCatalogReceived: CreateOrg");
                    CreateOrg(sipChannel);
                    GCommon.Logger.Debug("OnCatalogReceived: CreateOrg1");
                }

                int platType = 0;
                int manufacturer = 99; //其他
                var deviceDB = ORMHelper.Db.Select<Device281Plat>().Where(a => a.platid == sipChannel.ParentId).First();
                if (deviceDB != null)
                {
                    platType = deviceDB.plat_type;
                    manufacturer = deviceDB.manufacturer;
                }
                int currentGetNumber = SsyncState.Orgs.Count + SsyncState.Devices.Count;
                GCommon.Logger.Debug("OnCatalogReceived: currentGetNumber: " + currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);

                if (platType == 0 && manufacturer == 2) //海康
                {
                    currentGetNumber++;
                    GCommon.Logger.Debug("OnCatalogReceived2: currentGetNumber: " + currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);

                }
                GCommon.Logger.Debug("OnCatalogReceived3: currentGetNumber: " + currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);

                if ( currentGetNumber >= sipChannel.TotalNumber)
                {
                    GCommon.Logger.Debug("OnCatalogReceived4: currentGetNumber: " + currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);
                    try
                    {
                        UpdateCatelogToDB();
                    }
                    catch (Exception ex)
                    {
                        GCommon.Logger.Debug("OnCatalogReceived fail: " + ex.Message);
                    }
                    GCommon.Logger.Debug("OnCatalogReceived5: currentGetNumber: " + currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);

                }
                //else if (DateTime.Now.Subtract(SsyncState.StartTime).Minutes >= 1)
                //{
                //    GCommon.Logger.Debug("OnCatalogReceived6: currentGetNumber: " + currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);
                //    try
                //    {
                //        UpdateCatelogToDB();
                //    }
                //    catch (Exception ex)
                //    {
                //        GCommon.Logger.Debug("OnCatalogReceived fail7: " + ex.Message);
                //    }
                //    GCommon.Logger.Debug("OnCatalogReceived8: currentGetNumber: " + currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);
                //}
                //GCommon.Logger.Debug("OnCatalogReceived9: DateTime.Now.Subtract(SsyncState.StartTime).Minutes : " + DateTime.Now.Subtract(SsyncState.StartTime).Minutes + ", " + DateTime.Now.Subtract(SsyncState.StartTime).Seconds+ currentGetNumber + ", sipChannel.TotalNumber: " + sipChannel.TotalNumber + ",SsyncState.Orgs.Count:  " + SsyncState.Orgs.Count + ", SsyncState.Devices.Count: " + SsyncState.Devices.Count);


            }
        }

        private static void CreateOrg(SipChannel sipChannel)
        {
            try
            {

           
            if (sipChannel.ParentId != sipChannel.DeviceId)
            {
                var org = new organization();
                org.id = sipChannel.DeviceId;
                //if (isPlat)
                //{
                //    org.super_id = sipChannel.SipChannelDesc.CivilCode;
                //}
                //else
                //{
                //    org.super_id = sipChannel.ParentId;
                //}
                if (string.IsNullOrEmpty(sipChannel.SipChannelDesc.CivilCode))
                {
                    if (!string.IsNullOrEmpty(sipChannel.SipChannelDesc.ParentID))
                    {
                        org.super_id = sipChannel.SipChannelDesc.ParentID;
                    }
                    else
                    {
                        org.super_id = sipChannel.ParentId;
                    }
                }
                else
                {
                    org.super_id = sipChannel.SipChannelDesc.CivilCode;
                }

                if (!string.IsNullOrEmpty(sipChannel.SipChannelDesc.ParentID) && sipChannel.SipChannelDesc.ParentID.Contains("/"))
                {
                    var ids = sipChannel.SipChannelDesc.ParentID.Split("/");
                    if (ids != null && ids.Length > 0)
                    {
                        org.super_id = ids[ids.Length - 1];
                    }
                }

                org.name = sipChannel.SipChannelDesc.Name;
                org.domain = sipChannel.SipChannelDesc.IPAddress;
                //deviceNumber.status = sipChannel.SipChannelStatus;
                SsyncState.Orgs.Add(org);

            }
            }
            catch (Exception ex)
            {
                GCommon.Logger.Warn("createorgexp " + ex.Message);
                throw;
            }
        }

        private static void CreateDevice(SipChannel sipChannel)
        {
            var deviceNumber = new DeviceNumber();

            bool isPlat = true;
            if (!sipChannel.SipChannelDesc.ParentID.Contains(sipChannel.ParentId))
            {
                isPlat = false;
            }
            if (!isPlat || string.IsNullOrEmpty(sipChannel.SipChannelDesc.CivilCode))
            {
                deviceNumber.fatherid = sipChannel.ParentId;
            }
            else
            {
                deviceNumber.fatherid = sipChannel.SipChannelDesc.CivilCode;
            }

            //dahua 
            if (sipChannel.SipChannelDesc.ParentID.Contains("/"))
            {
                var ids = sipChannel.SipChannelDesc.ParentID.Split("/");
                if (ids != null && ids.Length > 0)
                {
                    deviceNumber.fatherid = ids[ids.Length - 1];
                }
            }


            //deviceNumber.dev = sipChannel.DeviceId;
            deviceNumber.dev = sipChannel.SipChannelDesc.DeviceID;
            //deviceNumber.num = sipChannel.SipChannelDesc.DeviceID;
            deviceNumber.name = sipChannel.SipChannelDesc.Name;
            deviceNumber.longitude = sipChannel.SipChannelDesc.LongitudeValue;
            deviceNumber.latitude = sipChannel.SipChannelDesc.LatitudeValue;
            deviceNumber.domain = sipChannel.SipChannelDesc.IPAddress;
            deviceNumber.modify_time = DateTime.Now;

            int status = 0;
            //deviceNumber.status = sipChannel.SipChannelStatus;
            switch (sipChannel.SipChannelStatus)
            {
                case DevStatus.ON:
                    status = 1;
                    break;
                case DevStatus.OFF:
                    status = 0;
                    break;
                case DevStatus.OK:
                    status = 1;
                    break;
                default:
                    break;
            }
            deviceNumber.status = status;
            deviceNumber.plat_id = SsyncState.PlatId;
            SsyncState.Devices.Add(deviceNumber);
        }

        private static void UpdateCatelogToDB()
        {
            GCommon.Logger.Debug("UpdateCatelogToDB");
            ORMHelper.Db.Delete<organization>().Where(x =>
x.plat_id.Equals(SsyncState.PlatId)).ExecuteAffrows();
            GCommon.Logger.Debug("UpdateCatelogToDB1");

            foreach (var org in SsyncState.Orgs)
            {
                GCommon.Logger.Debug("UpdateCatelogToDB2");

                org.plat_id = SsyncState.PlatId;
                try
                {
                    ORMHelper.Db.Insert(org).ExecuteAffrows();
                }
                catch (Exception ex)
                {
                    GCommon.Logger.Warn("UpdateCatelogToDB org insertdb fail: " + ex.Message);
                    continue;
                }
            }

            //foreach (var org in SsyncState.Orgs)
            //            {
            //    var obj1 = ORMHelper.Db.Select<organization>().Where(x =>
            //    x.id.Equals(org.id)).First();
            //                    if (obj1 != null)
            //                        {
            //        ORMHelper.Db.Delete<organization>().Where(x =>
            //        x.id.Equals(org.id)).ExecuteAffrows();
            //        obj1 = null;
            //                        }
            //    ORMHelper.Db.Insert(org).ExecuteAffrows();
            //                }
            var modifyTime = DateTime.Now;
            foreach (var device in SsyncState.Devices)
            {
                GCommon.Logger.Debug("UpdateCatelogToDB3");

                long count = ORMHelper.Db.Select<DeviceNumber>().Count();
                if (count >= Common.License.MaxDeviceCount) 
                {
                    GCommon.Logger.Warn("license fail too many device");
                    SsyncState.State.LastResult = false;
                    break;
                }
                device.modify_time = modifyTime;
                var obj1 = ORMHelper.Db.Select<DeviceNumber>().Where(x =>
x.dev.Equals(device.dev)).First();
                if (obj1 != null)
                {
                    if (SsyncState.Method == SyncMethod.KeepOrg)
                    {
                        device.num = obj1.num;
                    }
                    ORMHelper.Db.Delete<DeviceNumber>().Where(x =>
    x.dev.Equals(device.dev)).ExecuteAffrows();
                    obj1 = null;
                }

                switch (SsyncState.Method)
                {
                    case SyncMethod.SameAsId:
                        device.num = device.dev;
                        break;
                    case SyncMethod.KeepOrg:
                        break;
                    case SyncMethod.StartFromIndex:
                        device.num = SsyncState.SyncStartIndex++.ToString();
                        break;
                    default:
                        break;
                }
                GCommon.Logger.Debug("UpdateCatelogToDB4");

                try
                {
                    ORMHelper.Db.Insert(device).ExecuteAffrows();
                }
                catch (Exception ex)
                {
                    GCommon.Logger.Warn("UpdateCatelogToDB device insertdb fail: " + ex.Message);
                    continue;
                }

                GCommon.Logger.Debug("UpdateCatelogToDB5");

                SsyncState.State.LastResult = true;
            }
            GCommon.Logger.Debug("UpdateCatelogToDB6");

            //var result = ORMHelper.Db.Delete<DeviceNumber>().Where(a =>a.plat_id == SsyncState.PlatId).Where(a =>a.modify_time!=modifyTime).ExecuteAffrows();
            var result = ORMHelper.Db.Select<DeviceNumber>().Where(a => a.plat_id == SsyncState.PlatId).Where(a => modifyTime.Subtract(a.modify_time).Seconds > 1).First();
            GCommon.Logger.Debug("UpdateCatelogToDB7");

            SsyncState.State.IsProcessing = false;
            //SsyncState.State.orgCountBefore = 0;
            //SsyncState.State.DeviceCountBefore = 0;
            //SsyncState.State.DeviceCount = 0;
            //SsyncState.State.orgCount = 0;

            //SsyncState.Devices.Clear();
            //SsyncState.Orgs.Clear();
            //SsyncState.PlatId = "";
        }
    }

    public class SyncStateFull
    {
        public Controllers.NetManagerController.SyncState State { get; set; }
        public string PlatId { get; set; }
        public int Count { get; set; }

        public List<organization> Orgs { get; set; }
        public List<DeviceNumber> Devices { get; set; }

        public SyncMethod Method{ get; set; }
        public BigInteger SyncStartIndex { get; set; }

        public DateTime StartTime { get; set; }

        public SyncStateFull()
        {
            State = new Controllers.NetManagerController.SyncState();
            State.IsProcessing = false;
            Method = SyncMethod.SameAsId;
            Devices = new List<DeviceNumber> { };
            Orgs = new List<organization> { };
        }
    }

    public enum SyncMethod
    {
        SameAsId,
        KeepOrg,
        StartFromIndex
    }
}