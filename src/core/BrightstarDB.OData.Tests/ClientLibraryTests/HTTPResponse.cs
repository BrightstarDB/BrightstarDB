using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using NetworkedPlanet.Brightstar.OData.Tests.Messages;

namespace NetworkedPlanet.Brightstar.OData.Tests.ClientLibraryTests
{
    class HTTPResponse
    {
        private IndentedTextWriter writer;

        public IEdmModel GetMetadata(string uri)
        {
            HTTPClientRequestMessage message = new HTTPClientRequestMessage(uri);
            message.SetHeader("Accept", "application/xml");
            message.SetHeader("DataServiceVersion", ODataVersion.V1.ToHeaderValue());
            message.SetHeader("MaxDataServiceVersion", ODataVersion.V3.ToHeaderValue());

            using (ODataMessageReader messageReader = new ODataMessageReader(message.GetResponse()))
            {
                return messageReader.ReadMetadataDocument();

            }
        }

        //public void GetResponse(string uri, string format, ODataVersion version, ODataVersion maxVersion, IEdmModel model, string fileName)
        //{
        //    HTTPClientRequestMessage message = new HTTPClientRequestMessage(uri);
        //    message.SetHeader("Accept", format);
        //    message.SetHeader("DataServiceVersion", version.ToHeaderValue());
        //    message.SetHeader("MaxDataServiceVersion", maxVersion.ToHeaderValue());

        //    string filePath = @".\out\" + fileName + ".txt";
        //    using (StreamWriter outputWriter = new StreamWriter(filePath))
        //    {
        //        this.writer = new IndentedTextWriter(outputWriter, "  ");

        //        using (ODataMessageReader messageReader = new ODataMessageReader(message.GetResponse(), new ODataMessageReaderSettings(), model))
        //        {
        //            ODataReader reader = messageReader.CreateODataFeedReader();
        //            this.ReadAndOutputEntryOrFeed(reader);
        //        }
        //    }
        //}

        public ODataMessageReader GetResponse(string uri, string format, ODataVersion version, ODataVersion maxVersion, IEdmModel model)
        {
            HTTPClientRequestMessage message = new HTTPClientRequestMessage(uri);
            message.SetHeader("Accept", format);
            message.SetHeader("DataServiceVersion", version.ToHeaderValue());
            message.SetHeader("MaxDataServiceVersion", maxVersion.ToHeaderValue());
            return new ODataMessageReader(message.GetResponse(), new ODataMessageReaderSettings(), model);
        }

        private void ReadAndOutputEntryOrFeed(ODataReader reader)
        {
            while (reader.Read())
            {
                switch (reader.State)
                {
                    case ODataReaderState.FeedStart:
                        {
                            ODataFeed feed = (ODataFeed)reader.Item;
                            this.writer.WriteLine("ODataFeed:");
                            this.writer.Indent++;
                        }

                        break;

                    case ODataReaderState.FeedEnd:
                        {
                            ODataFeed feed = (ODataFeed)reader.Item;
                            if (feed.Count != null)
                            {
                                this.writer.WriteLine("Count: " + feed.Count.ToString());
                            }
                            if (feed.NextPageLink != null)
                            {
                                this.writer.WriteLine("NextPageLink: " + feed.NextPageLink.AbsoluteUri);
                            }

                            this.writer.Indent--;
                        }

                        break;

                    case ODataReaderState.EntryStart:
                        {
                            ODataEntry entry = (ODataEntry)reader.Item;
                            this.writer.WriteLine("ODataEntry:");
                            this.writer.Indent++;
                        }

                        break;

                    case ODataReaderState.EntryEnd:
                        {
                            ODataEntry entry = (ODataEntry)reader.Item;
                            this.writer.WriteLine("TypeName: " + (entry.TypeName ?? "<null>"));
                            this.writer.WriteLine("Id: " + (entry.Id ?? "<null>"));
                            if (entry.ReadLink != null)
                            {
                                this.writer.WriteLine("ReadLink: " + entry.ReadLink.AbsoluteUri);
                            }

                            if (entry.EditLink != null)
                            {
                                this.writer.WriteLine("EditLink: " + entry.EditLink.AbsoluteUri);
                            }

                            if (entry.MediaResource != null)
                            {
                                this.writer.Write("MediaResource: ");
                                this.WriteValue(entry.MediaResource);
                            }

                            this.WriteProperties(entry.Properties);

                            this.writer.Indent--;
                        }

                        break;

                    case ODataReaderState.NavigationLinkStart:
                        {
                            ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                            this.writer.WriteLine(navigationLink.Name + ": ODataNavigationLink: ");
                            this.writer.Indent++;
                        }

                        break;

                    case ODataReaderState.NavigationLinkEnd:
                        {
                            ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                            this.writer.WriteLine("Url: " + (navigationLink.Url == null ? "<null>" : navigationLink.Url.AbsoluteUri));
                            this.writer.Indent--;
                        }

                        break;
                }
            }
        }

