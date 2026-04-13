using System.Xml.Serialization;

namespace FritzboxPhonebookConv.Models
{
    /// <summary>
    /// A named reference to an XSLT file on the local filesystem.
    /// </summary>
    public class XsltProfile
    {
        [XmlAttribute]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute]
        public string FilePath { get; set; } = string.Empty;

        public override string ToString() => Name;
    }
}
