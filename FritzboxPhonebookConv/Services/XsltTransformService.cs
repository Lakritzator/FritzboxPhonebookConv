using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace FritzboxPhonebookConv.Services
{
    /// <summary>
    /// Applies an XSLT stylesheet to an XML string and saves the result.
    /// Supports both XML and text (e.g. CSV) output methods as declared in the XSLT.
    /// </summary>
    public static class XsltTransformService
    {
        /// <summary>
        /// Transforms <paramref name="inputXml"/> using the XSLT file at
        /// <paramref name="xsltFilePath"/> and returns the raw output bytes.
        /// The encoding is determined by the XSLT's &lt;xsl:output&gt; declaration.
        /// </summary>
        public static byte[] TransformToBytes(string inputXml, string xsltFilePath)
        {
            var xslt = new XslCompiledTransform();
            xslt.Load(xsltFilePath);

            byte[] inputBytes = Encoding.UTF8.GetBytes(inputXml);
            using (var inputStream = new MemoryStream(inputBytes))
            using (var xmlReader = XmlReader.Create(inputStream))
            using (var outputStream = new MemoryStream())
            {
                xslt.Transform(xmlReader, null, outputStream);
                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Saves the byte content returned by <see cref="TransformToBytes"/> to
        /// <paramref name="outputPath"/>, preserving the encoding declared in the XSLT.
        /// </summary>
        public static void SaveToFile(byte[] content, string outputPath)
        {
            File.WriteAllBytes(outputPath, content);
        }
    }
}