        private void WriteProperties(IEnumerable<ODataProperty> properties)
        {
            this.writer.WriteLine("Properties:");
            this.writer.Indent++;
            foreach (ODataProperty property in properties)
            {
                this.writer.Write(property.Name + ": ");
                this.WriteValue(property.Value);
            }

            this.writer.Indent--;
        }

        private void WriteValue(object value)
        {
            ODataComplexValue complexValue = value as ODataComplexValue;
            if (complexValue != null)
            {
                this.writer.WriteLine("ODataComplexValue");
                this.writer.Indent++;
                this.writer.WriteLine("TypeName: " + (complexValue.TypeName ?? "<null>"));
                this.WriteProperties(complexValue.Properties);
                this.writer.Indent--;

                return;
            }

            ODataMultiValue multiValue = value as ODataMultiValue;
            if (multiValue != null)
            {
                this.writer.WriteLine("ODataMultiValue");
                this.writer.Indent++;
                this.writer.WriteLine("TypeName: " + (multiValue.TypeName ?? "<null>"));
                this.writer.WriteLine("Items:");
                this.writer.Indent++;
                foreach (object item in multiValue.Items)
                {
                    this.WriteValue(item);
                }

                this.writer.Indent--;
                this.writer.Indent--;

                return;
            }

            ODataStreamReferenceValue streamReferenceValue = value as ODataStreamReferenceValue;
            if (streamReferenceValue != null)
            {
                this.writer.WriteLine("ODataStreamReferenceValue");
                this.writer.Indent++;
                if (streamReferenceValue.ReadLink != null)
                {
                    this.writer.WriteLine("ReadLink: " + streamReferenceValue.ReadLink.AbsoluteUri);
                }

                if (streamReferenceValue.EditLink != null)
                {
                    this.writer.WriteLine("EditLink: " + streamReferenceValue.EditLink.AbsoluteUri);
                }

                this.writer.Indent--;

                return;
            }

            if (value == null)
            {
                this.writer.WriteLine("null");
            }
            else
            {
                this.writer.WriteLine(value.ToString());
            }
        }

