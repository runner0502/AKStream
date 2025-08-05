using System;
using System.Text.Json.Serialization;

namespace LibZLMediaKitMediaServer.Structs.WebRequest.ZLMediaKit
{
    [Serializable]
    public class ReqZLMediaKitConnectRtpSever : ReqZLMediaKitRequestBase
    {
        private string? _dst_url;
        private ushort? _dst_port;
        private string? _stream_id;

        [JsonIgnore]
        public ushort? Dst_Port
        {
            get => _dst_port;
            set => _dst_port = value;
        }

        [JsonIgnore]
        public string? Dst_Url
        {
            get => _dst_url;
            set => _dst_url = value;
        }

        public string? Stream_Id
        {
            get => _stream_id;
            set => _stream_id = value;
        }
    }
}