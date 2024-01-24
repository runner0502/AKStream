// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var uid = QLicenseCore.LicenseHandler.GenerateUID("281");
using (StreamWriter writer = new StreamWriter("uid"))
{
    writer.Write(uid);
}
