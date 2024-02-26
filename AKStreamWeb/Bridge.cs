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

        private Bridge()
        {
            SPhoneSDK.SDKInit("0.0.0.0", 5066, 5, System.AppContext.BaseDirectory + "pjsip.log");
            //SPhoneSDK.SDKInit("172.19.6.41", 5066, 5, System.AppContext.BaseDirectory +  "pjsip.log");
            SPhoneSDK.Regist("1.1.1.1", "admin", "admin", false, true);
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


        }

        public void OnCallState(int callid, string number, CallState state, string stateText, bool isVideo)
        {
            switch (state) 
            {
                case CallState.STATE_DISCONNECTED:
                    var call = s_calls[callid];
                    if (call != null) 
                    {
                        s_calls.Remove(callid);
                    }
                    break;
            }
        }


        //private Timer _timer;

        //private void TestTimerCB(object obj)
        //{
        //    var device = LibGB28181SipServer.Common.SipDevices.Find(x => x.DeviceId == "11011200002000000001");
        //    if (device == null)
        //    {
        //        return;
        //    }
        //    var sipChannel = device.SipChannels.Find(x => x.DeviceId == "11010000581314000001");
        //    Common.SipServer.Subscribe(device, sipChannel, SIPSorcery.SIP.SIPMethodsEnum.OPTIONS, "", "", "", LibCommon.Structs.GB28181.XML.CommandType.Catalog, false, null, null, null, 100);
        //}

        public static Dictionary<int, SipChannel> s_calls = new Dictionary<int, SipChannel>();

        public static void OnIncomingCall_WithMsg(int callid, string number, CallState state, bool isVideo, string idsContent)
        //public static void OnIncomingCall(int callid, string number, CallState state, bool isVideo)
        {
            ResponseStruct rs;
            //string deviceId = "33020000021180000006";
            //string channelId = "34020000001320000012";
            string deviceId = "";
            string channelId = "";

        
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

                        channelId = to;
                    }
                }
            }

            //var channel = ORMHelper.Db.Select<DeviceNumber>().Where(x => x.dev.Equals(channelId)).First();

            //if (channel != null)
            //{
            //    var plat= ORMHelper.Db.Select<Device281Plat>().Where(x => x.ipaddr.Equals(channel.domain)).First();
            //    if (plat != null)
            //    {
            //        deviceId = plat.platid;
            //    }
            //}

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
         $"[{Common.LoggerHead}]->SIP来电号码信息错误->{deviceId}-{channelId}");

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
                    $"[{Common.LoggerHead}]->获取通道失败->{deviceId}-{channelId}->{JsonHelper.ToJson(rs)}");

                return ;
            }

            if (s_calls.ContainsKey(callid))
            {
                s_calls.Remove(callid);
            }

            s_calls.Add(callid, sipChannel);

            var ret = SipServerService.LiveVideo(deviceId, channelId, out rs);
            if (ret == null)
            {
                return;
            }
            //var ret = SipServerService.LiveVideo("11011200002000000001", "11010000581314000001", out rs);

            if (!rs.Code.Equals(ErrorNumber.None))
            {
                throw new AkStreamException(rs);
            }


            string url = ret.PlayUrl.Find(a => a.StartsWith("rtsp"));
            if (!string.IsNullOrEmpty(url))
            {
                int deviceIdVideo = SetupCaptureVideoFile(url);
                if (deviceIdVideo > 0)
                {
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

                //SetupCaptureAudioFile(url);

                //AudioDeviceInfo[] audioDevices = new AudioDeviceInfo[100];
                //SPhoneSDK.GetAudioDevices(audioDevices, out len);
                //if (len > 0)
                //{
                //    var deviceIdVideo = audioDevices[len - 1].id;
                //    SPhoneSDK.SetDefaultAudioDevice(deviceIdVideo, deviceIdVideo);
                //    //System.Threading.Thread.Sleep(1000);
                //}


                Answer(callid, true);
            }

        }

        private static SDK_onIncomingCall_WithMsg _onIncoming;
        private static SDK_onReceiveDtmf _onReceiveDtmf;
        private static SDK_onReceiveKeyframeRequest _onReceiveKeyframeRequest;
        private static SDK_onCallState _onCallstatechange;

        public static void OnReceiveDtmf(int callid, string dtmf)
        {
            dtmf = dtmf.Replace("\r", "");
            GCommon.Logger.Warn("OnReceiveDtmf callid: " + callid + ", dtmf: " + dtmf);
            var sipChannel = s_calls[callid];
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
            var sipChannel = s_calls[callid];
            if (sipChannel == null)
            {
                GCommon.Logger.Warn("OnReceiveKeyframeRequest not find sipchannel callid: " + callid );
                return;
            }
            ResponseStruct rs;

            var ret = SipServerService.ForceKeyframe(sipChannel.ParentId, sipChannel.DeviceId, out rs);
            if (!rs.Code.Equals(ErrorNumber.None))
            {
                throw new AkStreamException(rs);
            }

            //var device = LibGB28181SipServer.Common.SipDevices.Find(x => x.DeviceId == sipChannel.ParentId);
            //Common.SipServer.Subscribe(device, sipChannel, SIPSorcery.SIP.SIPMethodsEnum.OPTIONS, "", "", "", LibCommon.Structs.GB28181.XML.CommandType.Catalog, false, null, null, null, 100);
        }

    }
}
