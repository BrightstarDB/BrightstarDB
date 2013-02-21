using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Writing;

namespace BrightstarDB.SdShare
{
    public class OdbcCollectionProvider : CollectionProviderBase
    {
        private string _dataSourceConnectionString;        
        private readonly List<ResourcePublishingDefinition> _publishingDefinitions;

        public OdbcCollectionProvider()
        {
            _publishingDefinitions = new List<ResourcePublishingDefinition>();
        }

        public IEnumerable<ResourcePublishingDefinition> PublishingDefinitions
        {
            get { return _publishingDefinitions; }
        }

        public string DsConnection
        {
            get { return _dataSourceConnectionString; }
        }

        #region Overrides of CollectionProviderBase

        public override void Initialize(XElement configRoot)
        {
            // name
            var nameAttribute = configRoot.Attribute("name");
            if (nameAttribute == null) throw new Exception("Missing 'name' attribute on element CollectionProvider");
            Name = nameAttribute.Value;

            var identityAttribute = configRoot.Attribute("identifier");
            if (identityAttribute == null) throw new Exception("Missing 'identifier' attribute on element CollectionProvider");
            Identity = identityAttribute.Value;

            // description
            var descriptionAttribute = configRoot.Attribute("description");
            if (descriptionAttribute  == null)
            {
                // log warning
                Logging.LogWarning(0, "No description element for collection {0}", Name);
            } else
            {
                Description = descriptionAttribute.Value;                
            }

            // dsn
            var dsnElement = configRoot.Elements("DsnConnection").FirstOrDefault();
            if (dsnElement == null)
            {
                throw new Exception("Missing 'DsnConnection' element as child of CollectionProvider");
            }
            _dataSourceConnectionString = dsnElement.Value.Trim();

            // publishing definitions
            foreach (var xElement in configRoot.Descendants("Definition"))
            {
                // add each definition to the collection
                var def = new ResourcePublishingDefinition
                              {
                                  ResourcePrefix = new Uri(GetElementValue(xElement, "ResourcePrefix")),
                                  UriTemplate = new UriTemplate(GetElementValue(xElement, "UriTemplate"))
                              };

                if (DoesElementExist(xElement, "SuppressFragmentsFeed"))
                {
                    def.SuppressFragmentsFeed = bool.Parse(GetElementValue(xElement, "SuppressFragmentsFeed"));    
                }

                if (DoesElementExist(xElement, "FragmentsQuery"))
                {
                    def.FragmentsQuery = GetElementValue(xElement, "FragmentsQuery");
                    if (DoesElementExist(xElement, "SourceDataInLocalTime"))
                        def.SourceDataInLocalTime = bool.Parse(GetElementValue(xElement, "SourceDataInLocalTime"));                        
                } else
                {
                    def.NoTimeStampInData = true;
                    def.HashValueTable = GetElementValue(xElement, "HashValueTable");
                    // Axel was here
                    if (DoesElementExist(xElement, "HashValueFileName"))
                        def.HashValueFileName = GetElementValue(xElement, "HashValueFileName");
                    def.EntityIdColumn = GetElementValue(xElement, "EntityIdColumn");
                    def.HashValueKeyColumns = GetElementValues(xElement, "HashValueKeyColumn");
                    def.ValueCheckInterval = GetElementValue(xElement, "UpdateFrequency");
                }

                if (DoesElementExist(xElement, "EncodeIdForResourceId"))
                {
                    def.EncodeIdForResourceId = bool.Parse(GetElementValue(xElement, "EncodeIdForResourceId"));
                }

                // get FragmentGenerationDefinitions
                foreach (var fragmentGenerationDefinition in xElement.Descendants("FragmentGenerationDefinition"))
                {
                    var fragDef = new FragmentGenerationDefinition
                                      {
                                          SnapshotQuery = GetElementValue(fragmentGenerationDefinition, "SnapshotQuery"),
                                          FragmentQuery = GetElementValue(fragmentGenerationDefinition, "FragmentQuery")
                                      };

                    // get generic exclude columns
                    var rdfTemplatesElem = xElement.Descendants("RdfTemplates").FirstOrDefault();
                    if (rdfTemplatesElem.Attribute("genericExcludes") != null)
                    {
                        fragDef.GenericTemplateExcludeColumns = new List<string>(rdfTemplatesElem.Attribute("genericExcludes").Value.Split(','));
                    }

                    // get lines
                    var patternLines = new List<string>();
                    foreach (var lineElement in fragmentGenerationDefinition.Descendants("li"))
                    {
                        patternLines.Add(lineElement.Value.Replace("{{", "<").Replace("}}", ">"));
                    }
                    fragDef.RdfTemplateLines = patternLines;
                    def.FragmentGenerationDefinitions.Add(fragDef);
                }

                _publishingDefinitions.Add(def);
            }

            RawConfig = configRoot.ToString();
            IsEnabled = true;
        }

        private static string GetElementValue(XElement parent, string name)
        {
            var elem = parent.Elements(name).FirstOrDefault();
            if (elem == null) throw new Exception("Missing element " + name);
            return elem.Value.Replace("&lt;", "<");
        }

