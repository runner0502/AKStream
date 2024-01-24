using System.Security.Cryptography;
using System.Xml.Serialization;

namespace QLicenseCore
{
    public class Class1
    {
        public void CreateKey()
        {
            RSA rsa = RSA.Create();
            RSAParameters rsaKeyInfo = rsa.ExportParameters(true);
            XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
            using (System.IO.StreamWriter writer = new StreamWriter("E:\\rsak"))
            {
                serializer.Serialize(writer, rsaKeyInfo);
            }
        }
    }
}