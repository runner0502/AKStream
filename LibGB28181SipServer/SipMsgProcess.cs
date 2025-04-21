using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibCommon;
using LibCommon.Enums;
using LibCommon.Structs;
using LibCommon.Structs.DBModels;
using LibCommon.Structs.GB28181;
using LibCommon.Structs.GB28181.Net.SDP;
using LibCommon.Structs.GB28181.Net.SIP;
using LibCommon.Structs.GB28181.XML;
using LinCms.Core.Entities;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using Ubiety.Dns.Core.Records;

namespace LibGB28181SipServer
{
    /// <summary>
    /// gb28181-2016
    /// </summary>
    public static class SipMsgProcess
    {
        /// <summary>
        /// 普通消息回复状态OK
        /// </summary>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        private static async Task SendOkMessage(SIPRequest sipRequest)
        {
            SIPResponseStatusCodesEnum messaageResponse = SIPResponseStatusCodesEnum.Ok;
            SIPResponse okResponse = SIPResponse.GetResponse(sipRequest, messaageResponse, null);
            await Common.SipServer.SipTransport.SendResponseAsync(okResponse);
        }


        /// <summary>
        /// 插入历史记录到数据库
        /// </summary>
        /// <param name="tmpRecItem"></param>
        private static void InsertRecordItems(RecordInfoEx tmpRecItem)
        {
            var obj = GCommon.VideoChannelRecordInfo.FindLast(x => x.TaskId.Equals(tmpRecItem.Sn));
            if (obj != null)
            {
                if (obj.RecItems == null)
                {
                    obj.RecItems = new List<RecordInfo.RecItem>();
                }

                //已经存在
                foreach (var item in tmpRecItem.RecordInfo.RecordItems.Items)
                {
                    var tag = item.Address + item.Name + item.Secrecy + item.Type +
                              item.EndTime +
                              item.FilePath + item.StartTime + item.DeviceID + item.RecorderID +
                              tmpRecItem.DeviceId + tmpRecItem.Sn;
                    var crc32 = CRC32Helper.GetCRC32(tag);
                    var crc32Str = crc32.ToString().PadLeft(10, '0');
                    char[] tmpChars = crc32Str.ToCharArray();
                    tmpChars[0] = '1'; //回放流的ssrc第一位是1
                    string itemId = new string(tmpChars);
                    item.SsrcId = itemId; //ssrc的值
                    item.Stream = string.Format("{0:X8}", uint.Parse(itemId)); //ssrc的16进制表示
                    item.App = "rtp";
                    item.Vhost = "__defaultVhost__";
                    item.SipDevice = Common.SipDevices.FindLast(x => x.DeviceId.Equals(tmpRecItem.DeviceId));
                    item.SipChannel =
                        item.SipDevice.SipChannels.FindLast(x => x.DeviceId.Equals(tmpRecItem.ChannelId));
                    item.PushStatus = PushStatus.IDLE;
                    item.MediaServerStreamInfo = new MediaServerStreamInfo();
                    obj.RecItems.Add(item);
                }
            }
            else
            {
                //第一次
                var record = new VideoChannelRecordInfo();
                record.TatolCount = tmpRecItem.TatolNum;
                record.Expires = DateTime.Now.AddHours(24);
                record.TaskId = tmpRecItem.Sn;
                if (record.TatolCount <= 0)
                {
                    return;
                }

                if (record.RecItems == null)
                {
                    record.RecItems = new List<RecordInfo.RecItem>();
                }

                if (tmpRecItem.RecordInfo.RecordItems.Items != null &&
                    tmpRecItem.RecordInfo.RecordItems.Items.Count > 0)
                {
                    foreach (var item in tmpRecItem.RecordInfo.RecordItems.Items)
                    {
                        var tag = item.Address + item.Name + item.Secrecy + item.Type +
                                  item.EndTime +
                                  item.FilePath + item.StartTime + item.DeviceID + item.RecorderID +
                                  tmpRecItem.DeviceId + tmpRecItem.Sn;
                        var crc32 = CRC32Helper.GetCRC32(tag);
                        var crc32Str = crc32.ToString().PadLeft(10, '0');
                        char[] tmpChars = crc32Str.ToCharArray();
                        tmpChars[0] = '1'; //回放流的ssrc第一位是1
                        string itemId = new string(tmpChars);
                        item.SsrcId = itemId; //ssrc的值
                        item.Stream = string.Format("{0:X8}", uint.Parse(itemId)); //ssrc的16进制表示
                        item.App = "rtp";
                        item.Vhost = "__defaultVhost__";
                        item.SipDevice = Common.SipDevices.FindLast(x => x.DeviceId.Equals(tmpRecItem.DeviceId));
                        item.SipChannel =
                            item.SipDevice.SipChannels.FindLast(x => x.DeviceId.Equals(tmpRecItem.ChannelId));
                        item.PushStatus = PushStatus.IDLE;
                        item.MediaServerStreamInfo = new MediaServerStreamInfo();
                        record.RecItems.Add(item);
                    }
                }

                GCommon.VideoChannelRecordInfo.Add(record);
            }
        }

