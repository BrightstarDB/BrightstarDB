using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterManager
{
    public class ClusterConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region Implementation of IConfigurationSectionHandler

        /// <summary>
        /// Creates a configuration section handler.
        /// </summary>
        /// <returns>
        /// The created section handler object.
        /// </returns>
        /// <param name="parent">Parent object.</param><param name="configContext">Configuration context object.</param><param name="section">Section XML node.</param><filterpriority>2</filterpriority>
        public object Create(object parent, object configContext, XmlNode section)
        {
            var nodes = new List<NodeConfiguration>();
            var nodesList = section.SelectSingleNode("nodes");
            if (nodesList != null)
            {
                foreach(var addNode in nodesList.SelectNodes("add").OfType<XmlNode>())
                {
                    nodes.Add(ParseAddNode(addNode));
                }
            }
            var masterConfigurationNode = section.SelectSingleNode("masterConfiguration");
            var masterConfiguration = ParseMasterConfiguration(masterConfigurationNode);
            return new ClusterConfiguration
                       {
                           ClusterNodes = nodes,
                           MasterConfiguration = masterConfiguration
                       };
        }

        #endregion

        private NodeConfiguration ParseAddNode(XmlNode addNode)
        {
            if (addNode == null)
            {
                throw new ConfigurationErrorsException("Invalid node list configuration. Expected one or more <add> elements inside the <node> element.");
            }
            if (addNode.NodeType != XmlNodeType.Element)
            {
                throw new ConfigurationErrorsException("Invalid node list configuration. Expected <add> element inside <nodes> element.");
            }
            var hostAttr = addNode.Attributes["host"];
            var managementPortAttr = addNode.Attributes["managementPort"];
            
            if (hostAttr == null) throw new ConfigurationErrorsException("Invalid node configuration. The @host attribute is REQUIRED.");
            if (managementPortAttr == null) throw new ConfigurationErrorsException("Invalid node configuration. The @port attribute is REQUIRED.");
            int portNumber;
            if (!Int32.TryParse(managementPortAttr.Value, out portNumber))
            {
                throw new ConfigurationErrorsException("Invalid node configuration. Could not parse @port attribute value '" + managementPortAttr.Value + "' as an valid TCP port number.");
            }
            return new NodeConfiguration
                       {
                           Host = hostAttr.Value,
                           ManagementPort = GetPortNumber(addNode, "managementPort"),
                           ServiceTcpPort = GetPortNumber(addNode, "tcpServicePort"),
                           ServiceHttpPort = GetPortNumber(addNode, "httpServicePort")
                       };
        }

        private MasterConfiguration ParseMasterConfiguration(XmlNode masterConfigurationNode)
        {
            if (masterConfigurationNode.NodeType != XmlNodeType.Element)
            {
                throw new ConfigurationErrorsException("Invalid cluster configuration. Expected <masterConfiguration> element inside <clusterConfiguration> element.");
            }

            var writeQuorum = masterConfigurationNode.Attributes["writeQuorum"];
            int writeQuorumNumber;
            if (writeQuorum == null) throw new ConfigurationErrorsException("Invalid master configuration. The @writeQuorum attribute is REQUIRED");
            if (!Int32.TryParse(writeQuorum.Value, out writeQuorumNumber))
            {
                throw new ConfigurationErrorsException("Invalid master configuration. The @writeQuorum attribute could not be parsed as a valid integer value.");
            }
            return new MasterConfiguration {WriteQuorum = writeQuorumNumber};
        }

        private int GetPortNumber(XmlNode element, string attrName)
        {
            var portAttr = element.Attributes[attrName];
            if (portAttr == null) throw new ConfigurationErrorsException(String.Format("Invalid node configuration. The @{0} attribute is REQUIRED.", attrName));
            int portNumber;
            if (!Int32.TryParse(portAttr.Value, out portNumber))
            {
                throw new ConfigurationErrorsException(
                    String.Format(
                        "Invalid node configuration. Could not parse @{0} attribute value '{1}' as an valid TCP port number.",
                        attrName, portAttr.Value));
            }
            return portNumber;
        }
    }
}
