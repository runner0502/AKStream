// See https://aka.ms/new-console-template for more information
using QLicenseCore;

MyLicense _lic = null;
string _msg = string.Empty;
LicenseStatus _status = LicenseStatus.UNDEFINED;
_lic = (MyLicense)LicenseHandler.ParseLicenseFromBASE64String(
                   typeof(MyLicense),
                   File.ReadAllText("license"),
                   null,
                   out _status,
                   out _msg);
Console.WriteLine("Hello, World!");