        private static List<string> GetElementValues(XElement parent, string name)
        {
            return parent.Elements(name).Select(e => e.Value).ToList();
        }

        private static bool DoesElementExist(XElement parent, string name)
        {
            var elem = parent.Elements(name).FirstOrDefault();
            return elem != null;
        }

        public override IEnumerable<ISnapshot> GetSnapshots()
        {
            return new List<ISnapshot>
                       {new Snapshot {Id = "Everything", Name = "Complete View", PublishedDate = DateTime.UtcNow}};
        }

        public override IEnumerable<IFragment> GetFragments(DateTime since, DateTime before)
        {           
            foreach (var definition in _publishingDefinitions)
            {
                if (definition.SuppressFragmentsFeed) continue;
                if (definition.DataSourceManager != null)
                {
                    foreach (var info in definition.DataSourceManager.ListLastUpdated(since))
                    {
                        var psi = definition.UriTemplate.BindByPosition(definition.ResourcePrefix, Uri.EscapeDataString(info.EntityId));
                        yield return
                            new Fragment { PublishDate = 
                                           new DateTime(info.LastUpdated.Year, 
                                                        info.LastUpdated.Month, 
                                                        info.LastUpdated.Day, 
                                                        info.LastUpdated.Hour, 
                                                        info.LastUpdated.Minute, 
                                                        info.LastUpdated.Second, 
                                                        0, DateTimeKind.Utc), 
                                                        ResourceId = Uri.EscapeUriString(psi.AbsoluteUri), 
                                                        ResourceName = info.EntityId, 
                                                        ResourceUri = psi.AbsoluteUri };
                    }
                }
                else
                {
                    if (since.Equals(DateTime.MinValue)) since = SqlDateTime.MinValue.Value;
                    if (before.Equals(DateTime.MaxValue)) before = SqlDateTime.MaxValue.Value;
                    
                    var queryParams = new List<object> {definition.SourceDataInLocalTime ? since.ToLocalTime() : since};

                    var query = definition.FragmentsQuery;
                    query = query.Replace("[[since]]", "?");
                    
                    if (query.Contains("[[before]]"))
                    {
                        query = query.Replace("[[before]]", "?");
                        queryParams.Add(before);                        
                    }

                    var data = ExecuteQuery(_dataSourceConnectionString, query, queryParams.ToArray());

                    if (!(data.Columns.Contains("id") && data.Columns.Contains("name")))
                    {
                        throw new Exception("One of the required columns (id, name) was not present.");
                    }

                    foreach (DataRow row in data.Rows)
                    {
                        var fragment = new Fragment();
                        var rowId =  row["id"].ToString().Trim();
                        rowId = Uri.EscapeDataString(rowId);
                        var uri = definition.UriTemplate.BindByPosition(definition.ResourcePrefix, rowId);
                        if (data.Columns.Contains("updated"))
                        {
                            var value = (DateTime) row["updated"];
                            var localDateTime = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, 0, DateTimeKind.Local);
                            fragment.PublishDate = localDateTime.ToUniversalTime();
                        }
                        else
                        {
                            fragment.PublishDate = DateTime.UtcNow;
                        }
                        fragment.ResourceName = row["name"].ToString();
                        fragment.ResourceUri = uri.AbsoluteUri;
                        fragment.ResourceId = Uri.EscapeUriString(uri.AbsoluteUri);

                        yield return fragment;
                    }
                }
            }
        }

        public static DataTable ExecuteQuery(string dsnConnection, string query, params object[] queryParams)
        {
            try
            {
                using (var connection = new OdbcConnection(dsnConnection))
                {
                    connection.Open();
                    var queryCommand = connection.CreateCommand();
                    queryCommand.CommandText = query;
                    queryCommand.CommandTimeout = 1;

                    var i = 0;
                    foreach (var queryParam in queryParams)
                    {
                        queryCommand.Parameters.Add("@p" + i, OdbcType.DateTime).Value = queryParam;
                        i++;
                    }

                    var dataSet = new DataSet();
                    var da = new OdbcDataAdapter(queryCommand);
                    da.Fill(dataSet);
                    return dataSet.Tables[0];
                }
            } catch(Exception ex)
            {
                Logging.LogError(1, "Error executing query {0} on connection {1} message is {2}", query, dsnConnection, ex.Message);
                throw;
            }
        }

