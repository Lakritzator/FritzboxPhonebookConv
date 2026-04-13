using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FritzboxPhonebookConv.Models;

namespace FritzboxPhonebookConv.Services
{
    /// <summary>
    /// Communicates with a Fritz.Box router via TR-064 (SOAP over HTTP) to retrieve
    /// phonebook information and download phonebook XML files.
    /// Authentication is performed via HTTP Digest (handled automatically by
    /// <see cref="HttpClientHandler"/> when the router issues a 401 challenge).
    /// </summary>
    public class FritzBoxService : IDisposable
    {
        private const string ContactServicePath = "/upnp/control/x_contact";
        private const string ContactServiceType = "urn:dslforum-org:service:X_AVM-DE_OnTel:1";

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        public FritzBoxService(string host, int port, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host must not be empty.", nameof(host));

            _baseUrl = $"http://{host}:{port}";

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty),
                PreAuthenticate = false,
                UseDefaultCredentials = false,
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
        }

        /// <summary>
        /// Retrieves all phonebooks configured on the Fritz.Box.
        /// </summary>
        public async Task<List<Phonebook>> GetPhonebooksAsync()
        {
            string responseXml = await SendSoapRequestAsync(
                ContactServicePath,
                ContactServiceType,
                "GetPhonebookList",
                string.Empty).ConfigureAwait(false);

            XDocument doc = XDocument.Parse(responseXml);
            string idList = doc.Descendants("NewPhonebookList").FirstOrDefault()?.Value ?? string.Empty;

            if (string.IsNullOrWhiteSpace(idList))
                return new List<Phonebook>();

            var phonebooks = new List<Phonebook>();
            foreach (string idStr in idList.Split(','))
            {
                if (!int.TryParse(idStr.Trim(), out int id))
                    continue;

                Phonebook pb = await GetPhonebookInfoAsync(id).ConfigureAwait(false);
                if (pb != null)
                    phonebooks.Add(pb);
            }

            return phonebooks;
        }

        private async Task<Phonebook> GetPhonebookInfoAsync(int id)
        {
            string responseXml = await SendSoapRequestAsync(
                ContactServicePath,
                ContactServiceType,
                "GetPhonebook",
                $"<NewPhonebookID>{id}</NewPhonebookID>").ConfigureAwait(false);

            XDocument doc = XDocument.Parse(responseXml);

            string name = doc.Descendants("NewPhonebookName").FirstOrDefault()?.Value
                          ?? $"Phonebook {id}";
            string url = doc.Descendants("NewPhonebookURL").FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(url))
                return null;

            return new Phonebook { Id = id, Name = name, Url = url };
        }

        /// <summary>
        /// Downloads the raw phonebook XML from the URL returned by GetPhonebook.
        /// </summary>
        public async Task<string> DownloadPhonebookXmlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Phonebook URL must not be empty.", nameof(url));

            HttpResponseMessage response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await ReadContentAsStringAsync(response.Content).ConfigureAwait(false);
        }

        private async Task<string> SendSoapRequestAsync(
            string servicePath,
            string serviceType,
            string action,
            string bodyContent)
        {
            string soapEnvelope =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                "<s:Body>" +
                $"<u:{action} xmlns:u=\"{serviceType}\">" +
                bodyContent +
                $"</u:{action}>" +
                "</s:Body>" +
                "</s:Envelope>";

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

            using (var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + servicePath))
            {
                request.Content = content;
                request.Headers.Add("SOAPAction", $"\"{serviceType}#{action}\"");

                HttpResponseMessage response = await _httpClient.SendAsync(request).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await ReadContentAsStringAsync(response.Content).ConfigureAwait(false);
                    throw new InvalidOperationException(
                        $"SOAP call '{action}' failed ({(int)response.StatusCode} {response.ReasonPhrase}): {errorBody}");
                }

                return await ReadContentAsStringAsync(response.Content).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads HTTP content as a string, tolerating invalid or quoted charset values in the
        /// Content-Type header (e.g. <c>charset="utf-8"</c>) that would otherwise cause
        /// <see cref="HttpContent.ReadAsStringAsync"/> to throw. Falls back to UTF-8.
        /// </summary>
        private static async Task<string> ReadContentAsStringAsync(HttpContent content)
        {
            byte[] bytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);

            Encoding encoding = Encoding.UTF8;
            string charSet = content.Headers?.ContentType?.CharSet;
            if (!string.IsNullOrWhiteSpace(charSet))
            {
                // Strip surrounding quotes that some devices (e.g. Fritz!Box) include.
                charSet = charSet.Trim().Trim('"', '\'');
                try
                {
                    encoding = Encoding.GetEncoding(charSet);
                }
                catch (ArgumentException)
                {
                    // Invalid charset — fall back to UTF-8.
                }
            }

            return encoding.GetString(bytes);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }
    }
}