        public void ExecuteNetflixRequest(IEdmModel model, string fileName)
        {
            //we are going to create a GET request to the OData Netflix Catalog
            HTTPClientRequestMessage message = new HTTPClientRequestMessage("http://odata.netflix.com/v2/Catalog/Genres");
            message.SetHeader("Accept", "application/json");
            message.SetHeader("DataServiceVersion", ODataUtils.ODataVersionToString(ODataVersion.V2));
            message.SetHeader("MaxDataServiceVersion", ODataUtils.ODataVersionToString(ODataVersion.V2));

            //create a simple text file to write the response to and create a text writer
            string filePath = @".\out\" + fileName + ".txt";
            using (StreamWriter outputWriter = new StreamWriter(filePath))
            {
                //use an indented text writer for readability
                this.writer = new IndentedTextWriter(outputWriter, "  ");

                //issue the request and get the response as an ODataMessage. Create an ODataMessageReader over the response 
                //we will use the model when creating the reader as this will tell the library to validate when parsing
                using (ODataMessageReader messageReader = new ODataMessageReader(message.GetResponse(),
                    new ODataMessageReaderSettings(), model))
                {
                    //create a feed reader 
                    ODataReader reader = messageReader.CreateODataFeedReader();
                    while (reader.Read())
                    {
                        switch (reader.State)
                        {
                            case ODataReaderState.FeedStart:
                                {
                                    //this is just the beginning of the feed, data will not be parsed yet
                                    ODataFeed feed = (ODataFeed)reader.Item;
                                    this.writer.WriteLine("ODataFeed:");
                                    this.writer.Indent++;
                                }

                                break;

                            case ODataReaderState.FeedEnd:
                                {
                                    //this is the end of feed state. The entire message has been read at this point
                                    ODataFeed feed = (ODataFeed)reader.Item;
                                    if (feed.Count != null)
                                    {
                                        //if there is an inlinecount value write the value out
                                        this.writer.WriteLine("Count: " + feed.Count.ToString());
                                    }
                                    if (feed.NextPageLink != null)
                                    {
                                        //if there is a next link write that link as well
                                        this.writer.WriteLine("NextPageLink: " + feed.NextPageLink.AbsoluteUri);
                                    }

                                    this.writer.Indent--;
                                }

                                break;

                            case ODataReaderState.EntryStart:
                                {
                                    //this is just the start of the entry. 
                                    //Properties of the entity will not be parsed yet
                                    ODataEntry entry = (ODataEntry)reader.Item;
                                    this.writer.WriteLine("ODataEntry:");
                                    this.writer.Indent++;
                                }

                                break;

                            case ODataReaderState.EntryEnd:
                                {
                                    //at the point the whole entry has been read
                                    //and the properties of the entity are available
                                    ODataEntry entry = (ODataEntry)reader.Item;
                                    this.writer.WriteLine("TypeName: " + (entry.TypeName ?? "<null>"));
                                    this.writer.WriteLine("Id: " + (entry.Id ?? "<null>"));
                                    if (entry.ReadLink != null)
                                    {
                                        this.writer.WriteLine("ReadLink: " + entry.ReadLink.AbsoluteUri);
                                    }

                                    if (entry.EditLink != null)
                                    {
                                        this.writer.WriteLine("EditLink: " + entry.EditLink.AbsoluteUri);
                                    }

                                    if (entry.MediaResource != null)
                                    {
                                        this.writer.Write("MediaResource: ");
                                        this.WriteValue(entry.MediaResource);
                                    }

                                    this.WriteProperties(entry.Properties);

                                    this.writer.Indent--;
                                }

                                break;

                            case ODataReaderState.NavigationLinkStart:
                                {
                                    //navigation links have their own states. 
                                    //This could be an expanded link and include an entire expanded entry or feed.
                                    ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                                    this.writer.WriteLine(navigationLink.Name + ": ODataNavigationLink: ");
                                    this.writer.Indent++;
                                }

                                break;

                            case ODataReaderState.NavigationLinkEnd:
                                {
                                    ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                                    this.writer.WriteLine("Url: " +
                                        (navigationLink.Url == null ? "<null>" : navigationLink.Url.AbsoluteUri));
                                    this.writer.Indent--;
                                }

                                break;
                        }
                    }
                }
            }
        }

