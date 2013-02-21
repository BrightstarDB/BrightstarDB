using System;
using System.IO;
using System.Net.Sockets;
using System.ServiceModel;

namespace BrightstarDB.ClusterNode
{
    public class NodeClient
    {
        private string SendMessage(EndpointAddress endpoint, string message)
        {
            try
            {
                using (var client = new TcpClient(endpoint.Uri.Host, endpoint.Uri.Port))
                {
                    // todo: set a timeout
                    var stream = client.GetStream();
                    var streamWriter = new StreamWriter(stream);
                    streamWriter.WriteLine(message);
                    streamWriter.Flush();

                    // wait for response
                    var reader = new StreamReader(stream);
                    var response = reader.ReadLine();
                    return response;
                }
            }
            catch (Exception ex)
            {
                // todo: log error
                return null;
            }
        }

        public bool CheckIsMaster(EndpointAddress ep)
        {
            try
            {
                var result = SendMessage(ep, "AreYouMaster");
                return result.ToLower().Equals("true");
            } catch(Exception)
            {
                return false;
            }
        }

        public bool Ping(EndpointAddress ep)
        {
            try
            {
                var result = SendMessage(ep, "ping");
                return result.ToLower().Equals("pong");
            } catch(Exception ex)
            {
                // node not available
                return false;
            }
        }

        public bool SuggestMaster(EndpointAddress endpointAddress, EndpointAddress address)
        {
            var result = SendMessage(endpointAddress, "SuggestMasterIs:" + address.Uri.Host + ":" + address.Uri.Port);
            if (result.ToLower().Equals("accepted")) return true;
            return false;
        }
    }
}