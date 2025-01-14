using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Xml.Linq;
using LibCommon;
using LibCommon.Enums;
using LibCommon.Structs;
using LibCommon.Structs.DBModels;
using LibCommon.Structs.GB28181;
using LibCommon.Structs.GB28181.XML;
using LibSystemInfo;
using LinCms.Core.Entities;
using Newtonsoft.Json;
using SIPSorcery.SIP;

namespace LibGB28181SipServer
{
    public static class Common
    {
        public const int SIP_REGISTER_MIN_INTERVAL_SEC = 30; //最小Sip设备注册间隔
        public const int SIP_REGISTER_MAX_INTERVAL_SEC = 1800; //最大Sip设备注册间隔
        private static string _loggerHead = "SipServer";
        private static SipServerConfig _sipServerConfig = null!;
        private static string _sipServerConfigPath = GCommon.ConfigPath + "SipServerConfig.json";
        private static List<SipDevice> _sipDevices = new List<SipDevice>();
        private static ConcurrentQueue<Catalog> _tmpCatalogs = new ConcurrentQueue<Catalog>();
        private static ConcurrentQueue<RecordInfoEx> _tmpRecItems = new ConcurrentQueue<RecordInfoEx>();


        /// <summary>
        /// 用于操作_sipDevices时的锁
        /// </summary>
        public static object SipDevicesLock = new object();

        /// <summary>
        /// sip服务实例
        /// </summary>
        public static SipServer SipServer = null!;

        private static ConcurrentDictionary<string, NeedReturnTask> _needResponseRequests =
            new ConcurrentDictionary<string, NeedReturnTask>();

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
        static Common()
        {
            //string bodyXml1 = "<?xml version=\"1.0\"?>\r\n<Notify>\r\n<CmdType>Catalog</CmdType>\r\n<SN>45979</SN>\r\n<DeviceID>43100000122000900001</DeviceID>\r\n<SumNum>1</SumNum>\r\n<DeviceList Num=\"1\">\r\n<Item>\r\n<DeviceID>43102200121327000102</DeviceID>\r\n<Event>ON</Event>\r\n</Item>\r\n</DeviceList>\r\n</Notify>\r\n";
            //XElement bodyXml = XElement.Parse(bodyXml1);
            //UtilsHelper.XMLToObject<Catalog>(bodyXml);

            //try
            //{
            //    Common.TmpCatalogs.Enqueue(UtilsHelper.XMLToObject<Catalog>(bodyXml));
            //}
            //catch (Exception ex)
            //{
            //    var notifyCata = (UtilsHelper.XMLToObject<NotifyCatalog>(bodyXml));

            //    var config1 = ORMHelper.Db.Select<SysAdvancedConfig>().First();
            //    if (config1 != null)
            //    {
            //        GCommon.Logger.Debug("MessageProcess4");
            //        if (notifyCata.DeviceList != null && notifyCata.DeviceList.Items != null)
            //        {
            //            GCommon.Logger.Debug("MessageProcess5");

            //            foreach (var item in notifyCata.DeviceList.Items)
            //            {
            //                GCommon.Logger.Debug("MessageProcess6");

            //                using (HttpClient client = new HttpClient())
            //                {
            //                    var formData = new MultipartFormDataContent();
            //                    // 添加表单数据
            //                    formData.Add(new StringContent(config1.PushGisType), "mqType");
            //                    //formData.Add(new StringContent("queue.gis.third"), "topic");

            //                    formData.Add(new StringContent(config1.PushRegistStateTopic), "topic");
            //                    //string time1 = bodyXml.Element("Time")?.Value;
            //                    info1 info1 = new info1();
            //                    info1.user = bodyXml.Element("DeviceID")?.Value;
            //                    info1.name = "2";
            //                    //info1.lat = bodyXml.Element("Latitude")?.Value;
            //                    //info1.lon = bodyXml.Element("Longitude")?.Value;
            //                    //info1.time = time1;
            //                    info1.type = "3";
            //                    info1.subtype = "213";
            //                    if (item.Status == LibCommon.Structs.GB28181.Sys.DevStatus.OFF)
            //                    {
            //                        info1.status = 0;
            //                    }
            //                    else
            //                    {
            //                        info1.status = 1;
            //                    }

            //                    var deviceinfo = ORMHelper.Db.Select<DevicePlus>().Where(a => a.id == info1.user).First();
            //                    if (deviceinfo != null)
            //                    {
            //                        info1.DeviceType = deviceinfo.type;
            //                        info1.DeviceInfo = deviceinfo.info;
            //                    }
            //                    var data = LibCommon.JsonHelper.ToJson(info1, Formatting.None, MissingMemberHandling.Error);
            //                    formData.Add(new StringContent(data), "body");

            //                    //var httpRet = client.PostAsync("http://65.176.4.95:58080/api/ice/sendMsgByMQ", formData).Result.Content.ReadAsStringAsync();
            //                    var httpRet = client.PostAsync(config1.PushGisUrl, formData).Result.Content.ReadAsStringAsync();
            //                    GCommon.Logger.Warn("catalognotify send http "  + ", " + httpRet);
            //                }
            //            }
            //        }

            //    }
            //}
        }

