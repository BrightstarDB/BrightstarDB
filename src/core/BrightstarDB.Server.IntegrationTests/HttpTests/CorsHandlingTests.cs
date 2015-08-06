using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace BrightstarDB.Server.IntegrationTests.HttpTests
{
    [TestFixture]
    public class CorsHandlingTests : ClientTestBase
    {
        private string _testFixtureStoreName;
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            StartService();
            _testFixtureStoreName = "CorsHandlingTests_" + DateTime.Now.Ticks;

            var response = Post(new Uri("http://localhost:8090/brightstar"), new Dictionary<string, string>{{"storeName", _testFixtureStoreName}});
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            CloseService();
        }

        [Test]
        public void TestStoreListCors()
        {
            var response = Get(new Uri("http://localhost:8090/brightstar"), new Dictionary<string, string>
            {
                {"Origin", "http://example.com/"},
                {"Accept", "application/json"}
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Access-Control-Allow-Origin"], Is.EqualTo("*"));
        }

        [Test]
        public void TestStore404Cors()
        {
            var response = Get(new Uri(GetStoreUri(_testFixtureStoreName + "_invalid")),
                new Dictionary<string, string>
                {
                    {"Origin", "http://example.com/"},
                    {"Accept", "application/json"}
                });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Headers["Access-Control-Allow-Origin"], Is.EqualTo("*"));
        }

        static HttpWebResponse Post(Uri uri, object data = null, string contentType = null)
        {
            var request = HttpWebRequest.Create(uri);
            request.Method = WebRequestMethods.Http.Post;
            string requestBody = null;
            if (data is string)
            {
                requestBody = (string)data ;
                request.ContentType = contentType ?? "text/plain";
            } else if (data is Dictionary<string, string>)
            {
                var dict = (Dictionary<string, string>) data;
                request.ContentType = "application/x-www-form-urlencoded";
                foreach (var entry in dict)
                {
                    if (entry.Value != null)
                    {
                        requestBody += String.Format("{0}={1}&", Uri.EscapeDataString(entry.Key),
                            Uri.EscapeDataString(entry.Value));
                    }
                    else
                    {
                        requestBody += String.Format("{0}&", Uri.EscapeDataString(entry.Key));
                    }
                }
            }
            request.Headers[HttpRequestHeader.ContentEncoding] = "UTF-8";
            if (requestBody != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bytes.Length;
                using (var os = request.GetRequestStream())
                {
                    os.Write(bytes, 0, bytes.Length);
                    os.Close();
                }
            }
            else
            {
                request.ContentLength = 0;
            }
            return request.GetResponse() as HttpWebResponse;
        }

        private static HttpWebResponse Get(Uri uri, Dictionary<string, string> headers = null)
        {
            var request = HttpWebRequest.Create(uri) as HttpWebRequest;
            if (headers != null)
            {
                foreach (var entry in headers)
                {
                    switch (entry.Key.ToLowerInvariant())
                    {
                        case "accept":
                            request.Accept = entry.Value;
                            break;
                        default:
                            request.Headers.Set(entry.Key, entry.Value);
                            break;
                    }
                }
            }
            try
            {
                return request.GetResponse() as HttpWebResponse;
            }
            catch (WebException wex)
            {
                var response = wex.Response as HttpWebResponse;
                if (response != null) return response;
                throw;
            }
        }
    }
}
