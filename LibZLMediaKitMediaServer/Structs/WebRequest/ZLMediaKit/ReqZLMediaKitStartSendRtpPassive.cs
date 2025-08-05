using System;

namespace LibZLMediaKitMediaServer.Structs.WebRequest.ZLMediaKit
{
    /// <summary>
    /// 向上级推rtp流请求结构
    /// </summary>
    [Serializable]
    public class ReqZLMediaKitStartSendRtpPassive : ReqZLMediaKitRequestBase
    {
        private string _app;
        private string _is_udp;
        private ushort _src_port;
        private string _ssrc;
        private string _stream;
        private string _vhost;
        private int? _pt;
        private int? _use_ps;
        private int? _only_audio;


        /// <summary>
        /// vhost
        /// </summary>
        public string Vhost
        {
            get => _vhost;
            set => _vhost = value;
        }

        /// <summary>
        /// app
        /// </summary>
        public string App
        {
            get => _app;
            set => _app = value;
        }

        /// <summary>
        /// stream
        /// </summary>
        public string Stream
        {
            get => _stream;
            set => _stream = value;
        }

        /// <summary>
        /// s推流的rtp的ssrc,指定不同的ssrc可以同时推流到多个服务器
        /// </summary>
        public string Ssrc
        {
            get => _ssrc;
            set => _ssrc = value;
        }

        /// <summary>
        /// 是否udp
        /// </summary>
        public string Is_Udp
        {
            get => _is_udp;
            set => _is_udp = value;
        }

        /// <summary>
        /// 使用的本机端口，为0或不传时默认为随机端口
        /// </summary>
        public ushort Src_Port
        {
            get => _src_port;
            set => _src_port = value;
        }
        /// <summary>
        /// 发送时，rtp的pt（uint8_t）,不传时默认为96
        /// </summary>
        public int? Pt { get => _pt; set => _pt = value; }
        /// <summary>
        /// 发送时，rtp的负载类型。为1时，负载为ps；为0时，为es；不传时默认为1
        /// </summary>
        public int? Use_ps { get => _use_ps; set => _use_ps = value; }
        /// <summary>
        /// 当use_ps 为0时，有效。为1时，发送音频；为0时，发送视频；不传时默认为0
        /// </summary>
        public int? Only_audio { get => _only_audio; set => _only_audio = value; }

        public override string ToString()
        {
            return
                $"{nameof(Vhost)}: {Vhost}, {nameof(App)}: {App}, {nameof(Stream)}: {Stream}, {nameof(Ssrc)}: {Ssrc}, {nameof(Is_Udp)}: {Is_Udp}, {nameof(Src_Port)}: {Src_Port}";
        }
    }
}