using System.IO;
using Microsoft.Data.OData;
using NetworkedPlanet.Brightstar.OData.Tests.Messages;

namespace NetworkedPlanet.Brightstar.OData.Tests.ClientLibraryTests
{
    class HTTPRequests
    {
        public void BasicGetRequest(string uri, string format, ODataVersion version, ODataVersion maxVersion, string filename)
        {
            HTTPClientRequestMessage message = new HTTPClientRequestMessage(uri);
            message.SetHeader("Accept", format);
            message.SetHeader("DataServiceVersion", version.ToHeaderValue());
            message.SetHeader("MaxDataServiceVersion", maxVersion.ToHeaderValue());

            WriteResponseToFile(message.GetResponse() as HTTPClientResponseMessage, filename);
        }

        public static void WriteResponseToFile(HTTPClientResponseMessage response, string fileName)
        {
            string filePath = @".\out\" + fileName + ".txt";

            var streamTask = response.GetStreamAsync();
            streamTask.Wait();
            using (FileStream output = new FileStream(filePath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(output))
                {
                    writer.WriteLine(response.StatusCode + " " + response.StatusDescription);
                    foreach (var q in response.Headers)
                    {
                        writer.WriteLine(q.Key + ": " + q.Value);
                    }

                    writer.WriteLine();

                    using (StreamReader reader = new StreamReader(streamTask.Result))
                    {
                        writer.Write(reader.ReadToEnd());
                    }
                }
            }
        }
    }
}
