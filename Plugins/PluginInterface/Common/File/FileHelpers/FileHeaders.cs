using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace PluginInterface.Common.File
{
    public class FileHeaders
    {


        public static Headers Headers;

        public FileHeaders()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string xmlFileName = Path.Combine(assemblyFolder, "Common", "File", "FileHelpers", "Headers.xml");
            XmlSerializer serializer = new XmlSerializer(typeof(Headers));

            using (FileStream fileStream = new FileStream(xmlFileName, FileMode.Open))
            {
                Headers = (Headers)serializer.Deserialize(fileStream);
            }

        }

        public static byte[] ByteArrayFromString(string str)
        {
            string[] s = str.Split('\\').Skip(1).ToArray();
            byte[] data = new byte[s.Length];
            for (int i = 0; i < data.Length; i++)
                data[i] = byte.Parse(s[i].Replace("x", ""), System.Globalization.NumberStyles.HexNumber);
            return data;
        }
    }
}
