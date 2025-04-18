using LibCommon;
using LibCommon.Structs.DBModels;
using LinCms.Core.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace AKStreamWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            var tmpRet = UtilsHelper.GetMainParams(args);
            if (tmpRet != null && tmpRet.Count > 0)
            {
                foreach (var tmp in tmpRet)
                {
                    if (tmp.Key.ToUpper().Equals("-C"))
                    {
                        GCommon.OutConfigPath = tmp.Value;
                    }

                    if (tmp.Key.ToUpper().Equals("-L"))
                    {
                        GCommon.OutLogPath = tmp.Value;
                    }
                }
            }

            if (!string.IsNullOrEmpty(GCommon.OutLogPath))
            {
                if (!GCommon.OutLogPath.Trim().EndsWith('/'))
                {
                    GCommon.OutLogPath += "/";
                }
            }

            GCommon.InitLogger();
            Common.Init();

            //biz_licence licence = new biz_licence();
            //licence.max_device_number = 1;
            //licence.max_transcode_number = 1;
            //ORMHelper.Db.Insert(licence).ExecuteAffrows();

            //biz_system sys = new biz_system();
            //sys.thekey = "1";
            //ORMHelper.Db.Insert(sys).ExecuteAffrows();

            //biz_transcode tr = new biz_transcode();
            //ORMHelper.Db.Insert(tr).ExecuteAffrows();

            //SysBasicConfig sysBasicConfig = new SysBasicConfig();
            //ORMHelper.Db.Insert(sysBasicConfig).ExecuteAffrows();
            //var result = ORMHelper.Db.Select<SysBasicConfig>().ToListAsync();

            CreateHostBuilder(args).Build().Run();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GCommon.Logger.Error("CurrentDomain_UnhandledException : " + e.ExceptionObject.ToString());
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    if (string.IsNullOrEmpty(Common.AkStreamWebConfig.ListenIp))
                    {
                        webBuilder.UseStartup<Startup>().UseUrls($"http://*:{Common.AkStreamWebConfig.WebApiPort}");
                    }
                    else
                    {
                        var url = $"http://{Common.AkStreamWebConfig.ListenIp}:{Common.AkStreamWebConfig.WebApiPort}";
                        webBuilder.UseStartup<Startup>().UseUrls(url);
                    }
                });
    }
}