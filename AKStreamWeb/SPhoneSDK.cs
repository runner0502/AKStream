using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;



namespace XyCallLayer

//[DllImport(@"/home/tracy/github/pjsipfix/trunk/pjsip-apps/src/SoftPhone/libsphone.so")]
//[DllImport(@"./nativesip/libsphone.so")]
//[DllImport(@"DLL\SPhone.dll")]

{
    public static class SPhoneSDK
    {
        public enum TransportType
        {
            UDP = 1,
            TCP,
        };
        public enum CallState
        {
            STATE_NULL,	    /**< Before INVITE is sent or received  */
            STATE_CALLING,	    /**< After INVITE is sent		    */
            STATE_INCOMING,	    /**< After INVITE is received.	    */
            STATE_EARLY,	    /**< After response with To tag.	    */
            STATE_CONNECTING,	    /**< After 2xx is sent/received.	    */
            STATE_CONFIRMED,	    /**< After ACK is sent/received.	    */
            STATE_DISCONNECTED,   /**< Session is terminated.		    */
        };
        public enum StatusCode
        {
            TRYING = 100,
            RINGING = 180,
            CALL_BEING_FORWARDED = 181,
            QUEUED = 182,
            PROGRESS = 183,

            OK200 = 200,
            ACCEPTED = 202,

            MULTIPLE_CHOICES = 300,
            MOVED_PERMANENTLY = 301,
            MOVED_TEMPORARILY = 302,
            USE_PROXY = 305,
            ALTERNATIVE_SERVICE = 380,

            BAD_REQUEST = 400,
            UNAUTHORIZED = 401,
            PAYMENT_REQUIRED = 402,
            FORBIDDEN = 403,
            NOT_FOUND = 404,
            METHOD_NOT_ALLOWED = 405,
            NOT_ACCEPTABLE = 406,
            PROXY_AUTHENTICATION_REQUIRED = 407,
            REQUEST_TIMEOUT = 408,
            GONE = 410,
            REQUEST_ENTITY_TOO_LARGE = 413,
            REQUEST_URI_TOO_LONG = 414,
            UNSUPPORTED_MEDIA_TYPE = 415,
            UNSUPPORTED_URI_SCHEME = 416,
            BAD_EXTENSION = 420,
            EXTENSION_REQUIRED = 421,
            SESSION_TIMER_TOO_SMALL = 422,
            INTERVAL_TOO_BRIEF = 423,
            TEMPORARILY_UNAVAILABLE = 480,
            CALL_TSX_DOES_NOT_EXIST = 481,
            LOOP_DETECTED = 482,
            TOO_MANY_HOPS = 483,
            ADDRESS_INCOMPLETE = 484,
            AMBIGUOUS = 485,
            BUSY_HERE = 486,
            REQUEST_TERMINATED = 487,
            NOT_ACCEPTABLE_HERE = 488,
            BAD_EVENT = 489,
            REQUEST_UPDATED = 490,
            REQUEST_PENDING = 491,
            UNDECIPHERABLE = 493,

            INTERNAL_SERVER_ERROR = 500,
            NOT_IMPLEMENTED = 501,
            BAD_GATEWAY = 502,
            SERVICE_UNAVAILABLE = 503,
            SERVER_TIMEOUT = 504,
            VERSION_NOT_SUPPORTED = 505,
            MESSAGE_TOO_LARGE = 513,
            PRECONDITION_FAILURE = 580,

            BUSY_EVERYWHERE = 600,
            DECLINE = 603,
            DOES_NOT_EXIST_ANYWHERE = 604,
            NOT_ACCEPTABLE_ANYWHERE = 606,

            TSX_TIMEOUT = REQUEST_TIMEOUT,
            /*PJSIP_SC_TSX_RESOLVE_ERROR = 702,*/
            TSX_TRANSPORT_ERROR = SERVICE_UNAVAILABLE,
            /*add by liangjian*/
            LICENSE_NOT_ENOUGH = 10000, //license 授权不足
            REPEAT_LOGIN = 10001,       //重复登录
            RESERVE1 = 10002,       //保留1
            RESERVE2 = 10003,       //保留2
            RESERVE3 = 10004,		//保留3

            /* This is not an actual status code, but rather a constant
             * to force GCC to use 32bit to represent this enum, since
             * we have a code in PJSUA-LIB that assigns an integer
             * to this enum (see pjsua_acc_get_info() function).
             */
            force_32bit = 0x7FFFFFFF

        };
        public enum QosType
        {
            BEST_EFFORT,	/**< Best effort traffic (default value).
				     Any QoS function calls with specifying
				     this value are effectively no-op	*/
            BACKGROUND,	/**< Background traffic.		*/
            VIDEO,		/**< Video traffic.			*/
            VOICE,		/**< Voice traffic.			*/
            CONTROL		/**< Control traffic.			*/
        };
        public enum UserType
        {
            UserTypeDispatch,
            UserTypeHandheld,
            UserTypeCommonuser,
            UserTypeOutlineuser,
            UserTypeMonitoruser,
            UserTypeSsu,
            UserType3ghandheld,
            UserTypeMonitordevice,
            UserTypeNone,

            UserTypeTrunkDispatch = 100,
            UserTypeTrunkHandheld,
            UserTypeTrunkCommonuser,
            UserTypeTrunkOutlineuser,
            UserTypeTrunkMonitoruser,
            UserTypeTrunkSsu,
            UserTypeTrunk3ghandheld,
            UserTypeTrunkMonitordevice,
            UserTypeTrunkNone
        };
        public enum UserState
        {
            CallStateNone,
            CallStateInit,
            CallStateNormal,
            CallStateCallout,
            CallStateIncoming,
            CallStateRinging,
            CallStateConnect,
            CallStateHold,
            CallStateBusy,
            CallStateOffhook,
            CallStateRelease,
            CallStateUnspeak,
            CallStateSpeak,
            CallStateQueue,
            CallStateUnhold,
            CallStateZombie
        };

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        public struct VideoDeviceInfo
        {
            [MarshalAsAttribute(UnmanagedType.I4)]
            public int id;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string name;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string diver;

            /** Number of video formats supported by this device */
            int fmt_cnt;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            VideoFmt[] fmt;

        };
        struct VideoFmt
        {
            int id;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 8)]
            string fmtname;
            int w; /**< Video size (width) 	*/
            int h; /**< Video size (height) 	*/
            int fps;    /**< Number of frames per second.	*/
            int avg_bps;/**< Average bitrate.			*/
            int max_bps;/**< Maximum bitrate.			*/
        };
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        public struct AudioDeviceInfo
        {
            [MarshalAsAttribute(UnmanagedType.I4)]
            public int id;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string name;
            [MarshalAsAttribute(UnmanagedType.I4)]
            public int inputCnt;
            [MarshalAsAttribute(UnmanagedType.I4)]
            public int outputCnt;
        };
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        public struct PTTGroupInfo
        {
            [MarshalAsAttribute(UnmanagedType.I4)]
            public int id;
            [MarshalAsAttribute(UnmanagedType.I4)]
            public int islocal;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string groupName;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string groupNum;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string dnsprefix;
        };

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        public struct UserInfo
        {
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string userId;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string userName;
            public UserType typ;
            public UserState state;
            [MarshalAsAttribute(UnmanagedType.I4)]
            public int islocal;
            public bool isLogin;
            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string dnsprefix;
        };

        public delegate void SDK_onIncomingCall(int callid, string number, CallState state, bool isVideo);
        public delegate void SDK_onIncomingCall_WithIDS(int callid, string number, CallState state, bool isVideo, string idsContent);
        public delegate void SDK_onIncomingCall_WithMsg(int callid, string number, CallState state, bool isVideo, string idsContent);
        public delegate void SDK_onCallState(int callid, string number, CallState state, string stateText, bool isVideo);
        public delegate void SDK_onCallState_WithIDS(int callid, string number, CallState state, string stateText, bool isVideo, int hwnd, string idsContent);
        public delegate void SDK_onRegState(int accid, byte state, StatusCode code, string reason);
        public delegate void SDK_onSendMsgState(StatusCode status, string reason);
        public delegate void SDK_onReceiveMsg(string from, string msg);
        public delegate void SDK_onPTTState(string state);
        public delegate void SDK_onReceiveDtmf(int callid, string dtmf);
        public delegate void SDK_onReceiveKeyframeRequest(int callid);


        //来电回调
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_IncomingCall(SDK_onIncomingCall callback);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_IncomingCall_WithIDS(SDK_onIncomingCall_WithIDS callback);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_IncomingCall_WithMsg(SDK_onIncomingCall_WithMsg callback);
        //呼叫状态回调
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_CallState(SDK_onCallState callback);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_CallState_WithIDS(SDK_onCallState_WithIDS callback);
        //注册状态
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_RegState(SDK_onRegState callback);
        //短消息发送状态回调
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_SendMsgState(SDK_onSendMsgState callback);
        //收消息回调
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_ReceiveMsg(SDK_onReceiveMsg callback);
        //对讲状态通知
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_PTTState(SDK_onPTTState callback);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_ReceiveDtmf(SDK_onReceiveDtmf callback);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetCallback_ReceiveKeyframeRequest(SDK_onReceiveKeyframeRequest callback);

        //初始化
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SDKInit(string localIp, int localPort, int loglevel, string logFileName, TransportType typ = TransportType.UDP);
        //设置socket发送缓冲大小，默认128k，真实值需视系统api接口
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetSocketRcvBufferSize(int size);
        //销毁
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SDKDestory();
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void HandleIPChanged();
        //注册
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool Regist(string host, string uname, string pwd, bool mobile = false, bool localMode = false,
            int regTimeout = 60, QosType qos = QosType.BEST_EFFORT);
        //注销
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void UnRegist();
        //发起呼叫
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool MakeCall(string number, bool isVideo);
        //本地模式发起呼叫
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool MakeCallLocalMode(string uri, int port, bool isVideo);
        //应答
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void Answer(int callid, bool isVideo);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void AnswerWithHandle2(int callid, bool isVideo, int hwnd, StatusCode code);
        //挂断呼叫
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void Hangup(int callid);
        //挂断所有
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void HangupAll();
        //切换视频设备
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool ChangeVideoDevice(int callid, int deviceIndex);
        //切换音频输入设备
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool ChangeAudCaptureDevice(int callid, int deviceIndex);
        //切换音频输出设备
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool ChangeAudSpeakerDevice(int callid, int deviceIndex);
        //获取所有视频设备
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetVideoDevices([Out] VideoDeviceInfo[] info, out int len);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetVideoDevicesCount(out int len);
        //获取所有音频设备信息
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetAudioDevices([Out] AudioDeviceInfo[] info, out int len);
        //设置默认视频设备
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetDefaultVideoDevice(int index);
        //设置默认音频设备
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool SetDefaultAudioDevice(int input, int output);
        //预览-开始
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void Preview_Start();
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void Preview_Start_WithHandle(IntPtr p);
        //预览-结束
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void Preview_Stop();
        //设置视频图像句柄
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetVideoHandle(IntPtr p);
        //设置视频大小
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetVideoSize(int callid, int x, int y, int w, int h);
        //本地预览句柄
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetPreviewHandle(IntPtr h);
        //更改本地预览窗口大小
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetPreviewSize(int x, int y, int w, int h);
        //设置视频参数，分辨率、帧率、码率
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool SetVideoCodecParam(int w, int h, int fps, int bps);
        //发送关键帧
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SendKeyFrame(int callid);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SendKeyFrame1();
        //请求关键帧
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void RequestKeyFrame(int callid);
        //保持
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void Hold(int callid);
        //取保持
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void UnHold(int callid);
        //发送DTMF（rfc2833）
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SendDtmf(int callid, string dtmf);
        //发送短消息
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SendMsg(string num, string msg);
        //设置接收音频放大倍数
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetRxLevel(int callid, float value);
        //设置发送音频放大倍数
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetTxLevel(int callid, float value);

        //该量为实时音量幅度，无法作为声音大小量化系数
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetRxLevel(int callid, ref float value);
        //[DllImport(@"DLL\SPhone.dll")]
        //public extern static void GetTxLevel(int callid, ref float value); 

        //设置视频是否启用抖动缓冲,默认启用
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetVidUseJBuffer(bool value);
        //开启或关闭FEC编解码
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void EnableFec(bool fec);
        //设置fec参数，dropDelay抖动缓冲，redunMode模式（0=自动，1=固定冗余），redunRatio（冗余比率，有效值10~60），groupSize（FEC分组大小，建议值：D1=10,720P=16,1080P=22）
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetFecParams(int dropDelay_aud, int dropDelay_vid, int redunMode, int redunRatio, int groupSize);
        //呼叫中开启或关闭FEC编码（用于对比测试FEC效果），默认开启
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetFecEncodeEnable(int callid, bool bEnable);
        //设置指定呼叫的FEC编码丢包，用于对比测试及模拟丢包场景，type：0=不丢包，1=按间隔丢包，2=按随机比例丢包
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetFecForceEncodeLost(int callid, int type, int value);
        //获取指定呼叫的音视频下行丢包率，对结果乘了100后取整
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetDownLostRatio(int callid, out int aud, out int vid);
        //获取指定呼叫的音视频上行丢包率，对结果乘了100后取整
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetUpLostRatio(int callid, out int aud, out int vid);
        //获取指定呼叫的音视频双向时延，毫秒单位
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetDelayInMs(int callid, out int aud, out int vid);
        //获取指定呼叫的语音/视频上下行带宽，单位kbps
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetBandWidth(int callid, out int aud_up, out int aud_down, out int vid_up, out int vid_down);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetAudioCodecs(out string num, out int length);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetAudioCodecPriority(string num, int priority);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetVideoCodecs(out string num, out int length);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetVideoCodecPriority(string num, int priority);



        //开始接收并渲染视频vidType 0=h264,1=h265，目前仅支持h264，远程IP及端口主要用于nat打洞（本地端口及本地端口+1两个会被占用）
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool StartReceiveVideo(string remoteIp, int remotePort, int localPort, IntPtr h, int payload_type, int vidType = 0);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool StartSendVideo(string remoteIp, int remotePort, int localPort, int payload_type, int vidType = 0);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void GetReceiveVideoStatisticalInfo(int localPort, out int downLostRatio, out int downBandWidth, out int DelayInMs);
        //停止接收及渲染视频
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StopReceiveVideo(int localPort);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StopSendVideo(int localPort);
        //停止所有接收及渲染视频
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StopAllReceiver();

        //开始录制语音 id=callid或localport，支持格式：.wav .mp3 .aac .amr
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StartRecordAudio(int id, string fileName);
        //停止录制语音 id=callid或localport
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StopRecordAudio(int id);
        //开始录制视频 id=callid或localport fmt 0=mp4,1=avi
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StartRecordVideo(int id, string fileName);
        //停止录制视频 id=callid或localport
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StopRecordVideo(int id);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void TakeVideoSnapshoot(int id, string fileName);
        //设置视频渲染模式，0=拉伸 1=等比缩放，id=呼叫id或者本地port
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void SetVideoRenderMode(int id, int mode);
        //本地视频预览显示模式，，0=拉伸 1=等比缩放
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool Preview_Show_Mode(int mode);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void AudioDevTestStart(int capture, int speaker);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void AudioDevTestStop();
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void AudioDevTestGetLevel(out int captureLevel, out int speakerLevel);
        [DllImport(@"DLL\SPhone.dll")]
        //播放wav语音文件,callid>=0呼叫中播放，callid<0本地播放。16bit PCM mono/single channel (any clock rate is supported)
        public extern static void StartPlayAudio(int callid, string fileName, bool localPlay, bool loop);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void StopPlayAudio(int callid);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static int StartRecordLocalAudio(string fileName);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static int StopRecordLocalAudio();
        //开始录制本地视频
        [DllImport(@"DLL\SPhone.dll")]
        public extern static int StartRecordLocalVideo(string fileName);

        //开始录制本地视频
        [DllImport(@"DLL\SPhone.dll")]
        public extern static int StartRecordLocalVideo2(string fileName, int capid);
        //停止录制本地视频
        [DllImport(@"DLL\SPhone.dll")]
        public extern static int StopRecordLocalVideo();

        //本地视频截图 (必须开启视频预览)
        [DllImport(@"DLL\SPhone.dll")]
        public extern static int TakeLocalVideoSnapshoot(string fileName);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool TakeLocalVideoSnapshoot(bool bl);
        /// <summary>
        /// 设置视频无花屏
        /// </summary>
        /// <param name="val">设置=true，取消=flase</param>
        /// <returns></returns>
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool SetVideoNoBadPicture(bool val);
        /// <summary>
        /// 设置硬件加速
        /// </summary>
        /// <param name="val">设置=true，取消=flase</param>
        /// <returns></returns>
        [DllImport(@"DLL\SPhone.dll")]
        public extern static bool SetRendererUsesHardwareAcceleration(bool val);
        /// <summary>
        /// 共享屏幕-取消共享
        /// </summary>
        /// <param name="localPort"></param>
        /// <param name="deviceIndex"></param>
        [DllImport(@"DLL\SPhone.dll")]
        public extern static void ChangeSendVideoDevice(int localPort, int deviceIndex);

        [DllImport(@"DLL\SPhone.dll")]
        public extern static int SetupCaptureVideoFile(string fileName);
        [DllImport(@"DLL\SPhone.dll")]
        public extern static int SetupCaptureAudioFile(string fileName);
    }
}