        public override Stream GetFragment(string id, string contentType)
        {
                // need to see which definition we match
                ResourcePublishingDefinition definition = null;
                UriTemplateMatch match = null;
                foreach (var resourcePublishingDefinition in _publishingDefinitions)
                {
                    var newuri = new Uri(id);
                    match = resourcePublishingDefinition.UriTemplate.Match(resourcePublishingDefinition.ResourcePrefix, newuri);
                    if (match != null)
                    {
                        definition = resourcePublishingDefinition;
                        break;
                    }
                }

                if (definition == null) { throw new Exception("Unable to find matching definition for uri " + id); }

                var sb = new StringBuilder();
                foreach (var generationDefinition in definition.FragmentGenerationDefinitions)
                {
                    try
                    {
                        var data = ExecuteQuery(_dataSourceConnectionString, generationDefinition.FragmentQuery.Replace("[[id]]", match.BoundVariables["id"]));
                        foreach (DataRow row in data.Rows)
                        {
                            var dra = new DbDataRow(row);
                            foreach (var line in generationDefinition.RdfTemplateLines)
                            {
                                var linePattern = new NTripleLinePattern(line);
                                linePattern.GenerateNTriples(sb, dra, generationDefinition.GenericTemplateExcludeColumns, contentType.Equals("xml"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(1, "Error processing definition {0} {1} {2} {3}", ex.Message, generationDefinition.SnapshotQuery, _dataSourceConnectionString, ex.StackTrace);
                    }
                }

                try
                {
                    var g = new Graph();
                    var parser = new NTriplesParser(TokenQueueMode.SynchronousBufferDuringParsing);
                    var triplesStr = sb.ToString();
                    parser.Load(g, new StringReader(triplesStr));

                    if (contentType.Equals("xml"))
                    {
                        var ms = new MemoryStream();
                        var sw = new StreamWriter(ms, Encoding.UTF8);
                        var rdfxmlwriter = new RdfXmlWriter();

                        var strw = new System.IO.StringWriter();
                        rdfxmlwriter.Save(g, strw);
                        var data = strw.ToString();

                        data = data.Replace("~~~2B~~~", "%2B");
                        data = data.Replace("~~~SLASH~~~", "%2F");
                        data = data.Replace("utf-16", "utf-8");                                             
                        sw.Write(data);
                        sw.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        return ms;
                    }
                    else
                    {
                        var ms = new MemoryStream();
                        var sw = new StreamWriter(ms);
                        sw.Write(triplesStr);
                        sw.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        return ms;
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogError(1, "Error getting fragment {0} {1}", ex.Message, ex.StackTrace);
                    throw;
                }
        }

        

        public override Stream GetSnapshot(string id, string contentType)
        {
            Logging.LogInfo("GetSnapshot for " + id);
            var tmpFileName = ConfigurationReader.Configuration.HashValueStorageLocation + Path.DirectorySeparatorChar + Guid.NewGuid();
            Logging.LogInfo("Writing to file " + tmpFileName);

            try {
                using (var fs = new FileStream(tmpFileName, FileMode.Create))
                {
                    var sb = new StringBuilder();

                    foreach (var definition in _publishingDefinitions)
                    {
                        foreach (var generationDefinition in definition.FragmentGenerationDefinitions)
                        {
                            try
                            {
                                using (var connection = new OdbcConnection(_dataSourceConnectionString))
                                {
                                    connection.Open();
                                    var odbcCommand = new OdbcCommand(generationDefinition.SnapshotQuery)
                                                          {
                                                              Connection = connection,
                                                              CommandTimeout = 0
                                                          };
                                    var dr = odbcCommand.ExecuteReader();
                                    var schema = dr.GetSchemaTable();
                                    var columnNames = (from DataRow row in schema.Rows select row[0].ToString().ToLower()).ToList();
                                    var flushCount = 0;
                                    var drAdaptor = new DbReaderDataRow(dr, columnNames);
                                    while (dr.Read())
                                    {
                                        try
                                        {
                                            flushCount++;
                                            foreach (var line in generationDefinition.RdfTemplateLines)
                                            {
                                                var pattern = new NTripleLinePattern(line);
                                                pattern.GenerateNTriples(sb, drAdaptor, generationDefinition.GenericTemplateExcludeColumns, false);
                                            }
                                        }
                                        catch (Exception dataex)
                                        {
                                            Logging.LogError(1, "Error Processing Data Line in " + generationDefinition.SnapshotQuery + " : " + dataex.Message);
                                        }

                                        if (flushCount >= 1000)
                                        {
                                            try
                                            {
                                                var sw = new StreamWriter(fs);
                                                sw.Write(sb.ToString());
                                                sw.Flush();
                                                sb = new StringBuilder();
                                            }
                                            catch (Exception ex)
                                            {
                                                var msg = ex.Message;
                                                Logging.LogError(1, "Error exporting triples " + msg + " " + ex.StackTrace);
                                            }
                                        }
                                    }

                                    if (flushCount >= 0)
                                    {
                                        try
                                        {
                                            var sw = new StreamWriter(fs);
                                            sw.Write(sb.ToString());
                                            sw.Flush();
                                            sb = new StringBuilder();
                                        }
                                        catch (Exception ex)
                                        {
                                            var msg = ex.Message;
                                            Logging.LogError(1, "Error exporting triples " + msg + " " + ex.StackTrace);
                                        }
                                    }

                                    dr.Close();
                                    connection.Close();
                                }
                            }
                            catch (Exception ext)
                            {
                                Logging.LogError(1, "Error processing definition {0} {1} {2} {3}", ext.Message, generationDefinition.SnapshotQuery, _dataSourceConnectionString, ext.StackTrace);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(1, "Exception getting snapshot " + ex.Message + " " + ex.StackTrace);
            }
            return new FileStream(tmpFileName, FileMode.Open);
        }

        public override Stream GetSample(string mimeType)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