        /// <summary>
        /// sip设备列表
        /// </summary>
        public static List<SipDevice> SipDevices
        {
            get => _sipDevices;
            set => _sipDevices = value;
        }

        /// <summary>
        /// Sip网关配置实例
        /// </summary>
        /// 
        public static SipServerConfig SipServerConfig
        {
            get => _sipServerConfig;
            set => _sipServerConfig = value;
        }

        /// <summary>
        /// Sip网关配置文件路径
        /// </summary>
        public static string SipServerConfigPath
        {
            get => _sipServerConfigPath;
            set => _sipServerConfigPath = value;
        }

        /// <summary>
        /// 日志头标识
        /// </summary>
        public static string LoggerHead
        {
            get => _loggerHead;
            set => _loggerHead = value;
        }

        /// <summary>
        /// 需要信息回复的消息列表
        /// </summary>
        public static ConcurrentDictionary<string, NeedReturnTask> NeedResponseRequests
        {
            get => _needResponseRequests;
            set => _needResponseRequests = value;
        }


        /// <summary>
        /// 收到的设备目录线程安全队列，收到的设备目录先缓存在这里
        /// </summary>
        public static ConcurrentQueue<Catalog> TmpCatalogs
        {
            get => _tmpCatalogs;
            set => _tmpCatalogs = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 收到历史文件目录时先缓存在这里
        /// </summary>
        public static ConcurrentQueue<RecordInfoEx> TmpRecItems
        {
            get => _tmpRecItems;
            set => _tmpRecItems = value ?? throw new ArgumentNullException(nameof(value));
        }


        /// <summary>
        /// 初始化一SipServerConfig
        /// </summary>
        /// <returns></returns>
        private static SipServerConfig InitSipServerConfig(out ResponseStruct rs)
        {
            try
            {
                SipServerConfig sipServerConfig = null!;
                SystemInfo systemInfo = new SystemInfo();
                string macAddr = "";
                var sys = systemInfo.GetSystemInfoObject();
                int i = 0;
                while ((sys == null || sys.NetWorkStat == null) && i < 50)
                {
                    i++;
                    Thread.Sleep(20);
                }

                if (sys != null && sys.NetWorkStat != null)
                {
                    macAddr = sys.NetWorkStat.Mac;
                    systemInfo.Dispose();
                }

                if (string.IsNullOrEmpty(macAddr))
                {
                    rs = new ResponseStruct()
                    {
                        Code = ErrorNumber.Sys_GetMacAddressExcept,
                        Message = ErrorMessage.ErrorDic![ErrorNumber.Sys_GetMacAddressExcept],
                    };
                    return null!; //mac 地址没找到了，报错出去
                }

                IPInfo ipInfo = UtilsHelper.GetIpAddressByMacAddress(macAddr, true);
                if (!string.IsNullOrEmpty(ipInfo.IpV4))
                {
                    sipServerConfig = new SipServerConfig();
                    sipServerConfig.Authentication = true;
                    sipServerConfig.SipUsername = "admin";
                    sipServerConfig.SipPassword = "123#@!qwe";
                    sipServerConfig.GbVersion = "GB-2016";
                    sipServerConfig.MsgProtocol = "TCP"; //使用TCP可以完美支持tcp信令
                    sipServerConfig.SipPort = 5060;
                    sipServerConfig.IpV6Enable = !string.IsNullOrEmpty(ipInfo.IpV6);
                    if (sipServerConfig.IpV6Enable)
                    {
                        sipServerConfig.SipIpV6Address = ipInfo.IpV6;
                    }

                    sipServerConfig.SipIpAddress = ipInfo.IpV4;
                    sipServerConfig.KeepAliveInterval = 5;
                    sipServerConfig.KeepAliveLostNumber = 2;
                    /*SipDeviceID 20位编码规则
                    *1-2省级 33 浙江省
                    *3-4市级 02 宁波市
                    *5-6区级 00 宁波市区
                    *7-8村级 00 宁波市区
                    *9-10行业 02 社会治安内部接入
                    *11-13设备类型 118 NVR
                    *14 网络类型 0 监控专用网
                    *15-20 设备序号 000001 1号设备 
                    */
                    sipServerConfig.ServerSipDeviceId = "33020000021180000001";
                    if (sipServerConfig.NoAuthenticationRequireds == null)
                    {
                        sipServerConfig.NoAuthenticationRequireds = new List<NoAuthenticationRequired>();
                    }

                    sipServerConfig.NoAuthenticationRequireds.Add(new NoAuthenticationRequired()
                    {
                        DeviceId = sipServerConfig.ServerSipDeviceId,
                        IpV4Address = sipServerConfig.SipIpAddress,
                        IpV6Address = sipServerConfig.SipIpV6Address,
                    });
                    sipServerConfig.Realm = sipServerConfig.ServerSipDeviceId.Substring(0, 10);
                    rs = new ResponseStruct()
                    {
                        Code = ErrorNumber.None,
                        Message = ErrorMessage.ErrorDic![ErrorNumber.None],
                    };
                    return sipServerConfig;
                }
            }
            catch (Exception ex)
            {
                rs = new ResponseStruct()
                {
                    Code = ErrorNumber.Sys_GetMacAddressExcept,
                    Message = ErrorMessage.ErrorDic![ErrorNumber.Sys_GetMacAddressExcept],
                    ExceptMessage = ex.Message,
                    ExceptStackTrace = ex.StackTrace,
                };
                return null!;
            }

            rs = new ResponseStruct()
            {
                Code = ErrorNumber.Other,
                Message = ErrorMessage.ErrorDic![ErrorNumber.Other],
            };
            return null!;
        }

        /// <summary>
        /// 通过通道id获取通道类型
        /// </summary>
        /// <param name="sipChannelId"></param>
        /// <returns></returns>
        public static SipChannelType GetSipChannelType(string sipChannelId)
        {
            if (sipChannelId.Trim().Length != 20)
            {
                return SipChannelType.Unknow;
            }

            int extId = int.Parse(sipChannelId.Substring(10, 3));
            if (extId == 131 || extId == 132 || extId == 137 || extId == 138 || extId == 139)
            {
                return SipChannelType.VideoChannel;
            }

            if (extId == 135 || extId == 205)
            {
                return SipChannelType.AlarmChannel;
            }

            if (extId == 137)
            {
                return SipChannelType.AudioChannel;
            }

            if (extId >= 140 && extId <= 199)
            {
                return SipChannelType.VideoChannel;
            }
            if (extId >= 119 && extId <= 129)
            {
                return SipChannelType.VideoChannel;
            }

            return SipChannelType.OtherChannel;
        }

        /// <summary>
        /// 返加0说明文件存在并正确加载
        /// 返回1说明文件不存在已新建并加载
        /// 返回-1说明文件创建或读取异常失败
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static int ReadSipServerConfig(out ResponseStruct rs)
        {
            if (!File.Exists(_sipServerConfigPath))
            {
                var config = InitSipServerConfig(out rs);
                if (config != null && rs.Code.Equals(ErrorNumber.None))
                {
                    _sipServerConfig = config;
                    if (UtilsHelper.WriteJsonConfig(_sipServerConfigPath, _sipServerConfig))
                    {
                        rs = new ResponseStruct()
                        {
                            Code = ErrorNumber.None,
                            Message = ErrorMessage.ErrorDic![ErrorNumber.None],
                        };
                        return 1;
                    }

                    rs = new ResponseStruct()
                    {
                        Code = ErrorNumber.Sys_JsonWriteExcept,
                        Message = ErrorMessage.ErrorDic![ErrorNumber.Sys_JsonWriteExcept],
                    };
                    return -1;
                }
            }
            else
            {
                try
                {
                    _sipServerConfig =
                        (UtilsHelper.ReadJsonConfig<SipServerConfig>(_sipServerConfigPath) as SipServerConfig)!;
                    if (_sipServerConfig != null)
                    {
                        rs = new ResponseStruct()
                        {
                            Code = ErrorNumber.None,
                            Message = ErrorMessage.ErrorDic![ErrorNumber.None],
                        };

                        return 0;
                    }

                    rs = new ResponseStruct()
                    {
                        Code = ErrorNumber.Sys_JsonReadExcept,
                        Message = ErrorMessage.ErrorDic![ErrorNumber.Sys_JsonReadExcept],
                    };
                    return -1;
                }
                catch (Exception ex)
                {
                    rs = new ResponseStruct()
                    {
                        Code = ErrorNumber.Other,
                        Message = ErrorMessage.ErrorDic![ErrorNumber.Other],
                        ExceptMessage = ex.Message,
                        ExceptStackTrace = ex.StackTrace,
                    };
                    return -1;
                }
            }

            rs = new ResponseStruct()
            {
                Code = ErrorNumber.Other,
                Message = ErrorMessage.ErrorDic![ErrorNumber.Other],
            };
            return 1;
        }

        public static bool DBConfigToFile()
        {
            try
            {
                var config = ORMHelper.Db.Select<LinCms.Core.Entities.SysBasicConfig>().First();
                if (config == null)
                {
                    return false;
                }
                SipServerConfig.SipIpAddress = config.GatewayPublicIp;
                SipServerConfig.ListenIp = config.GatewayIp;
                SipServerConfig.ServerSipDeviceId = config.GatewayCode;
                SipServerConfig.SipPort = (ushort)config.SignalPort;
                var advconfig = ORMHelper.Db.Select<LinCms.Core.Entities.SysAdvancedConfig>().First();
                if (advconfig != null)
                {
                    if (advconfig.AuthEnable == 0)
                    {
                        SipServerConfig.Authentication = false;
                    }
                    else
                    {
                        SipServerConfig.Authentication = true;
                    }

                }
            }
            catch (Exception ex)
            {
                GCommon.Logger.Warn("dbconfigtofile fail: " +ex.Message);
            }
            

            //if (UtilsHelper.WriteJsonConfig(_sipServerConfigPath, _sipServerConfig))
            //{
            //    return true;
            //}
            return false;
        }
    }
}