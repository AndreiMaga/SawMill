using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FIPF.File
{
    [XmlRoot(ElementName = "File")]
    public class File
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "Size")]
        public string Size { get; set; }
        [XmlElement(ElementName = "Header")]
        public string Header { get; set; }
        [XmlElement(ElementName = "Footer")]
        public string Footer { get; set; }
    }

    [XmlRoot(ElementName = "Headers")]
    public class Headers
    {
        [XmlElement(ElementName = "File")]
        public List<File> File { get; set; }
    }
}