        /// <summary>
        /// 在线程中处理历史回放文件列表
        /// </summary>
        public static void ProcessRecordInfoThread()
        {
            while (true)
            {
                while (!Common.TmpRecItems.IsEmpty)
                {
                    var ret = Common.TmpRecItems.TryDequeue(out RecordInfoEx recordInfo);
                    if (ret && recordInfo != null)
                    {
                        try
                        {
                            InsertRecordItems(recordInfo);
                        }
                        catch (Exception ex)
                        {
                            GCommon.Logger.Error(
                                $"[{Common.LoggerHead}]->插入历史回放文件信息时发生异常->{ex.Message}\r\n{ex.StackTrace}");
                        }
                    }

                    Thread.Sleep(10);
                }

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 线程处理队列中的设备目录
        /// </summary>
        public static void ProcessCatalogThread()
        {
            while (true)
            {
                while (!Common.TmpCatalogs.IsEmpty)
                {
                    var ret = Common.TmpCatalogs.TryDequeue(out Catalog tmpCatalog);
                    if (ret && tmpCatalog != null)
                    {
                        try
                        {
                            InsertDeviceItems(tmpCatalog);
                        }
                        catch (Exception ex)
                        {
                            GCommon.Logger.Error(
                                $"[{Common.LoggerHead}]->插入设备目录时发生异常->{ex.Message}\r\n{ex.StackTrace}");
                        }
                    }

                    Thread.Sleep(10);
                }

                Thread.Sleep(1000);
            }
        }


        /// <summary>
        /// 处理设备目录添加
        /// </summary>
        /// <param name="catalog"></param>
        /// <returns></returns>
        public static void InsertDeviceItems(Catalog catalog)
        {
            if (catalog != null)
            {
                var tatolNum = catalog.SumNum;
                var tmpSipDeviceList = Common.SipDevices.FindAll(x => x.DeviceId.Equals(catalog.DeviceID));

                if (tmpSipDeviceList.Count > 0)
                {
                    foreach (var tmpSipDevice in tmpSipDeviceList)
                    {
                        foreach (var tmpChannelDev in catalog.DeviceList.Items)
                        {
                            lock (tmpSipDevice.SipChannelOptLock) //锁粒度在SipDevice中，不影响其他线程的效率
                            {
                                SipChannel sipChannelInList = tmpSipDevice.SipChannels.FindLast(x =>
                                    x.SipChannelDesc.DeviceID.Equals(tmpChannelDev.DeviceID));
                                if (sipChannelInList == null)
                                {
                                    var newSipChannel = new SipChannel()
                                    {
                                        LastUpdateTime = DateTime.Now,
                                        LocalSipEndPoint = tmpSipDevice.LocalSipEndPoint!,
                                        PushStatus = PushStatus.IDLE,
                                        RemoteEndPoint = tmpSipDevice.RemoteEndPoint!,
                                        SipChannelDesc = tmpChannelDev,
                                        ParentId = tmpSipDevice.DeviceId,
                                        DeviceId = tmpChannelDev.DeviceID,
                                        TotalNumber = tatolNum,
                                    };
                                    if (tmpChannelDev.InfList != null)
                                    {
                                        newSipChannel.SipChannelDesc.InfList = tmpChannelDev.InfList;
                                    }

                                    newSipChannel.SipChannelStatus = tmpChannelDev.Status;
                                    newSipChannel.SipChannelType = Common.GetSipChannelType(tmpChannelDev.DeviceID);
                                    if (newSipChannel.SipChannelType == SipChannelType.AudioChannel ||
                                        newSipChannel.SipChannelType == SipChannelType.VideoChannel)
                                    {
                                        var ret = UtilsHelper.GetSSRCInfo(tmpSipDevice.DeviceId,
                                            newSipChannel.DeviceId);
                                        newSipChannel.SsrcId = ret.Key;
                                        newSipChannel.Stream = ret.Value;
                                    }
                                    tmpSipDevice.SipChannels.Add(newSipChannel);


                                    Task.Run(() => { OnCatalogReceived?.Invoke(newSipChannel); }); //抛线程出去处理
                                    GCommon.Logger.Info(
                                        $"[{Common.LoggerHead}]->Sip设备通道信息->{tmpSipDevice.DeviceId}->增加Sip通道成功->({newSipChannel.SipChannelType.ToString()})->{newSipChannel.SipChannelDesc.DeviceID}->此设备当前通道数量:{tmpSipDevice.SipChannels.Count}条");
                                }
                                else
                                {
                                    GCommon.Logger.Info("receive catelog update : " + tmpChannelDev.ToJson());
                                    sipChannelInList.LastUpdateTime = DateTime.Now; //如果sip通道已经存在，则更新相关字段
                                    sipChannelInList.SipChannelStatus = tmpChannelDev.Status;
                                    sipChannelInList.SipChannelDesc = tmpChannelDev;
                                    sipChannelInList.TotalNumber = tatolNum;
                                    if (tmpChannelDev.InfList != null)
                                    {
                                        sipChannelInList.SipChannelDesc.InfList = tmpChannelDev.InfList;
                                    }

                                    Task.Run(() => { OnCatalogReceived?.Invoke(sipChannelInList); }); //抛线程出去处理
                                    GCommon.Logger.Info(
                                        $"[{Common.LoggerHead}]->Sip设备通道信息->{tmpSipDevice.DeviceId}->增加Sip通道成功->({sipChannelInList.SipChannelType.ToString()})->{sipChannelInList.SipChannelDesc.DeviceID}->此设备当前通道数量:{tmpSipDevice.SipChannels.Count}条");
                                }
                            }

                            if (tmpSipDevice.SipChannels.Count > 0
                               ) //当正确收到过一次以后就返回成功
                            {
                                var _taskTag = $"CATALOG:{tmpSipDevice.DeviceId}";
                                var ret = Common.NeedResponseRequests.TryRemove(_taskTag,
                                    out NeedReturnTask _task);
                                if (ret && _task != null && _task.AutoResetEvent2 != null)
                                {
                                    try
                                    {
                                        _task.AutoResetEvent2.Set();
                                    }
                                    catch (Exception ex)
                                    {
                                        ResponseStruct exrs = new ResponseStruct()
                                        {
                                            Code = ErrorNumber.Sys_AutoResetEventExcept,
                                            Message = ErrorMessage.ErrorDic![ErrorNumber.Sys_AutoResetEventExcept],
                                            ExceptMessage = ex.Message,
                                            ExceptStackTrace = ex.StackTrace
                                        };
                                        GCommon.Logger.Warn(
                                            $"[{Common.LoggerHead}]->AutoResetEvent.Set异常->{JsonHelper.ToJson(exrs)}");
                                    }
                                }
                            }
                        }

                        tmpSipDevice.DeviceInfo!.Channel = tmpSipDevice.SipChannels!.Count;
                    }
                }
                else
                {
                    GCommon.Logger.Warn(
                        $"[{Common.LoggerHead}]->处理添加时出现异常情况->Sip设备{catalog.DeviceID}不在系统列表中，已跳过处理");
                }
            }
        }


        /// <summary>
        /// 保持心跳时的回复
        /// </summary>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        private static async Task SendKeepAliveOk(SIPRequest sipRequest)
        {
            SIPResponseStatusCodesEnum keepAliveResponse = SIPResponseStatusCodesEnum.Ok;
            SIPResponse okResponse = SIPResponse.GetResponse(sipRequest, keepAliveResponse, null);
            await Common.SipServer.SipTransport.SendResponseAsync(okResponse);
        }

        private static async Task Send100try(SIPRequest sipRequest)
        {
            SIPResponseStatusCodesEnum keepAliveResponse = SIPResponseStatusCodesEnum.Trying;
            SIPResponse okResponse = SIPResponse.GetResponse(sipRequest, keepAliveResponse, null);
            await Common.SipServer.SipTransport.SendResponseAsync(okResponse);
        }

        /// <summary>
        /// 获取流共享信息
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        private static bool GetShareInfo(SIPRequest req, out ShareInviteInfo info)
        {
            info = null;
            var sdpBody = req.Body;
            GCommon.Logger.Debug("GetShareInfo request body: " + sdpBody.ToString());
            try
            {
                string mediaip = "";
                ushort mediaport = 0;
                string ssrc = "";
                string channelid =
                    req.Header.Subject.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[
                        0];
                channelid = channelid.Substring(0, channelid.IndexOf(':'));
                bool isUdp = true;
                //Console.WriteLine(channelid);
                int index = sdpBody.IndexOf("\r\n");
                GCommon.Logger.Debug("GetShareInfo111: index: " + index);

                string[] sdpBodys = sdpBody.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                if (sdpBodys.Length <= 1)
                {
                    GCommon.Logger.Debug("GetShareInfo1: ");

                    sdpBodys = sdpBody.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                }

                if (sdpBodys.Length <=1 )
                {
                    GCommon.Logger.Debug("GetShareInfo2: ");

                    sdpBodys = sdpBody.Split("\r", StringSplitOptions.RemoveEmptyEntries);
                }
                GCommon.Logger.Debug("GetShareInfo3: " + sdpBodys.Length);
                for (global::System.Int32 i = 0; i < sdpBodys.Length; i++)
                {
                    GCommon.Logger.Debug("GetShareInfo: body,  " + i + ", " + sdpBodys[i]);

                }

                foreach (var line in sdpBodys)
                {
                    GCommon.Logger.Debug("GetShareInfo4: " + line);

                    if (line.Trim().ToLower().StartsWith("o="))
                    {
                        GCommon.Logger.Debug("GetShareInfo6: ");

                        var tmp = line.ToLower().Split("ip4", StringSplitOptions.RemoveEmptyEntries);
                        if (tmp.Length == 2)
                        {
                            GCommon.Logger.Debug("GetShareInfo7: ");

                            mediaip = tmp[1];
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (line.Trim().ToLower().StartsWith("m=audio"))
                    {
                        GCommon.Logger.Debug("GetShareInfo8: ");

                        if (line.Contains("TCP"))
                        {
                            isUdp = false;
                            mediaport = ushort.Parse(UtilsHelper.GetValue(line.ToLower(), "m\\=audio", "tcp").Trim());
                        }
                        else
                        {
                            isUdp = true;
                            mediaport = ushort.Parse(UtilsHelper.GetValue(line.ToLower(), "m\\=audio", "rtp").Trim());
                        }
                    }

                    if (line.Trim().ToLower().StartsWith("y="))
                    {
                        GCommon.Logger.Debug("GetShareInfo100: ");
                        var tmp2 = line.Split("=", StringSplitOptions.RemoveEmptyEntries);
                        if (tmp2.Length == 2)
                        {
                            ssrc = tmp2[1];
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                ResponseStruct rs;
               //var shareList = WebApiHelper.GetShareChannelList(out rs);
                //if (rs.Code.Equals(ErrorNumber.None) && shareList != null)
                {
                    //var obj = shareList.FindLast(x =>
                    //    x.ShareDeviceId.Equals(channelid));
                    //if (obj != null)
                    {
                        info = new ShareInviteInfo()
                        {
                            ChannelId = channelid.Trim(),
                            RemoteIpAddress = mediaip.Trim(),
                            RemotePort = mediaport,
                            Ssrc = ssrc.Trim(),
                            CallId = req.Header.CallId.Trim(),
                            Cseq = req.Header.CSeq,
                            FromTag = req.Header.From.FromTag,
                            ToTag = req.Header.To.ToTag,
                            //MediaServerId = obj.MediaServerId.Trim(),
                            //Stream = obj.MainId.Trim(),
                            //App = obj.App.Trim(),
                            //Vhost = obj.Vhost.Trim(),
                            Is_Udp = isUdp,
                        };

                        GCommon.Logger.Debug(
                            $"[{Common.LoggerHead}]->获取sdp协商信息成功->{JsonHelper.ToJson(info)}");
                        return true;
                    }
                }
               
            }
            catch
            {
                return false;
            }

            return false;
        }

        static int _localPort = 7000;

        private static bool CreateSdp(SIPRequest reqold, ref ShareInviteInfo info, out string sdpout)
        {
            sdpout = "";
            var from = reqold.Header.From;
            var to = reqold.Header.To;
            string callId = reqold.Header.CallId;
            SIPRequest req = SIPRequest.GetRequest(SIPMethodsEnum.INVITE, reqold.Header.To.ToURI,
                new SIPToHeader(to.ToName, to.ToURI, to.ToTag),
                new SIPFromHeader("", from.FromURI, from.FromTag));
            req.Header.Contact = new List<SIPContactHeader>()
                { new SIPContactHeader(reqold.Header.From.FromName, reqold.Header.From.FromURI) };
            req.Header.UserAgent = ConstString.SIP_USERAGENT_STRING;
            req.Header.Allow = null;
            req.Header.Vias = reqold.Header.Vias;
            req.Header.CallId = callId;
            req.Header.CSeq = reqold.Header.CSeq;
            var sdpConn = new SDPConnectionInformation(Common.SipServerConfig.SipIpAddress);
            var sdp = new SDP()
            {
                Version = 0,
                SessionId = "0",
                Username = Common.SipServerConfig.ServerSipDeviceId,
                SessionName = "Talk",
                Connection = sdpConn,
                Timing = "0 0",
                Address = Common.SipServerConfig.SipIpAddress,
            };

            //var psFormat = new SDPMediaFormat(SDPMediaFormatsEnum.PS)
            //{
            //    IsStandardAttribute = false,
            //};
            var pcmaFormat = new SDPMediaFormat(SDPMediaFormatsEnum.PCMA)
            {
                IsStandardAttribute = false,
            };

            ResponseStruct rs;
            if ( info.LocalRtpPort > 0)
            {
                var media = new SDPMediaAnnouncement()
                {
                    Media = SDPMediaTypesEnum.audio,
                    Port = info.LocalRtpPort,
                };
                //info.LocalRtpPort = rtpPort;
                //media.MediaFormats.Add(psFormat);
                media.MediaFormats.Add(pcmaFormat);
                media.AddExtra("a=sendonly");
                if (!info.Is_Udp)
                {
                    media.Transport = "TCP/RTP/AVP";
                    media.AddExtra("a=connection:new");
                    media.AddExtra("a=setup:passive");
                }
                else
                {
                    media.Transport = "RTP/AVP";
                }
                //media.AddFormatParameterAttribute(psFormat.FormatID, psFormat.Name);
                media.AddFormatParameterAttribute(pcmaFormat.FormatID, pcmaFormat.Name);
                
                //media.AddExtra($"a=username:{Common.SipServerConfig.SipUsername}");
                //media.AddExtra($"a=password:{Common.SipServerConfig.SipPassword}");
                media.AddExtra($"y={info.Ssrc}");
                media.AddExtra("f=");
                sdp.Media.Add(media);
                sdpout = sdp.ToString();
                return true;
            }

            GCommon.Logger.Warn(
                $"[{Common.LoggerHead}]->申请rtp(发送)端口失败->");
            return false;
        }


        /// <summary>
        /// 创建invite协商结果
        /// </summary>
        /// <param name="oldreq"></param>
        /// <param name="sdp"></param>
        /// <returns></returns>
        private static SIPResponse CreateInviteResponse(SIPRequest oldreq, string sdp)
        {
            var res = SIPResponse.GetResponse(oldreq, SIPResponseStatusCodesEnum.Ok, null);
            //res.Header.UserAgent = Common.SipUserAgent;
            res.Header.ContentType = "Application/sdp";
            res.Header.CallId = oldreq.Header.CallId;
            res.Header.To.ToTag = UtilsHelper.CreateNewCSeq().ToString();
           // _catalogCallId = res.Header.CallId;
            res.Header.CSeq = oldreq.Header.CSeq;
            res.Header.CSeqMethod = SIPMethodsEnum.INVITE;
            res.Body = sdp;
            res.Header.Contact = new List<SIPContactHeader>()
            { new SIPContactHeader(oldreq.Header.To.ToName, oldreq.Header.To.ToURI) };
            return res;
        }

        private static async Task ProcessInvite(SIPRequest sipRequest)
        {
            //SIPResponseStatusCodesEnum keepAliveResponse = SIPResponseStatusCodesEnum.Ok;
            //SIPResponse okResponse = SIPResponse.GetResponse(sipRequest, keepAliveResponse, null);
            //await Common.SipServer.SipTransport.SendResponseAsync(okResponse);

            ShareInviteInfo shareinfo = null;
            var shareinfook = GetShareInfo(sipRequest, out shareinfo);
            if (OnReceiveInvite != null)
            {
                OnReceiveInvite(shareinfo, sipRequest);
            }
        }
        private static async Task ProcessBye(SIPRequest sipRequest)
        {
            //SIPResponseStatusCodesEnum keepAliveResponse = SIPResponseStatusCodesEnum.Ok;
            //SIPResponse okResponse = SIPResponse.GetResponse(sipRequest, keepAliveResponse, null);
            //await Common.SipServer.SipTransport.SendResponseAsync(okResponse);

            if (OnReceiveBye != null)
            {
                OnReceiveBye(sipRequest);
            }
        }

        public static SIPResponse SendInviteOk(SIPRequest sipRequest, ShareInviteInfo shareinfo)
        {
            //SIPResponseStatusCodesEnum keepAliveResponse = SIPResponseStatusCodesEnum.Ok;
            //SIPResponse okResponse = SIPResponse.GetResponse(sipRequest, keepAliveResponse, null);
            //await Common.SipServer.SipTransport.SendResponseAsync(okResponse);

            SIPResponse response = null;

            if (shareinfo != null)
            {
                string sdp = "";
                var sdpok = CreateSdp(sipRequest, ref shareinfo, out sdp);
                if (sdpok && !string.IsNullOrEmpty(sdp))
                {
                    response = CreateInviteResponse(sipRequest, sdp);
                    shareinfo.ToTag = response.Header.To.ToTag;
                     Common.SipServer.SipTransport.SendResponseAsync(response);
                    //retok = OnInviteChannel?.Invoke(shareinfo, out rs);
                    //if (retok == true && rs.Code.Equals(ErrorNumber.None))
                    //{
                    //    GCommon.Logger.Info(
                    //        $"[{Common.LoggerHead}]->共享推流成功->{sipRequest.RemoteSIPEndPoint}->{JsonHelper.ToJson(shareinfo)}");
                    //}
                    //else
                    //{
                    //    GCommon.Logger.Warn(
                    //        $"[{Common.LoggerHead}]->共享推流失败->{sipRequest.RemoteSIPEndPoint}->{JsonHelper.ToJson(shareinfo)}->{JsonHelper.ToJson(rs)}");
                    //}
                }
                else
                {
                    GCommon.Logger.Warn(
                        $"[{Common.LoggerHead}]->共享推流失败->{sipRequest.RemoteSIPEndPoint}->{JsonHelper.ToJson(shareinfo)}");
                }
            }
            else
            {
                GCommon.Logger.Warn(
                    $"[{Common.LoggerHead}]->共享推流失败->{sipRequest.RemoteSIPEndPoint}->{JsonHelper.ToJson(shareinfo)}");
            }
            return response;
        }

        /// <summary>
        /// 当收到心跳数据而Sip设备处于未注册状态，发送心跳异常给设备，让设备重新注册
        /// </summary>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        private static async Task SendKeepAliveExcept(SIPRequest sipRequest)
        {
            SIPResponseStatusCodesEnum keepAliveResponse = SIPResponseStatusCodesEnum.BadRequest;
            SIPResponse okResponse = SIPResponse.GetResponse(sipRequest, keepAliveResponse, null);
            await Common.SipServer.SipTransport.SendResponseAsync(okResponse);
        }


        /// <summary>
        /// 消息处理
        /// </summary>
        /// <param name="localSipChannel"></param>
        /// <param name="localSipEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        private static async Task MessageProcess(SIPChannel localSipChannel, SIPEndPoint localSipEndPoint,
            SIPEndPoint remoteEndPoint,
            SIPRequest sipRequest)
        {
            GCommon.Logger.Debug(
                            $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}MessageProcess->{sipRequest}");
            LoadOptions option = new LoadOptions();
            sipRequest.Body = sipRequest.Body.Replace("<Status/>", "<Status>OFF</Status>");
            XElement bodyXml = XElement.Parse(sipRequest.Body);
            string cmdType = bodyXml.Element("CmdType")?.Value.ToUpper()!;
            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}MessageProcess cmdType->{cmdType}");
            if (!string.IsNullOrEmpty(cmdType))
            {
                switch (cmdType)
                {
                    case "KEEPALIVE": //处理心跳
                        string sipDeviceId = bodyXml.Element("DeviceID")?.Value.ToUpper()!;
                        var tmpSipDevice =
                            Common.SipDevices.FindLast((x => x.DeviceId.Equals(sipDeviceId)));
                        if (tmpSipDevice != null)
                        {
                            var time = DateTime.Now;
                            //2022.8.17
                            //如果间隔注册时间超过最大注册时间，表示设备需要重新注册，发送BadRequest消息
                            await SendKeepAliveOk(sipRequest);
                            if (!tmpSipDevice.IsReday)
                            {
                                tmpSipDevice.IsReday = true; //设备已就绪
                                Task.Run(() => //设备就绪通知
                                {
                                    OnDeviceReadyReceived?.Invoke(tmpSipDevice);
                                });
                            }

                            Task.Run(() =>
                            {
                                OnKeepaliveReceived?.Invoke(sipDeviceId, time, tmpSipDevice.KeepAliveLostTime);
                            }); //抛线程出去处理


                            if (tmpSipDevice.KeepAliveTime != null)//获取设备的实际心跳周期
                            {
                                tmpSipDevice.KeepAliveTimeSpentMS = (time - tmpSipDevice.KeepAliveTime).TotalMilliseconds;
                            }


                            tmpSipDevice.KeepAliveTime = time;
                            if (tmpSipDevice.RemoteEndPoint != null &&
                                tmpSipDevice.RemoteEndPoint != remoteEndPoint &&
                                tmpSipDevice.RemoteEndPoint.Protocol == SIPProtocolsEnum.udp
                               ) //如果udp协议当endpoint发生变化时更新成新的
                            {
                                //udp协议下，如果发现心跳中的remoteEndPoint与注册时的remoteEndPoint不同时，将心跳的remoteEndPoint秒换老的remoteEndPoint以保证nat穿透下Sip通讯的正常使用
                                tmpSipDevice.RemoteEndPoint = remoteEndPoint;
                            }

                            GCommon.Logger.Debug(
                                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的心跳->{sipRequest}");
                        }
                        else
                        {
                            GCommon.Logger.Debug(
                                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的心跳->{sipRequest}->但是Sip设备不存在，发送BadRequest消息,使设备重新注册");
                            await SendKeepAliveExcept(sipRequest);
                        }

                        break;
                    case "CATALOG": //处理设备目录
                        GCommon.Logger.Debug(
                            $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}MessageProcess1->{sipRequest}");
                        await SendOkMessage(sipRequest);
                        try
                        {
                            Common.TmpCatalogs.Enqueue(UtilsHelper.XMLToObject<Catalog>(bodyXml));
                        }
                        catch (Exception ex)
                        {
                            NewMethod(bodyXml);
                        }

                        GCommon.Logger.Debug(
                            $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}设备目录信息->{sipRequest}");

                        break;
                    case "DEVICEINFO":
                        await SendOkMessage(sipRequest);
                        var tmpDeviceInfo = UtilsHelper.XMLToObject<DeviceInfo>(bodyXml);
                        if (tmpDeviceInfo != null)
                        {
                            var tmpSipDeviceFind =
                                Common.SipDevices.FindLast(x => x.DeviceId.Equals(tmpDeviceInfo.DeviceID));
                            if (tmpSipDeviceFind != null)
                            {
                                Task.Run(() =>
                                {
                                    OnDeviceInfoReceived?.Invoke(tmpSipDeviceFind, tmpDeviceInfo);
                                }); //抛线程出去处理
                                tmpSipDeviceFind.DeviceInfo = tmpDeviceInfo;
                                tmpSipDeviceFind.DeviceInfo.Channel = tmpSipDeviceFind.SipChannels.Count;
                                GCommon.Logger.Debug(
                                    $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}设备信息->{sipRequest}");
                            }
                        }

                        break;
                    case "DEVICESTATUS":
                        await SendOkMessage(sipRequest);
                        var tmpDeviceStatus = UtilsHelper.XMLToObject<DeviceStatus>(bodyXml);
                        if (tmpDeviceStatus != null)
                        {
                            var tmpSipDeviceFind =
                                Common.SipDevices.FindLast(x => x.DeviceId.Equals(tmpDeviceStatus.DeviceID));
                            if (tmpSipDeviceFind != null)
                            {
                                Task.Run(() =>
                                {
                                    OnDeviceStatusReceived?.Invoke(tmpSipDeviceFind, tmpDeviceStatus);
                                }); //抛线程出去处理

                                tmpSipDeviceFind.DeviceStatus = tmpDeviceStatus;
                                GCommon.Logger.Debug(
                                    $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}设备状态信息->{sipRequest}");
                            }
                        }

                        break;
                    case "RECORDINFO":
                        var recObj = new RecordInfoEx();
                        recObj.RecordInfo = UtilsHelper.XMLToObject<RecordInfo>(bodyXml);
                        if (recObj.RecordInfo != null)
                        {
                            recObj.DeviceId = sipRequest.Header.From.FromURI.User;
                            recObj.Sn = recObj.RecordInfo.SN;
                            recObj.TatolNum = recObj.RecordInfo.SumNum;
                            var tmpDev = Common.SipDevices.FindLast(x => x.DeviceId.Equals(recObj.DeviceId));
                            if (tmpDev != null && tmpDev.SipChannels != null && tmpDev.SipChannels.Count > 0)
                            {
                                var tmpChannel =
                                    tmpDev.SipChannels.FindLast(x => x.DeviceId.Equals(recObj.RecordInfo.DeviceID));
                                if (tmpChannel != null)
                                {
                                    GCommon.Logger.Debug(
                                        $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的录像查询结果->{recObj.DeviceId}->{recObj.ChannelId}->录像结果总数为:{recObj.TatolNum}->包体:{JsonHelper.ToJson(recObj.RecordInfo, Formatting.Indented)}");
                                    recObj.ChannelId = tmpChannel.DeviceId;
                                    Common.TmpRecItems.Enqueue(recObj);
                                    string _taskTag =
                                        $"RECORDINFO:{recObj.DeviceId}:{recObj.ChannelId}:{recObj.Sn}";
                                    var ret = Common.NeedResponseRequests.TryRemove(_taskTag, out NeedReturnTask _task);
                                    if (ret && _task != null && _task.AutoResetEvent2 != null)
                                    {
                                        try
                                        {
                                            _task.AutoResetEvent2.Set();
                                        }
                                        catch (Exception ex)
                                        {
                                            ResponseStruct exrs = new ResponseStruct()
                                            {
                                                Code = ErrorNumber.Sys_AutoResetEventExcept,
                                                Message = ErrorMessage.ErrorDic![ErrorNumber.Sys_AutoResetEventExcept],
                                                ExceptMessage = ex.Message,
                                                ExceptStackTrace = ex.StackTrace
                                            };
                                            GCommon.Logger.Warn(
                                                $"[{Common.LoggerHead}]->AutoResetEvent.Set异常->{JsonHelper.ToJson(exrs)}");
                                        }
                                    }
                                }
                            }
                        }

                        await SendOkMessage(sipRequest);
                        break;

                    case "MEDIASTATUS":
                        await SendOkMessage(sipRequest);
                        MediaStatus mediaStatus = UtilsHelper.XMLToObject<MediaStatus>(bodyXml);
                        if (mediaStatus != null)
                        {
                            var callId = "MEDIASTATUS" + sipRequest.Header.CallId;
                            var ret = Common.NeedResponseRequests.TryRemove(callId, out NeedReturnTask _task);
                            if (ret && _task != null)
                            {
                                ((RecordInfo.RecItem)_task.Obj).PushStatus = PushStatus.IDLE;
                                ((RecordInfo.RecItem)_task.Obj).MediaServerStreamInfo = null;
                                Task.Run(() =>
                                {
                                    OnInviteHistoryVideoFinished?.Invoke((RecordInfo.RecItem)_task.Obj);
                                }); //抛线程出去处理

                                GCommon.Logger.Debug(
                                    $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的点播结束消息->{_task.SipDevice.DeviceId}->{_task.SipChannel.DeviceId}->Stream->{((RecordInfo.RecItem)_task.Obj).SsrcId}:{((RecordInfo.RecItem)_task.Obj).Stream}");
                            }
                        }

                        break;
                    case "BROADCAST":
                        await SendOkMessage(sipRequest);
                        break;
                    case "MOBILEPOSITION":
                        var config = ORMHelper.Db.Select<SysAdvancedConfig>().First();
                        if (config != null && config.PushGisEnable == 1)
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                var formData = new MultipartFormDataContent();
                                // 添加表单数据
                                formData.Add(new StringContent(config.PushGisType), "mqType");
                                //formData.Add(new StringContent("queue.gis.third"), "topic");

                                formData.Add(new StringContent(config.PushPositionTopic), "topic");
                                string time1 = bodyXml.Element("Time")?.Value;
                                info1 info1 = new info1();
                                info1.user = bodyXml.Element("DeviceID")?.Value;
                                info1.name = "2";
                                info1.lat = bodyXml.Element("Latitude")?.Value;
                                info1.lon = bodyXml.Element("Longitude")?.Value;
                                info1.time = time1;
                                info1.type = "3";
                                info1.subtype = "213";
                                info1.status = 1;
                                var deviceinfo = ORMHelper.Db.Select<DevicePlus>().Where(a => a.id == info1.user).First();
                                if (deviceinfo != null)
                                {
                                    info1.DeviceType = deviceinfo.type;
                                    info1.DeviceInfo = deviceinfo.info;
                                }
                                var data = JsonHelper.ToJson(info1, Formatting.None, MissingMemberHandling.Error);
                                formData.Add(new StringContent(data), "body");
                 
                                //var httpRet = client.PostAsync("http://65.176.4.95:58080/api/ice/sendMsgByMQ", formData).Result.Content.ReadAsStringAsync();
                                var httpRet = client.PostAsync(config.PushGisUrl, formData).Result.Content.ReadAsStringAsync();
                                GCommon.Logger.Warn("MOBILEPOSITION send http " + info1.ToJson() + ", " + httpRet);
                            }
                        }
                        else
                        {
                            GCommon.Logger.Warn("mobileposition config is null");
                        }
                        await SendOkMessage(sipRequest);
                                                break;
                }
            }
        }

        private static void NewMethod(XElement bodyXml)
        {
            GCommon.Logger.Debug(
                                        $"[{Common.LoggerHead}]->MessageProcess2 notifycatalog->");
            var notifyCata = (UtilsHelper.XMLToObject<NotifyCatalog>(bodyXml));
            GCommon.Logger.Debug(
            $"[{Common.LoggerHead}]->MessageProcess3 notifycatalog->");

            var config1 = ORMHelper.Db.Select<SysAdvancedConfig>().First();
            if (config1 != null)
            {
                GCommon.Logger.Debug("MessageProcess4");
                if (notifyCata.DeviceList != null && notifyCata.DeviceList.Items != null)
                {
                    foreach (var item in notifyCata.DeviceList.Items)
                    {
                        GCommon.Logger.Debug("MessageProcess6");

                        using (HttpClient client = new HttpClient())
                        {
                            var formData = new MultipartFormDataContent();
                            // 添加表单数据
                            formData.Add(new StringContent(config1.PushGisType), "mqType");
                            //formData.Add(new StringContent("queue.gis.third"), "topic");

                            formData.Add(new StringContent(config1.PushRegistStateTopic), "topic");
                            //string time1 = bodyXml.Element("Time")?.Value;
                            info1 info1 = new info1();
                            info1.user = item.DeviceID;
                            info1.name = "2";
                            //info1.lat = bodyXml.Element("Latitude")?.Value;
                            //info1.lon = bodyXml.Element("Longitude")?.Value;
                            //info1.time = time1;
                            info1.type = "3";
                            info1.subtype = "213";
                            if (item.Status == LibCommon.Structs.GB28181.Sys.DevStatus.OFF)
                            {
                                info1.status = 0;
                            }
                            else
                            {
                                info1.status = 1;
                            }

                            ORMHelper.Db.Update<DeviceNumber>().Where(x =>
                            x.dev == item.DeviceID).Set(x => x.status, info1.status).ExecuteAffrowsAsync();

                           var deviceinfo = ORMHelper.Db.Select<DevicePlus>().Where(a => a.id == info1.user).First();
                            if (deviceinfo != null)
                            {
                                info1.DeviceType = deviceinfo.type;
                                info1.DeviceInfo = deviceinfo.info;
                            }
                            var data = JsonHelper.ToJson(info1, Formatting.None, MissingMemberHandling.Error);
                            formData.Add(new StringContent(data), "body");

                            //var httpRet = client.PostAsync("http://65.176.4.95:58080/api/ice/sendMsgByMQ", formData).Result.Content.ReadAsStringAsync();
                            var httpRet = client.PostAsync(config1.PushGisUrl, formData).Result.Content.ReadAsStringAsync();
                            GCommon.Logger.Warn("catalognotify send http " + info1.ToJson() + ", " + httpRet);
                        }
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

        }


    /// <summary>
    /// 处理心跳检测失败的设备，认为这类设备已经离线，需要踢除
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static void DoKickSipDevice(SipDevice sipDevice)
        {
            string tmpSipDeviceStr = JsonHelper.ToJson(sipDevice);
            try
            {
                //发一个心跳异常消息给设备，由于不确定是否可行，为防止报错，用try..catch包起来
                Task.Run(() => { SendKeepAliveExcept(sipDevice.LastSipRequest); }); //抛线程出去处理
            }
            catch
            {
            }

            Task.Run(() => { OnUnRegisterReceived?.Invoke(tmpSipDeviceStr); }); //抛线程出去处理

            lock (Common.SipDevicesLock)
            {
                Common.SipDevices.Remove(sipDevice);
                sipDevice.SipChannels = null!;
                sipDevice.Dispose();
                GCommon.Logger.Info(
                    $"[{Common.LoggerHead}]->Sip设备心跳丢失超过限制，已经注销->{tmpSipDeviceStr}");
            }

            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->当前Sip设备列表数量:->{Common.SipDevices.Count}");
        }


        /// <summary>
        /// 检查设备是否在鉴权要求以外
        /// </summary>
        /// <param name="sipDeviceId"></param>
        /// <param name="ipv4"></param>
        /// <param name="ipv6"></param>
        /// <returns></returns>
        private static bool CheckDeviceAuthenticationNeed(string sipDeviceId, string ipv4, string ipv6)
        {
            var found = Common.SipServerConfig.NoAuthenticationRequireds.FindAll(x =>
                x.DeviceId.Equals(sipDeviceId.Trim()));
            if (found != null && found.Count > 0)
            {
                foreach (var obj in found)
                {
                    if (obj != null)
                    {
                        string tmpIpv4 = obj.IpV4Address;
                        string tmpIpv6 = obj.IpV6Address;
                        if (string.IsNullOrEmpty(tmpIpv4) && string.IsNullOrEmpty(tmpIpv6))
                        {
                            return false; //如果没有指定ip,则sipdeivceid一致，不需要鉴权
                        }

                        if (!string.IsNullOrEmpty(tmpIpv4) && !string.IsNullOrEmpty(ipv4))
                        {
                            if (tmpIpv4.Trim().Equals(ipv4.Trim()))
                            {
                                return false; //ipv4一致，不需要鉴权
                            }
                        }

                        if (!string.IsNullOrEmpty(tmpIpv6) && !string.IsNullOrEmpty(ipv6))
                        {
                            if (tmpIpv6.Trim().Equals(ipv6.Trim()))
                            {
                                return false; //ipv6一致，不需要鉴权
                            }
                        }
                    }
                }
            }

            return true; //需要鉴权
        }

        /// <summary>
        /// 处理sip设备注册事件
        /// </summary>
        /// <param name="localSipChannel"></param>
        /// <param name="localSipEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        private static async Task RegisterProcess(SIPChannel localSipChannel, SIPEndPoint localSipEndPoint,
            SIPEndPoint remoteEndPoint,
            SIPRequest sipRequest)
        {
            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的Sip设备注册信息->{sipRequest}");

            string sipDeviceId = sipRequest.Header.From.FromURI.User;
            string sipDeviceIpV4Address = sipRequest.RemoteSIPEndPoint.Address.MapToIPv4().ToString();
            string sipDeviceIpV6Address = sipRequest.RemoteSIPEndPoint.Address.MapToIPv6().ToString();

            SIPResponse tryingResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Trying, null);
            await Common.SipServer.SipTransport.SendResponseAsync(tryingResponse);
            SIPResponseStatusCodesEnum registerResponse = SIPResponseStatusCodesEnum.Ok;
            if (sipRequest.Header.Contact?.Count > 0)
            {
                long expiry = sipRequest.Header.Contact[0].Expires > 0
                    ? sipRequest.Header.Contact[0].Expires
                    : sipRequest.Header.Expires;
                if (expiry <= 0)
                {
                    //注销设备
                    var tmpSipDevice = Common.SipDevices.FindLast(x => x.DeviceId.Equals(sipDeviceId));
                    if (tmpSipDevice != null)
                    {
                        try
                        {
                            Task.Run(() =>
                            {
                                OnUnRegisterReceived?.Invoke(JsonHelper.ToJson(tmpSipDevice));
                            }); //抛线程出去处理

                            GCommon.Logger.Info(
                                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的Sip设备注销请求->{tmpSipDevice.DeviceId}->已经注销，当前Sip设备数量:{Common.SipDevices.Count}个");

                            lock (Common.SipDevicesLock)
                            {
                                Common.SipDevices.Remove(tmpSipDevice);
                                tmpSipDevice.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            ResponseStruct rs = new ResponseStruct()
                            {
                                Code = ErrorNumber.Sip_Except_DisposeSipDevice,
                                Message = ErrorMessage.ErrorDic![ErrorNumber.Sip_Except_DisposeSipDevice],
                                ExceptMessage = ex.Message,
                                ExceptStackTrace = ex.StackTrace,
                            };
                            throw new AkStreamException(rs);
                        }
                    }
                }
                else
                {
                    if (Common.SipServerConfig.Authentication &&
                        CheckDeviceAuthenticationNeed(sipDeviceId, sipDeviceIpV4Address, sipDeviceIpV6Address))
                    {
                        if (sipRequest.Header.AuthenticationHeaders.Count <= 0)
                        {
                            SIPAuthenticationHeader authHeader =
                                new SIPAuthenticationHeader(SIPAuthorisationHeadersEnum.WWWAuthenticate,
                                    Common.SipServerConfig.Realm, SIPRequestAuthenticator.GetNonce());
                            var unAuthorisedHead =
                                new SIPRequestAuthenticationResult(SIPResponseStatusCodesEnum.Unauthorised, authHeader);
                            unAuthorisedHead.AuthenticationRequiredHeader.SIPDigest.Opaque = "";
                            authHeader.SIPDigest.DigestAlgorithm = DigestAlgorithmsEnum.MD5;
                            unAuthorisedHead.AuthenticationRequiredHeader.SIPDigest.Algorithhm =
                                SIPAuthorisationDigest.AUTH_ALGORITHM;

                            var unAuthorizedResponse = SIPResponse.GetResponse(sipRequest,
                                SIPResponseStatusCodesEnum.Unauthorised, null);
                            unAuthorizedResponse.Header.AuthenticationHeaders.Add(unAuthorisedHead
                                .AuthenticationRequiredHeader);

                            unAuthorizedResponse.Header.Allow = null;
                            unAuthorizedResponse.Header.Expires = 7200;
                          
                            await Common.SipServer.SipTransport.SendResponseAsync(unAuthorizedResponse);
                            return;
                        }
                        else
                        {
                            var password = OnDeviceAuthentication?.Invoke(sipDeviceId); //向外部获取鉴权密钥
                            /*GB28181Sip注册鉴权算法：
                            HA1=MD5(username:realm:passwd) //username和realm在字段“Authorization”中可以找到，passwd这个是由客户端和服务器协商得到的，一般情况下UAC端存一个UAS也知道的密码就行了
                             HA2=MD5(Method:Uri)//Method一般有INVITE, ACK, OPTIONS, BYE, CANCEL, REGISTER；Uri可以在字段“Authorization”找到
                             response = MD5(HA1:nonce:HA2)
                             */

                            string ha1 = UtilsHelper.Md5(sipRequest.Header.AuthenticationHeaders[0].SIPDigest.Username +
                                                         ":" + sipRequest.Header.AuthenticationHeaders[0].SIPDigest
                                                             .Realm +
                                                         ":" + (string.IsNullOrEmpty(password)
                                                             ? Common.SipServerConfig.SipPassword
                                                             : password));

                            string ha2 = UtilsHelper.Md5("REGISTER" + ":" +
                                                         sipRequest.Header.AuthenticationHeaders[0].SIPDigest.URI);
                            string ha3 = UtilsHelper.Md5(ha1 + ":" +
                                                         sipRequest.Header.AuthenticationHeaders[0].SIPDigest.Nonce +
                                                         ":" +
                                                         ha2);

                            if (!ha3.Equals(sipRequest.Header.AuthenticationHeaders[0].SIPDigest.Response))
                            {
                                GCommon.Logger.Debug(
                                    $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的Sip设备注册请求->鉴权失败,注册失败");
                                SIPRequest req = SIPRequest.GetRequest(SIPMethodsEnum.BYE, sipRequest.URI);
                                req.Header.CallId = sipRequest.Header.CallId;
                                req.Header.From.FromTag = sipRequest.Header.From.FromTag;
                                req.Header.To.ToTag = sipRequest.Header.To.ToTag;
                                await Common.SipServer.SipTransport.SendRequestAsync(remoteEndPoint, req);
                                return; //验证通不过就不再回复，验证通过的话，就会往下走
                            }
                        }
                    }

                    //设备注册
                    var tmpSipDevice = Common.SipDevices.FindLast(x => x.DeviceId.Equals(sipDeviceId));
                    if (tmpSipDevice == null)
                    {
                        tmpSipDevice = new SipDevice(Common.SipServerConfig);
                        tmpSipDevice.KickMe += DoKickSipDevice;
                        tmpSipDevice.Username = "";
                        tmpSipDevice.Password = "";
                        tmpSipDevice.RegisterTime = DateTime.Now;
                        tmpSipDevice.SipChannels = new List<SipChannel>();
                        tmpSipDevice.KeepAliveTime = DateTime.Now;
                        tmpSipDevice.KeepAliveLostTime = 0;
                        tmpSipDevice.DeviceInfo!.DeviceID = sipDeviceId;
                        tmpSipDevice.DeviceId = sipDeviceId;
                        tmpSipDevice.LocalSipEndPoint = localSipEndPoint;
                        tmpSipDevice.RemoteEndPoint = remoteEndPoint;
                        tmpSipDevice.SipChannelLayout = localSipChannel;
                        tmpSipDevice.IpAddress = remoteEndPoint.Address;
                        tmpSipDevice.Port = remoteEndPoint.Port;
                        tmpSipDevice.ContactUri = sipRequest.Header.Contact[0].ContactURI;
                        tmpSipDevice.LastSipRequest = sipRequest;
                        try
                        {
                            lock (Common.SipDevicesLock)
                            {
                                if (Common.SipDevices.Count(x => x.DeviceId.Equals(sipDeviceId)) <=
                                    0) //保证不存在
                                {
                                    Common.SipDevices.Add(tmpSipDevice);
                                    Task.Run(() =>
                                    {
                                        OnRegisterReceived?.Invoke(JsonHelper.ToJson(tmpSipDevice));
                                    }); //抛线程出去处理

                                    GCommon.Logger.Info(
                                        $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的Sip设备注册请求->{tmpSipDevice.DeviceId}->注册完成，当前Sip设备数量:{Common.SipDevices.Count}个");
                                }
                                else
                                {
                                    GCommon.Logger.Debug(
                                        $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的Sip设备注册请求->{tmpSipDevice.DeviceId}->注册请求重复已经忽略，当前Sip设备数量:{Common.SipDevices.Count}个");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ResponseStruct rs = new ResponseStruct()
                            {
                                Code = ErrorNumber.Sip_Except_RegisterSipDevice,
                                Message = ErrorMessage.ErrorDic![ErrorNumber.Sip_Except_RegisterSipDevice],
                                ExceptMessage = ex.Message,
                                ExceptStackTrace = ex.StackTrace,
                            };
                            throw new AkStreamException(rs);
                        }
                    }
                    else
                    {
                        if ((DateTime.Now - tmpSipDevice.RegisterTime).TotalSeconds >
                            Common.SIP_REGISTER_MIN_INTERVAL_SEC)
                        {
                            tmpSipDevice.RegisterTime = DateTime.Now;

                            Task.Run(() => { OnRegisterReceived?.Invoke(JsonHelper.ToJson(tmpSipDevice)); }); //抛线程出去处理


                            GCommon.Logger.Info(
                                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的Sip设备注册请求->{tmpSipDevice.DeviceId}->已经更新注册时间，当前Sip设备数量:{Common.SipDevices.Count}个");
                        }
                        else
                        {
                            GCommon.Logger.Debug(
                                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的Sip设备异常注册请求->已忽略，当前Sip设备数量:{Common.SipDevices.Count}个");
                        }
                    }
                }
            }
            else
            {
                registerResponse = SIPResponseStatusCodesEnum.BadRequest;
            }

            SIPNonInviteTransaction registerTransaction =
                new SIPNonInviteTransaction(Common.SipServer.SipTransport, sipRequest, null);
            SIPResponse retResponse = SIPResponse.GetResponse(sipRequest, registerResponse, null);
            /*增加tplink 摄像头支持*/
            retResponse.Header.Contact = sipRequest.Header.Contact;
            retResponse.Header.Expires = sipRequest.Header.Expires;
            retResponse.Header.SetDateHeader();
            /*增加tplink 摄像头支持*/
            retResponse.Header.Date = DateTime.Now.ToString("yyyy-MM-dd’T’HH: mm:ss.SSS"); //增加与服务器授时
            registerTransaction.SendResponse(retResponse);
        }


        /// <summary>
        /// SipRequest数据处理
        /// </summary>
        /// <param name="localSipChannel"></param>
        /// <param name="localSipEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        public static async Task SipTransportRequestReceived(SIPChannel localSipChannel, SIPEndPoint localSipEndPoint,
            SIPEndPoint remoteEndPoint,
            SIPRequest sipRequest)
        {

            GCommon.Logger.Debug(
                               $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}->{sipRequest}->  SipTransportRequestReceived");
            switch (sipRequest.Method)
            {
                case SIPMethodsEnum.REGISTER: //处理注册
                    await RegisterProcess(localSipChannel, localSipEndPoint, remoteEndPoint, sipRequest);
                    break;
                case SIPMethodsEnum.MESSAGE: //心跳、目录查询、设备信息、设备状态等消息的内容处理
                    await MessageProcess(localSipChannel, localSipEndPoint, remoteEndPoint, sipRequest);
                    break;
                case SIPMethodsEnum.INVITE:
                    await Send100try(sipRequest);
                    await ProcessInvite(sipRequest);
                    break;
                case SIPMethodsEnum.BYE:
                    await SendOkMessage(sipRequest);
                    await ProcessBye(sipRequest);
                    break;
                case SIPMethodsEnum.NOTIFY: //心跳、目录查询、设备信息、设备状态等消息的内容处理
                    await MessageProcess(localSipChannel, localSipEndPoint, remoteEndPoint, sipRequest);
                     break;
            }
        }


        /// <summary>
        /// 结束invite的回复
        /// </summary>
        /// <param name="sipResponse"></param>
        /// <param name="sipChannel"></param>
        /// <returns></returns>
        private static async Task InviteEnd(SIPResponse sipResponse, RecordInfo.RecItem record)
        {
            var from = sipResponse.Header.From;
            var to = sipResponse.Header.To;
            string callId = sipResponse.Header.CallId;

            SIPRequest req = SIPRequest.GetRequest(SIPMethodsEnum.BYE, sipResponse.Header.To.ToURI,
                new SIPToHeader(to.ToName, to.ToURI, to.ToTag),
                new SIPFromHeader("", from.FromURI, from.FromTag));
            req.Header.Contact = new List<SIPContactHeader>()
                { new SIPContactHeader(sipResponse.Header.From.FromName, sipResponse.Header.From.FromURI) };
            req.Header.UserAgent = ConstString.SIP_USERAGENT_STRING;
            req.Header.Allow = null;
            req.Header.Vias = sipResponse.Header.Vias;
            req.Header.CallId = callId;
            req.Header.CSeq = sipResponse.Header.CSeq;

            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->回复终止实时流请求状态Bye{sipResponse.RemoteSIPEndPoint}->{req}");
            await Common.SipServer.SipTransport.SendRequestAsync(sipResponse.RemoteSIPEndPoint, req);
        }


        /// <summary>
        /// 结束invite的回复
        /// </summary>
        /// <param name="sipResponse"></param>
        /// <param name="sipChannel"></param>
        /// <returns></returns>
        private static async Task InviteEnd(SIPResponse sipResponse, SipChannel sipChannel)
        {
            var from = sipResponse.Header.From;
            var to = sipResponse.Header.To;
            string callId = sipResponse.Header.CallId;

            SIPRequest req = SIPRequest.GetRequest(SIPMethodsEnum.BYE, sipResponse.Header.To.ToURI,
                new SIPToHeader(to.ToName, to.ToURI, to.ToTag),
                new SIPFromHeader("", from.FromURI, from.FromTag));
            req.Header.Contact = new List<SIPContactHeader>()
                { new SIPContactHeader(sipResponse.Header.From.FromName, sipResponse.Header.From.FromURI) };
            req.Header.UserAgent = ConstString.SIP_USERAGENT_STRING;
            req.Header.Allow = null;
            req.Header.Vias = sipResponse.Header.Vias;
            req.Header.CallId = callId;
            req.Header.CSeq = sipResponse.Header.CSeq;
            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->回复终止实时流请求状态Bye{sipResponse.RemoteSIPEndPoint}->{req}");
            await Common.SipServer.SipTransport.SendRequestAsync(sipResponse.RemoteSIPEndPoint, req);
        }

        /// <summary>
        /// 回复invite状态
        /// </summary>
        /// <param name="sipResponse"></param>
        /// <param name="sipChannel"></param>
        /// <returns></returns>
        private static async Task InviteOk(SIPResponse sipResponse, SipChannel sipChannel)
        {
            var from = sipResponse.Header.From;
            var to = sipResponse.Header.To;
            string callId = sipResponse.Header.CallId;
            sipChannel.InviteSipResponse = sipResponse;

            SIPRequest req = SIPRequest.GetRequest(SIPMethodsEnum.ACK, sipResponse.Header.To.ToURI,
                new SIPToHeader(to.ToName, to.ToURI, to.ToTag),
                new SIPFromHeader(null, from.FromURI, from.FromTag));
            req.Header.Contact = new List<SIPContactHeader>()
                { new SIPContactHeader(sipResponse.Header.From.FromName, sipResponse.Header.From.FromURI) };
            req.Header.UserAgent = ConstString.SIP_USERAGENT_STRING;
            req.Header.Allow = null;
            req.Header.Vias = sipResponse.Header.Vias;
            req.Header.CallId = callId;
            req.Header.CSeq = sipResponse.Header.CSeq;
            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->回复实时流请求状态ACK{sipResponse.RemoteSIPEndPoint}->{req}");
            await Common.SipServer.SipTransport.SendRequestAsync(sipResponse.RemoteSIPEndPoint, req);
        }

        /// <summary>
        /// 回复回放流invite状态
        /// </summary>
        /// <param name="sipResponse"></param>
        /// <param name="sipChannel"></param>
        /// <returns></returns>
        private static async Task InviteOk(SIPResponse sipResponse, RecordInfo.RecItem record)
        {
            var from = sipResponse.Header.From;
            var to = sipResponse.Header.To;
            string callId = sipResponse.Header.CallId;
            record.InviteSipResponse = sipResponse;
            foreach (var obj in GCommon.VideoChannelRecordInfo)
            {
                if (obj != null && obj.RecItems != null && obj.RecItems.Count > 0)
                {
                    var o = obj.RecItems.FindLast(x =>
                        x.Stream.Trim().ToLower().Equals(record.Stream.Trim().ToLower()));
                    if (o != null)
                    {
                        o.InviteSipResponse = record.InviteSipResponse;
                        o.CSeq = sipResponse.Header.CSeq;
                        o.ToTag = sipResponse.Header.To.ToTag;
                        o.CallId = record.CallId;
                        o.FromTag = record.FromTag;
                        break;
                    }
                }
            }


            SIPRequest req = SIPRequest.GetRequest(SIPMethodsEnum.ACK, sipResponse.Header.To.ToURI,
                new SIPToHeader(to.ToName, to.ToURI, to.ToTag),
                new SIPFromHeader("", from.FromURI, from.FromTag));
            req.Header.Contact = new List<SIPContactHeader>()
                { new SIPContactHeader(sipResponse.Header.From.FromName, sipResponse.Header.From.FromURI) };
            req.Header.UserAgent = ConstString.SIP_USERAGENT_STRING;
            req.Header.Allow = null;
            req.Header.Vias = sipResponse.Header.Vias;
            req.Header.CallId = callId;
            req.Header.CSeq = sipResponse.Header.CSeq;
            GCommon.Logger.Debug(
                $"[{Common.LoggerHead}]->回复回放流请求状态ACK{sipResponse.RemoteSIPEndPoint}->{req}");
            await Common.SipServer.SipTransport.SendRequestAsync(sipResponse.RemoteSIPEndPoint, req);
        }

        /// <summary>
        /// 检测sdp是通道时实流还是通道回放流
        /// 
        /// </summary>
        /// <param name="sdp"></param>
        /// <returns>true通道实时流，false通道回放流</returns>
        private static bool CheckIsChannleInviteBySdpString(string sdp)
        {
            string[] sdpTmp = sdp.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (sdpTmp.Length <= 0)
            {
                sdpTmp = sdp.Split("\r", StringSplitOptions.RemoveEmptyEntries);
            }

            if (sdpTmp.Length <= 0)
            {
                sdpTmp = sdp.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            }

            if (sdpTmp.Length > 0)
            {
                foreach (var sdpparm in sdpTmp)
                {
                    if (!string.IsNullOrEmpty(sdpparm) && sdpparm.Trim().ToUpper().StartsWith("Y="))
                    {
                        var s = sdpparm.Trim().ToUpper().Replace("Y=", "");
                        if (!string.IsNullOrEmpty(s))
                        {
                            if (s.StartsWith("1"))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// sip Response 处理
        /// </summary>
        /// <param name="localSipChannel"></param>
        /// <param name="localSipEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="sipResponse"></param>
        /// <returns></returns>
        public static async Task SipTransportResponseReceived(SIPChannel localSipChannel, SIPEndPoint localSipEndPoint,
            SIPEndPoint remoteEndPoint,
            SIPResponse sipResponse)
        {
            GCommon.Logger.Debug($"收到的回复信息:\r\n远端端点：{remoteEndPoint.ToString()}\r\n内容：{sipResponse}");
            var status = sipResponse.Status;
            SIPMethodsEnum method;
            bool ret;
            NeedReturnTask _task;
            switch (status)
            {
                case SIPResponseStatusCodesEnum.Ok:
                    method = sipResponse.Header.CSeqMethod;
                    ret = Common.NeedResponseRequests.TryRemove(sipResponse.Header.CallId,
                        out _task);
                    if (ret && _task != null)
                    {
                        switch (method)
                        {
                            case SIPMethodsEnum.INVITE: //请求实时流成功回复
                                bool isChannleInvite = CheckIsChannleInviteBySdpString(sipResponse.Body);

                                if (isChannleInvite)
                                {
                                    await InviteOk(sipResponse, _task.SipChannel);
                                }
                                else
                                {
                                    var record = (RecordInfo.RecItem)_task.Obj;

                                    await InviteOk(sipResponse, record);
                                    if (_task.TimeoutCheckTimer != null && _task.TimeoutCheckTimer.Enabled == true)
                                    {
                                        _task.TimeoutCheckTimer.Enabled = false; //不再执行超时自动销毁，回放结束后会有事件通知
                                    }

                                    Common.NeedResponseRequests.TryAdd(
                                        "MEDIASTATUS" + sipResponse.Header.CallId,
                                        _task); //再次加入等待列表,播放完成时会回调,使用同一个callid
                                }

                                break;
                            case SIPMethodsEnum.BYE: //停止实时流成功回复
                                isChannleInvite = _task.Obj == null;
                                if (isChannleInvite)
                                {
                                    await InviteEnd(sipResponse, _task.SipChannel);
                                }
                                else
                                {
                                    var record = (RecordInfo.RecItem)_task.Obj;
                                    await InviteEnd(sipResponse, record);
                                    var ret1 = Common.NeedResponseRequests.TryRemove(
                                        "MEDIASTATUS" + sipResponse.Header.CallId,
                                        out NeedReturnTask _task1);
                                    if (ret1 && _task1 != null)
                                    {
                                        ((RecordInfo.RecItem)_task1.Obj).PushStatus = PushStatus.IDLE;
                                        ((RecordInfo.RecItem)_task1.Obj).MediaServerStreamInfo = null;
                                        Task.Run(() =>
                                        {
                                            OnInviteHistoryVideoFinished?.Invoke((RecordInfo.RecItem)_task1.Obj);
                                        }); //抛线程出去处理
                                        GCommon.Logger.Debug(
                                            $"[{Common.LoggerHead}]->结束点播->{_task1.SipDevice.DeviceId}->{_task1.SipChannel.DeviceId}->Stream->{((RecordInfo.RecItem)_task1.Obj).SsrcId}:{((RecordInfo.RecItem)_task1.Obj).Stream}");
                                    }
                                }

                                break;
                        }

                        try
                        {
                            var tmpSipDevice =
                                Common.SipDevices.FindLast(x => x.DeviceId.Equals(_task.SipDevice.DeviceId));
                            if (tmpSipDevice != null)
                            {
                                tmpSipDevice.LastSipResponse = sipResponse;
                            }

                            switch (_task.CommandType) //再次入列
                            {
                                case CommandType.RecordInfo:
                                    Common.NeedResponseRequests.TryAdd(
                                        _task.CommandType.ToString().ToUpper() + ":" + _task.SipDevice.DeviceId + ":" +
                                        _task.SipChannel.DeviceId + ":" + ((SipQueryRecordFile)_task.Obj).TaskId,
                                        _task); //再次加入等待列表,obj.TaskId中是外部生成的sn

                                    break;
                                case CommandType.Catalog:
                                    Common.NeedResponseRequests.TryAdd(
                                        _task.CommandType.ToString().ToUpper() + ":" + _task.SipDevice.DeviceId,
                                        _task); //再次加入等待列表

                                    break;
                            }

                            GCommon.Logger.Debug(
                                $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的SipResponse->{sipResponse}");
                            _task.AutoResetEvent.Set(); //通知调用者任务完成,凋用者后续要做dispose操作
                        }
                        catch (Exception ex)
                        {
                            GCommon.Logger.Error(ex.Message + "\r\n" + ex.StackTrace);
                        }
                    }
                    else
                    {
                        GCommon.Logger.Debug(
                            $"[{Common.LoggerHead}]->收到来自{remoteEndPoint}的SipResponse->{sipResponse}");
                    }

                    break;
            }
        }

        #region 各类事件

        /// <summary>
        /// sip服务状态
        /// </summary>
        public static event Action<string, ServiceStatus> OnServiceChanged = null!;

        /// <summary>
        /// 录像文件接收
        /// </summary>
        public static event Action<RecordInfo> OnRecordInfoReceived = null!;

        /// <summary>
        /// 设备目录接收
        /// </summary>
        public static event GCommon.CatalogReceived OnCatalogReceived = null!;

        /// <summary>
        /// 设备目录通知
        /// </summary>
        public static event Action<NotifyCatalog> OnNotifyCatalogReceived = null!;

        /// <summary>
        /// 语音广播通知
        /// </summary>
        public static event Action<VoiceBroadcastNotify> OnVoiceBroadcaseReceived = null!;

        /// <summary>
        /// 报警通知
        /// </summary>
        public static event Action<Alarm> OnAlarmReceived = null!;

        /// <summary>
        /// 平台之间心跳接收
        /// </summary>
        public static event GCommon.KeepaliveReceived OnKeepaliveReceived = null!;


        /// <summary>
        /// 设备就绪通知，当设备准备好的时候触发
        /// </summary>
        public static event GCommon.SipDeviceReadyReceived OnDeviceReadyReceived = null;

        /// <summary>
        /// 设备状态查询接收
        /// </summary>
        public static event GCommon.DeviceStatusReceived OnDeviceStatusReceived = null;

        /// <summary>
        /// 点播完成或结束事件
        /// </summary>
        public static event GCommon.InviteHistroyVideoFinished OnInviteHistoryVideoFinished = null;

        /// <summary>
        /// 设备信息查询接收
        /// </summary>
        public static event GCommon.DeviceInfoReceived OnDeviceInfoReceived = null;


        /// <summary>
        /// 设备配置查询接收
        /// </summary>
        public static event Action<SIPEndPoint, DeviceConfigDownload> OnDeviceConfigDownloadReceived = null!;

        /// <summary>
        /// 历史媒体发送结束接收
        /// </summary>
        public static event Action<SIPEndPoint, MediaStatus> OnMediaStatusReceived = null!;

        /// <summary>
        /// 响应状态码接收
        /// </summary>
        public static event Action<SIPResponse, string, SIPEndPoint> OnResponseCodeReceived = null!;

        /// <summary>
        /// 响应状态码接收
        /// </summary>
        public static event Action<SIPResponse, SIPRequest, string, SIPEndPoint> OnResponseNeedResponeReceived = null!;

        /// <summary>
        /// 预置位查询接收
        /// </summary>,
        public static event Action<SIPEndPoint, PresetInfo> OnPresetQueryReceived = null!;

        /// <summary>
        /// 设备注册时
        /// </summary>
        public static event GCommon.RegisterDelegate OnRegisterReceived = null!;

        /// <summary>
        /// 设备注销时
        /// </summary>
        public static event GCommon.UnRegisterDelegate OnUnRegisterReceived = null!;

        /// <summary>
        /// 设备有警告时
        /// </summary>
        public static event GCommon.DeviceAlarmSubscribeDelegate OnDeviceAlarmSubscribe = null!;

        /// <summary>
        /// 当设备发生注册鉴权时，需要返回值为此设备的鉴权密钥
        /// </summary>
        public static event GCommon.DeviceAuthentication OnDeviceAuthentication = null!;
        public static event GCommon.ReceiveInviteDelegate OnReceiveInvite = null!;
        public static event GCommon.ReceiveByeDelegate OnReceiveBye = null!;

        #endregion
    }
}