        public void ExecuteBaseballStatsRequest(IEdmModel model, string fileName)
        {
            //we are going to create a GET request to the OData Netflix Catalog
            HTTPClientRequestMessage message = new HTTPClientRequestMessage("http://baseball-stats.info/OData/baseballstats.svc/");
            message.SetHeader("Accept", "application/atom+xml");
            message.SetHeader("DataServiceVersion", ODataUtils.ODataVersionToString(ODataVersion.V2));
            message.SetHeader("MaxDataServiceVersion", ODataUtils.ODataVersionToString(ODataVersion.V2));

            //create a simple text file to write the response to and create a text writer
            string filePath = @".\out\" + fileName + ".txt";
            using (StreamWriter outputWriter = new StreamWriter(filePath))
            {
                //use an indented text writer for readability
                this.writer = new IndentedTextWriter(outputWriter, "  ");

                //issue the request and get the response as an ODataMessage. Create an ODataMessageReader over the response 
                //we will use the model when creating the reader as this will tell the library to validate when parsing
                using (ODataMessageReader messageReader = new ODataMessageReader(message.GetResponse(),
                    new ODataMessageReaderSettings(), model))
                {
                    //create a feed reader 
                    ODataReader reader = messageReader.CreateODataFeedReader();
                    while (reader.Read())
                    {
                        switch (reader.State)
                        {
                            case ODataReaderState.FeedStart:
                                {
                                    //this is just the beginning of the feed, data will not be parsed yet
                                    ODataFeed feed = (ODataFeed)reader.Item;
                                    this.writer.WriteLine("ODataFeed:");
                                    this.writer.Indent++;
                                }

                                break;

                            case ODataReaderState.FeedEnd:
                                {
                                    //this is the end of feed state. The entire message has been read at this point
                                    ODataFeed feed = (ODataFeed)reader.Item;
                                    if (feed.Count != null)
                                    {
                                        //if there is an inlinecount value write the value out
                                        this.writer.WriteLine("Count: " + feed.Count.ToString());
                                    }
                                    if (feed.NextPageLink != null)
                                    {
                                        //if there is a next link write that link as well
                                        this.writer.WriteLine("NextPageLink: " + feed.NextPageLink.AbsoluteUri);
                                    }

                                    this.writer.Indent--;
                                }

                                break;

                            case ODataReaderState.EntryStart:
                                {
                                    //this is just the start of the entry. 
                                    //Properties of the entity will not be parsed yet
                                    ODataEntry entry = (ODataEntry)reader.Item;
                                    this.writer.WriteLine("ODataEntry:");
                                    this.writer.Indent++;
                                }

                                break;

                            case ODataReaderState.EntryEnd:
                                {
                                    //at the point the whole entry has been read
                                    //and the properties of the entity are available
                                    ODataEntry entry = (ODataEntry)reader.Item;
                                    this.writer.WriteLine("TypeName: " + (entry.TypeName ?? "<null>"));
                                    this.writer.WriteLine("Id: " + (entry.Id ?? "<null>"));
                                    if (entry.ReadLink != null)
                                    {
                                        this.writer.WriteLine("ReadLink: " + entry.ReadLink.AbsoluteUri);
                                    }

                                    if (entry.EditLink != null)
                                    {
                                        this.writer.WriteLine("EditLink: " + entry.EditLink.AbsoluteUri);
                                    }

                                    if (entry.MediaResource != null)
                                    {
                                        this.writer.Write("MediaResource: ");
                                        this.WriteValue(entry.MediaResource);
                                    }

                                    this.WriteProperties(entry.Properties);

                                    this.writer.Indent--;
                                }

                                break;

                            case ODataReaderState.NavigationLinkStart:
                                {
                                    //navigation links have their own states. 
                                    //This could be an expanded link and include an entire expanded entry or feed.
                                    ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                                    this.writer.WriteLine(navigationLink.Name + ": ODataNavigationLink: ");
                                    this.writer.Indent++;
                                }

                                break;

                            case ODataReaderState.NavigationLinkEnd:
                                {
                                    ODataNavigationLink navigationLink = (ODataNavigationLink)reader.Item;
                                    this.writer.WriteLine("Url: " +
                                        (navigationLink.Url == null ? "<null>" : navigationLink.Url.AbsoluteUri));
                                    this.writer.Indent--;
                                }

                                break;
                        }
                    }
                }
            }
        }
    }
}
