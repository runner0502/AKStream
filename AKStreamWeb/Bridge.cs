using AKStreamWeb.Services;
using LibCommon.Structs.WebRequest;
using LibCommon;
using XyCallLayer;
using static XyCallLayer.SPhoneSDK;
using System.Collections.Generic;
using LibCommon.Structs.DBModels;
using LibCommon.Structs.GB28181;
using LibZLMediaKitMediaServer;
using System.Threading.Channels;
using WebSocketSharp;
using LibCommon.Structs.GB28181.XML;
using System.Threading;
using SIPSorcery.SIP;
using LinCms.Core.Entities;
using System.Runtime.InteropServices;
using LibGB28181SipServer;
using System.Linq;
using System;
using System.ComponentModel;

namespace AKStreamWeb
{
    public class Bridge
    {

        private static Bridge s_instance;
        public static Bridge GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = new Bridge();
            }
            return s_instance;
        }

        private static bool _transcode = false;
        private static bool _enableVoice = false;

        private static object _lock = new object();

        private Bridge()
        {
            SPhoneSDK.SDKInit( Common.AkStreamWebConfig.SipIp, Common.AkStreamWebConfig.SipPort, 5, System.AppContext.BaseDirectory + "pjsip.log");
            //SPhoneSDK.SDKInit("172.19.6.41", 5066, 5, System.AppContext.BaseDirectory +  "pjsip.log");
            SPhoneSDK.Regist("1.1.1.1", "admin", "admin", Common.AkStreamWebConfig.PublicMediaIp, false, true);
            _onIncoming = OnIncomingCall_WithMsg;
            SPhoneSDK.SetCallback_IncomingCall_WithMsg(_onIncoming);
            _onReceiveDtmf = OnReceiveDtmf;
            SPhoneSDK.SetCallback_ReceiveDtmf(_onReceiveDtmf);
            _onCallstatechange = OnCallState;
            SPhoneSDK.SetCallback_CallState(_onCallstatechange);

            _onReceiveKeyframeRequest = OnReceiveKeyframeRequest;
            SPhoneSDK.SetCallback_ReceiveKeyframeRequest(_onReceiveKeyframeRequest);
            //SPhoneSDK.SetCallback_IncomingCall(OnIncomingCall);
            SPhoneSDK.SetDefaultVideoDevice(1);

            //_timer = new Timer(TestTimerCB, null, 10000, 10000);

            var basicConfig = ORMHelper.Db.Select<SysAdvancedConfig>().First();
            if (basicConfig != null) 
            {
                if (basicConfig.TranscodeEnable == 1)
                {
                    _transcode = true;
                   // SPhoneSDK.SetVidHardwareEncoding(false);
                }
                if (basicConfig.IntercomEnable == 1)
                {
                    _enableVoice = true;
                }
            }

            SipMsgProcess.OnReceiveInvite += SipMsgProcess_OnReceiveInvite;
            SipMsgProcess.OnReceiveBye += SipMsgProcess_OnReceiveBye;

        }

        private void SipMsgProcess_OnReceiveBye(SIPRequest req)
        {
            GCommon.Logger.Warn("SipMsgProcess_OnReceiveBye");
            foreach (var item in LibGB28181SipServer.Common.SipDevices)
            {
                foreach (var channel in item.SipChannels)
                {
                    if (channel.Callid == req.Header.CallId)
                    {
                        channel.Callid = "";
                        channel.AudioPortConf = -1;
                        channel.SipCallid = -1;
                        GCommon.Logger.Warn("SipMsgProcess_OnReceiveBye find channelid: " + channel.DeviceId);
                        return;
                    }
                }
            } 
        }

        static int s_LocalPort = 7000;

        private void SipMsgProcess_OnReceiveInvite(LibCommon.Structs.ShareInviteInfo info, SIPRequest req)
        {
            if (s_calls.Count > 0)
            {
                int result= StartAudioSendStream(s_LocalPort, info.RemoteIpAddress, info.RemotePort, s_callidIntercom);
                if (result > 0)
                {
                    GCommon.Logger.Warn("StartAudioSendStream success AudioPortConf: " + result + "," + req.Header.CallId);
                    info.LocalRtpPort = (ushort)s_LocalPort;
                    Common.SipServer.SendInviteOK(req, info);
                    s_calls[s_callidIntercom].SipChannel.AudioPortConf = result;
                    s_calls[s_callidIntercom].SipChannel.Callid = req.Header.CallId;
                    s_calls[s_callidIntercom].SipChannel.SipCallid = s_callidIntercom;
                }
                else
                {
                    GCommon.Logger.Warn("StartAudioSendStream fail");
                }
                s_LocalPort += 2;

            }
        }

        public void OnCallState(int callid, string number, CallState state, string stateText, bool isVideo)
        {
            switch (state) 
            {
                case CallState.STATE_DISCONNECTED:
                    try
                    {
                        GCommon.Logger.Debug("onsipcallstate: disconnect callid: " + callid);
                        lock (_lock)
                        {
                            var call = s_calls[callid];
                            if (call != null)
                            {
                                if (call.SipChannel.SipCallid == callid)
                                {
                                    GCommon.Logger.Debug("onsipcallstate: disconnect callid: " + callid + ", set broadcat terminal");
                                    call.SipChannel.SipCallid = -1;
                                }
                                s_calls.Remove(callid);
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                    }
                    
                    break;
            }
        }


        private Timer _timer;

        private void TestTimerCB(object obj)
        {
            var device = LibGB28181SipServer.Common.SipDevices.Find(x => x.DeviceId == "43100000122000900001");
            if (device == null)
            {
                return;
            }
            var sipChannel = device.SipChannels.Find(x => x.DeviceId == "43100000001310615349");
            Common.SipServer.Subscribe(device, sipChannel, SIPSorcery.SIP.SIPMethodsEnum.OPTIONS, "", "", "", LibCommon.Structs.GB28181.XML.CommandType.MobilePosition, false, null, null, null, 100);
            _timer.Dispose();
        }

        public void Subcribe()
        {
            _timer = new Timer(TestTimerCB, null, 10000, 1000000000);
        }

    public static Dictionary<int, CallInfoInternal> s_calls = new Dictionary<int, CallInfoInternal>();

        public static int s_callidIntercom = 0;

        public static void OnIncomingCall_WithMsg(int callid, string number, CallState state, bool isVideo, string idsContent)
        //public static void OnIncomingCall(int callid, string number, CallState state, bool isVideo)
        {
            GCommon.Logger.Warn("sipincoming start：" + callid);
            string msg;
            if (Common.License.DoExtraValidation(out msg) != QLicenseCore.LicenseStatus.VALID)
            {
                GCommon.Logger.Warn("sipincoming license fail: " + msg);
                return;
            }
            ResponseStruct rs;
            //string deviceId = "33020000021180000006";
            //string channelId = "34020000001320000012";
            string deviceId = "";
            string channelId = "";
            string numberdb = "";

        
            string findStr = "To: sip:";
            int toIndex = idsContent.IndexOf(findStr);
            if (toIndex <= 0)
            {
                findStr = "To: <sip:";
                toIndex = idsContent.IndexOf(findStr);
            }
            if (toIndex <= 0)
            {
                findStr = "t: <sip:";
                toIndex = idsContent.IndexOf(findStr);
            }
            if (toIndex <= 0)
            {
                findStr = "t: sip:";
                toIndex = idsContent.IndexOf(findStr);
            }

            if (toIndex > 0)
            {
                int startIndex = toIndex + findStr.Length;
                int endIndex = idsContent.IndexOf("@", startIndex);
                if (endIndex > 0) 
                {
                    string to = idsContent.Substring(startIndex, endIndex - startIndex);
                    if (!to.IsNullOrEmpty())
                    {
                        //var strs = to.Split("_");
                        //if (strs != null && strs.Length == 2)
                        //{
                        //    deviceId = strs[0];
                        //    channelId = strs[1];
                        //}

                        numberdb = to;
                    }
                }
            }

            var channeldb = ORMHelper.Db.Select<DeviceNumber>().Where(x => x.num.Equals(numberdb)).First();

            if (channeldb != null)
            {
                //var plat = ORMHelper.Db.Select<Device281Plat>().Where(x => x.ipaddr.Equals(channel.domain)).First();
                //if (plat != null)
                //{
                //    deviceId = plat.platid;
                //}
                channelId = channeldb.dev;
            }
            if (string.IsNullOrEmpty(channelId))
            {
                GCommon.Logger.Warn("sipincoming 没有这个号码：" + numberdb);
                return;
            }



            foreach (var device in LibGB28181SipServer.Common.SipDevices)
            {
                foreach (var channel in device.SipChannels)
                {
                    if (channel.SipChannelDesc.DeviceID == channelId)
                    {
                        deviceId = device.DeviceId;
                        break;
                    }
                }
            }

            if (deviceId.IsNullOrEmpty() || channelId.IsNullOrEmpty())
            {
                GCommon.Logger.Warn(
         $"[{Common.LoggerHead}]->SIP来电号码信息错误sipincoming->{deviceId}-{channelId}");

                return;
            }

            ServerInstance mediaServer;
            VideoChannel videoChannel;
            SipDevice sipDevice;
            SipChannel sipChannel;
            rs = new ResponseStruct()
            {
                Code = ErrorNumber.None,
                Message = ErrorMessage.ErrorDic![ErrorNumber.None],
            };
            sipChannel = SipServerService.GetSipChannelById(deviceId, channelId, out rs);
            if (sipChannel == null)
            {
                GCommon.Logger.Warn(
                    $"[{Common.LoggerHead}]->获取通道失败 sipincoming->{deviceId}-{channelId}->{JsonHelper.ToJson(rs)}");

                return ;
            }


            GCommon.Logger.Warn("sipincoming before livevideo " + callid);
            var ret = SipServerService.LiveVideo(deviceId, channelId, out rs);
            GCommon.Logger.Warn("sipincoming end livevideo " + callid);

            if (ret == null)
            {
                GCommon.Logger.Warn("sipincoming livevideo fail：" + numberdb);
                return;
            }
            //var ret = SipServerService.LiveVideo("11011200002000000001", "11010000581314000001", out rs);

            //if (!rs.Code.Equals(ErrorNumber.None))
            //{
            //    throw new AkStreamException(rs);
            //}


            string url = ret.PlayUrl.Find(a => a.StartsWith("rtsp"));
            if (!string.IsNullOrEmpty(url))
            {
                SetHardEncodeVideo(callid, 1);

                var basicConfig = ORMHelper.Db.Select<SysAdvancedConfig>().First();
                if (basicConfig != null)
                {
                    if (basicConfig.TranscodeEnable == 1)
                    {
                        _transcode = true;
                        //SPhoneSDK.SetVidHardwareEncoding(false);
                    }
                }

                var callinfo = new CallInfoInternal();
                callinfo.SipChannel = sipChannel;
                callinfo.IsTranscode = false;
                callinfo.called = numberdb;
                callinfo.caller = number;
                callinfo.CameraName = channeldb.name;
                callinfo.CreateTime = DateTime.Now;
                callinfo.Reslution = "640*480";
                callinfo.calledDeviceNumber = numberdb;
                try
                {
                    string findStrip = "SIP/2.0/UDP ";
                    int ipindex = idsContent.IndexOf(findStrip);
                    int startIndex = ipindex + findStrip.Length;
                    int ipendIndex = idsContent.IndexOf(":", ipindex);
                    callinfo.CallerIP = idsContent.Substring(startIndex, ipendIndex - startIndex);
                }
                catch (Exception ex) { }
                bool isTranscode = false;
                if (_transcode)
                {
                    var transcodeConfig = ORMHelper.Db.Select<biz_transcode>().Where(a=>number.StartsWith(a.caller_number)).Where(a=>a.state == "1").First();
                    if (transcodeConfig != null )
                    {
                        int currentTranscodeCount = 0;
                        foreach (var item in s_calls)
                        {
                            if (item.Value.IsTranscode)
                            {
                                currentTranscodeCount++;
                            }
                        }
                        if (currentTranscodeCount >= Common.License.MaxRunCount)
                        {
                            GCommon.Logger.Warn("sipincoming license fail transcode too many");
                        }
                        else
                        {
                            isTranscode = true;
                            SetHardEncodeVideo(callid, 0);
                            callinfo.IsTranscode = true;
                            if (transcodeConfig.EncoderType == 0)
                            {
                                SetVideoCodecPriority("H265/103", 0);
                            }
                            else
                            {
                                SetVideoCodecPriority("H265/103", 254);
                            }

                            if (!string.IsNullOrEmpty(transcodeConfig.reslution))
                            {
                                var res = transcodeConfig.reslution.Split("*");
                                if (res != null && res.Length == 2)
                                {
                                    try
                                    {
                                        int width = int.Parse(res[0]);
                                        int height = int.Parse(res[1]);
                                        if (width > 0 && height > 0)
                                        {
                                            SetVideoCodecParam(width, height, 30, 650 * 1024);
                                            callinfo.Reslution = width + "*" + height;
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                        GCommon.Logger.Warn("transcode reslution format error");
                                    }
                                }
                            }
                        }
                    }
                }

                GCommon.Logger.Warn("sipincoming before videocapture " + callid);
                int deviceIdVideo = SetupCaptureVideoFile(url);
                GCommon.Logger.Warn("sipincoming end videocapture " + callid);

                if (deviceIdVideo > 0)
                {
                    if (!isTranscode)
                    {
                        int width = GetVideoDeviceWidth(deviceIdVideo);
                        int height = GetVideoDeviceHeight(deviceIdVideo);
                        callinfo.Reslution = width + "*" + height;
                    }
                    SPhoneSDK.ChangeVideoDevice1(callid, deviceIdVideo);
                    SPhoneSDK.SetDefaultVideoDevice(deviceIdVideo);
                }

                //SPhoneSDK.VideoDeviceInfo[] VideoDeviceInfos1 = new SPhoneSDK.VideoDeviceInfo[100];
                //int len = 0;
                //SPhoneSDK.GetVideoDevices(VideoDeviceInfos1, out len);
                //if (len > 0)
                //{
                //    var deviceIdVideo = VideoDeviceInfos1[len - 1].id;
                //    SPhoneSDK.SetDefaultVideoDevice(deviceIdVideo);
                //    //System.Threading.Thread.Sleep(1000);
                //}

                SetupCaptureAudioFile(url);
                int len = 0;
                AudioDeviceInfo[] audioDevices = new AudioDeviceInfo[100];
                SPhoneSDK.GetAudioDevices(audioDevices, out len);
                if (len > 0)
                {
                    var deviceIdAudio = audioDevices[len - 1].id;
                    SPhoneSDK.SetDefaultAudioDevice(deviceIdAudio, deviceIdAudio);
                    //System.Threading.Thread.Sleep(1000);
                }

                GCommon.Logger.Warn("sipincoming answer：" + numberdb);
                lock (_lock)
                {
                if (s_calls.ContainsKey(callid))
                {
                    s_calls.Remove(callid);
                }
                }


                lock (_lock)
                {
                    s_calls.Add(callid, callinfo);
                }
                Answer(callid, true);

                if (_enableVoice)
                {
                    if (sipChannel.AudioPortConf == -1)
                    {
                        GCommon.Logger.Warn("incoming request broadcast");
                        s_callidIntercom = callid;
                        SipMethodProxy sipMethodProxy = new SipMethodProxy(Common.AkStreamWebConfig.WaitSipRequestTimeOutMSec);
                        var result = sipMethodProxy.BroadcastRequest(deviceId, channelId);
                    }
                    else
                    {
                        //if (sipChannel.SipCallid >= 0)
                        //{
                        //    GCommon.Logger.Warn("incoming broadcast speaking do nothing");
                        //}
                        //else
                        //{
                            var th = new Thread(() =>
                            {
                                Thread.Sleep(500);
                                GCommon.Logger.Warn("incoming AddToAudioPort");
                                SPhoneSDK.AddToAudioPort(callid, sipChannel.AudioPortConf);
                                sipChannel.SipCallid = callid;
                            });
                            th.Start();
                        //}
                    }
                }
            }
            else
            {
                GCommon.Logger.Warn("sipincoming fail： mediaserver url is null");
            }

            GCommon.Logger.Warn("sipincoming end：" + callid);
        }

        private static SDK_onIncomingCall_WithMsg _onIncoming;
        private static SDK_onReceiveDtmf _onReceiveDtmf;
        private static SDK_onReceiveKeyframeRequest _onReceiveKeyframeRequest;
        private static SDK_onCallState _onCallstatechange;

        public static void OnReceiveDtmf(int callid, string dtmf)
        {
            dtmf = dtmf.Replace("\r", "");
            GCommon.Logger.Warn("OnReceiveDtmf callid: " + callid + ", dtmf: " + dtmf);

            SipChannel sipChannel = null;
            lock (_lock)
            {
                var info = s_calls[callid];
                if (info != null)
                {
                    sipChannel = s_calls[callid].SipChannel;
                }
            }
            if (sipChannel == null) 
            {
                GCommon.Logger.Warn("OnReceiveDtmf not find sipchannel callid: " + callid + ", dtmf: " + dtmf);
                return;
            }

            ResponseStruct rs;
            ReqPtzCtrl cmd = new ReqPtzCtrl();
            //cmd.ChannelId = "34020000001320000012";
            //cmd.DeviceId = "33020000021180000006";
            cmd.ChannelId = sipChannel.DeviceId;
            cmd.DeviceId = sipChannel.ParentId;
            cmd.Speed = 100;
            switch (dtmf)
            {
                case "1":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Zoom1;
                    break;
                case "3":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Zoom2;
                    break;
                case "7":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Focus2;
                    break;
                case "9":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Focus1;
                    break;
                case "5":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Stop;
                    break;
                case "8":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Up;
                    break;
                case "4":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Left;
                    break;
                case "6":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Right;
                    break;
                case "2":
                    cmd.PtzCommandType = LibCommon.Enums.PTZCommandType.Down;
                    break;
                default:
                    break;
            }


            var ret = SipServerService.PtzCtrl(cmd, out rs);
            if (!rs.Code.Equals(ErrorNumber.None))
            {
                throw new AkStreamException(rs);
            }

            // return ret;
        }

        public static void OnReceiveKeyframeRequest(int callid)
        {
            GCommon.Logger.Warn("OnReceiveKeyframeRequest callid: " + callid);
            try
            {
                SipChannel sipChannel = null;
                lock (_lock)
                {
                    if (s_calls != null && s_calls.Count > 0)
                    {
                        var info = s_calls[callid];
                        if (info != null)
                        {
                            sipChannel = info.SipChannel;
                        }
                    }
                }
                    if (sipChannel == null)
                    {
                        GCommon.Logger.Warn("OnReceiveKeyframeRequest not find sipchannel callid: " + callid);
                        return;
                    }
                    ResponseStruct rs;

                    var ret = SipServerService.ForceKeyframe(sipChannel.ParentId, sipChannel.DeviceId, out rs);
            }
            catch (Exception ex)
            {
                GCommon.Logger.Warn("OnReceiveKeyframeRequest callid: " + callid + ", fail :" +ex.Message);
            }
            //if (!rs.Code.Equals(ErrorNumber.None))
            //{
            //    throw new AkStreamException(rs);
            //}

            //var device = LibGB28181SipServer.Common.SipDevices.Find(x => x.DeviceId == sipChannel.ParentId);
            //Common.SipServer.Subscribe(device, sipChannel, SIPSorcery.SIP.SIPMethodsEnum.OPTIONS, "", "", "", LibCommon.Structs.GB28181.XML.CommandType.Catalog, false, null, null, null, 100);
        }

    }

    public class CallInfoInternal
    {
        public SipChannel SipChannel { get; set; }
        public bool IsTranscode { get; set; }

        public string caller { get; set; }
        public string called { get; set; }
        public string CameraName { get; set; }
        public DateTime CreateTime { get; set; }
        public string Reslution { get; set; }
        public string CallerIP { get; set; }
        public string calledDeviceNumber { get; set; }
        

    }
}
