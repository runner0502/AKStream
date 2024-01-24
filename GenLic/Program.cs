// See https://aka.ms/new-console-template for more information
using QLicenseCore;

MyLicense license = new MyLicense();

Console.WriteLine("uid 文件路径：");
string uidFile = Console.ReadLine();
Console.WriteLine("到期时间(格式如 2024-01-01)：");
DateTime expireTime = DateTime.Parse(Console.ReadLine());
Console.WriteLine("最大接入个数：");
int maxDevice = int.Parse(Console.ReadLine());
Console.WriteLine("最大并发个数：");
int maxCon = int.Parse(Console.ReadLine());

license.ExpireDateTime = expireTime;
license.MaxDeviceCount = maxDevice;
license.MaxRunCount = maxCon;


//license.ExpireDateTime = DateTime.Now;
//license.MaxDeviceCount = 4;
//license.MaxRunCount = 7;

license.Type = LicenseTypes.Single;
//var uid = QLicenseCore.LicenseHandler.GenerateUID("281");
var uid = File.ReadAllText(uidFile);
license.UID = uid;

var sinLic = QLicenseCore.LicenseHandler.GenerateLicenseBASE64String(license, null, null);
using (StreamWriter writer = new StreamWriter("license"))
{
    writer.Write(sinLic);
}
