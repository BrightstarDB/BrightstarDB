using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.OData.Tests.Tests
{
    [TestClass]
    public class ODataTests : ODataTestBase
    {
        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar");

            if (!client.DoesStoreExist("OdataTests"))
            {
                DropAndRecreateStore();
                CreateData();
            }
            else
            {
                GetSkills();
            }
            StartService(new Uri("http://localhost:8090/odata"));
        }

        [ClassCleanup]
        public static void TearDown()
        {
            StopService();
        }

        [TestInitialize]
        public void TestSetUp()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        #region Helper Methods

        private void CheckProperties(XElement item, string[] propertyNames, bool idIncluded = true)
        {
            var expected = propertyNames.Count();
            if(idIncluded)  expected ++; //+1 for id
            CheckPropertyCount(item, expected);
            if(idIncluded)
            {
                var id = GetPropertyValue(item, "Id");
                Assert.IsNotNull(id);   
            }
            foreach (var propertyName in propertyNames)
            {
                var value = GetPropertyValue(item, propertyName);
                Assert.IsNotNull(value, string.Format("The parameter {0} was expected but not found in the item's properties", propertyName));
            }
        }

        private void CheckPropertyCount(XElement entry, int expectedNum)
        {
            var content = entry.Element(Atom + "content");
            Assert.IsNotNull(content);
            var properties = content.Element(Metadata + "properties");
            Assert.IsNotNull(properties);
            var allProperties = properties.Descendants();
            Assert.AreEqual(expectedNum, allProperties.Count(), "Number of properties did not match the expected number");
            
        }

        private string GetPropertyValue(XElement entry, string propertyName)
        {
            var content = entry.Element(Atom + "content");
            if (content == null) return null;
            var properties = content.Element(Metadata + "properties");
            if (properties == null) return null;
            var property = properties.Element(Data + propertyName);
            if (property == null) return null;
            return property.Value;
        }

        private const string _related = "http://schemas.microsoft.com/ado/2007/08/dataservices/related/";

        private Dictionary<string, string> GetRelatedLinks(XElement item)
        {
            var related = new Dictionary<string, string>();
            var links = item.Elements(Atom + "link");
            foreach (var link in links)
            {
                var rel = link.Attribute("rel");
                if (rel == null || !rel.Value.StartsWith(_related)) continue;
                var titleAtt = link.Attribute("title");
                var title = titleAtt != null ? titleAtt.Value : "";
                var hrefAtt = link.Attribute("href");
                var href = hrefAtt != null ? hrefAtt.Value : "";
                related.Add(title, href);
            }
            return related;
        }

        private XDocument GetDocumentForItem(XElement item)
        {
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);
            return singleEntryDoc;
        }

        private string GetUrlForItem(XElement item)
        {
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            return url;
        }

        
        #endregion

        #region Metadata Tests
        private static readonly string[] ExpectedEntityNames = new[]
                                                          {
                                                              "Article",
                                                              "Company",
                                                              "Department",
                                                              "JobRole",
                                                              "Person",
                                                              "Project",
                                                              "Skill",
                                                              "DataTypeTestEntity"
                                                          };

        private static readonly string[] ExpectedAssociations = new[]
                                                          {
                                                              "Article_Publisher_Person_Articles",
                                                              "Department_Company_Company_Departments",
                                                              "Person_Department_Department_Persons",
                                                              "Person_JobRole_JobRole_Persons",
                                                              "Person_Skills_Skill_SkilledPeople",
                                                              "Skill_Children_Skill_Parent"
                                                          };

        private static readonly string[] ExpectedAssociationSets = new[]
                                                          {
                                                              "Publisher_Articles",
                                                              "Company_Departments",
                                                              "Department_Persons",
                                                              "JobRole_Persons",
                                                              "Skills_SkilledPeople",
                                                              "Children_Parent",
                                                          };

        [TestMethod]
        public void TestMetadataContent()
        {
            var metadataDoc = Get(".");
            Assert.IsNotNull(metadataDoc);
            XElement workspace = metadataDoc.Root.Element(App + "workspace");
            Assert.IsNotNull(workspace);
            var collections = workspace.Elements(App + "collection");
            Assert.AreEqual(ExpectedEntityNames.Count(), collections.Count(), "Returned collections list did not match length of expected entity names list");
            foreach (var collection in collections)
            {
                var title = collection.Element(Atom + "title");
                Assert.IsNotNull(title);
                Assert.IsNotNull(title.Value);
                Assert.IsTrue(ExpectedEntityNames.Contains(title.Value), "The collection name '{0}' could not be found in the list of expected entity names");
            }
        }

        [TestMethod]
        public void TestGetFullMetadataModel()
        {
            var metadataDoc = Get("$metadata");
            Assert.IsNotNull(metadataDoc);
            var ds = metadataDoc.Root.Element(Edmx + "DataServices");
            Assert.IsNotNull(ds);
            var schemas = ds.Elements(Edm + "Schema");
            Assert.IsNotNull(schemas);
            Assert.AreEqual(2, schemas.Count());

            var schema = schemas.First();
            Assert.IsNotNull(schema);
            var entityTypes = schema.Elements(Edm + "EntityType");
            Assert.IsNotNull(entityTypes);
            Assert.AreEqual(ExpectedEntityNames.Count(), entityTypes.Count());
            foreach (var et in entityTypes)
            {
                var name = et.Attribute("Name");
                Assert.IsNotNull(name);
                Assert.IsNotNull(name.Value);
                Assert.IsTrue(ExpectedEntityNames.Contains(name.Value), "The entity type name '{0}' could not be found in the list of expected entity names");
            }

            var associations = schema.Elements(Edm + "Association");
            Assert.IsNotNull(associations);
            Assert.AreEqual(ExpectedAssociations.Count(), associations.Count());
            foreach (var a in associations)
            {
                var name = a.Attribute("Name");
                Assert.IsNotNull(name);
                Assert.IsNotNull(name.Value);
                Assert.IsTrue(ExpectedAssociations.Contains(name.Value), "The association '{0}' could not be found in the list of expected association names");
            }

            var schemaNs = schemas.Last();
            Assert.IsNotNull(schemaNs);
            var entityContainer = schemaNs.Element(Edm + "EntityContainer");
            Assert.IsNotNull(entityContainer);
            var entitySets = entityContainer.Elements(Edm + "EntitySet");
            Assert.IsNotNull(entitySets);
            Assert.AreEqual(ExpectedEntityNames.Count(), entityTypes.Count());

            foreach (var es in entitySets)
            {
                var name = es.Attribute("Name");
                Assert.IsNotNull(name);
                Assert.IsNotNull(name.Value);
                Assert.IsTrue(ExpectedEntityNames.Contains(name.Value), "The entity type name '{0}' could not be found in the list of expected entity names");
            }

            var associationSets = entityContainer.Elements(Edm + "AssociationSet");
            Assert.IsNotNull(associationSets);
            Assert.AreEqual(ExpectedAssociationSets.Count(), associationSets.Count());
            foreach (var set in associationSets)
            {
                var name = set.Attribute("Name");
                Assert.IsNotNull(name);
                Assert.IsNotNull(name.Value);
                Assert.IsTrue(ExpectedAssociationSets.Contains(name.Value), "The association '{0}' could not be found in the list of expected association names", name.Value);
            }

        }
        #endregion

        [TestMethod]
        public void TestGetMetadata()
        {
            var metadataDoc = Get(".");
            Assert.IsNotNull(metadataDoc);
            XElement workspace = metadataDoc.Root.Element(App + "workspace");
            Assert.IsNotNull(workspace);
            var collections = workspace.Elements(App + "collection");
            Assert.IsNotNull(collections);
            Assert.AreEqual(8, collections.Count(), "Metadata does not have the expected number of collections");
        }
        #region Test Collections

        [Ignore]
        [TestMethod]
        public void TestGetArticles()
        {
            var doc = Get("Article");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(100, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] {"Title", "BodyText"};
            CheckProperties(singleEntryDoc.Root, expectedProperties);
        }

        [TestMethod]
        public void TestGetCompanies()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] {"Name", "Address", "DateFormed", "SomeDouble", "SomeDecimal"};
            CheckProperties(singleEntryDoc.Root, expectedProperties);

        }

        [TestMethod]
        public void TestGetDepts()
        {
            var doc = Get("Department");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(2, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] {"Name", "DeptId"};
            CheckProperties(singleEntryDoc.Root, expectedProperties);

        }

        [TestMethod]
        public void TestGetJobRoles()
        {
            var doc = Get("JobRole");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(5, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] {"Description"};
            CheckProperties(singleEntryDoc.Root, expectedProperties);

        }

        //GetPeople collection fails when the Age property is included.

        [TestMethod]
        public void TestGetPeople()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] { "Name", "Salary", "EmployeeId", "Age", "DateOfBirth" };
            CheckProperties(singleEntryDoc.Root, expectedProperties);

        }

        [TestMethod]
        public void TestGetSkills()
        {
            var doc = Get("Skill");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] {"Name"};
            CheckProperties(singleEntryDoc.Root, expectedProperties);
        }

        #endregion

        [TestMethod]
        public void TestJobRoleRelationships()
        {
            var doc = Get("JobRole");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var related = GetRelatedLinks(singleEntryDoc.Root);
            Assert.AreEqual(1, related.Count);
            Assert.IsTrue(related.ContainsKey("Persons"));

            var url1 = related["Persons"];
            var relDoc = Get(url1);
            Assert.IsNotNull(relDoc);

            //there are 2 people in each job role
            var relatedEntities = relDoc.Descendants(Atom + "entry");
            Assert.IsNotNull(relatedEntities);
            Assert.AreEqual(2, relatedEntities.Count());
        }

        [TestMethod]
        public void TestDepartmentRelationships()
        {
            var doc = Get("Department");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            //get an item
            var item = entries.First();
            var singleEntryDoc = GetDocumentForItem(item);

            var related = GetRelatedLinks(singleEntryDoc.Root);
            Assert.AreEqual(2, related.Count);
            Assert.IsTrue(related.ContainsKey("Persons"));
            Assert.IsTrue(related.ContainsKey("Company"));

            var url1 = related["Persons"];
            var relDoc = Get(url1);
            Assert.IsNotNull(relDoc);

            //there are 5 people in each dept
            var relatedEntities = relDoc.Descendants(Atom + "entry");
            Assert.IsNotNull(relatedEntities);
            Assert.AreEqual(5, relatedEntities.Count());
        }

        [TestMethod]
        public void TestPeopleRelationships()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            var item = entries.First();
            var singleEntryDoc = GetDocumentForItem(item);
            Assert.IsNotNull(singleEntryDoc);

            var related = GetRelatedLinks(singleEntryDoc.Root);
            Assert.AreEqual(4, related.Count); //department, role, skills, articles
            Assert.IsTrue(related.ContainsKey("Department"));
            Assert.IsTrue(related.ContainsKey("JobRole"));
            Assert.IsTrue(related.ContainsKey("Skills"));
            Assert.IsTrue(related.ContainsKey("Articles"));

            var skillsUrl = related["Skills"];
            var relSkillsDoc = Get(skillsUrl);
            Assert.IsNotNull(relSkillsDoc);

            var relSkills = relSkillsDoc.Descendants(Atom + "entry");
            Assert.IsNotNull(relSkills);
            Assert.AreEqual(1, relSkills.Count());

            var deptUrl = related["Department"];
            var relDeptDoc = Get(deptUrl);
            Assert.IsNotNull(relDeptDoc);

            var relDept = relDeptDoc.Descendants(Atom + "entry");
            Assert.IsNotNull(relDept);
            Assert.AreEqual(1, relDept.Count());


            var jrUrl = related["JobRole"];
            var relJobRoleDoc = Get(jrUrl);
            Assert.IsNotNull(relJobRoleDoc);

            var relRoles = relJobRoleDoc.Descendants(Atom + "entry");
            Assert.IsNotNull(relRoles);
            Assert.AreEqual(1, relRoles.Count());



        }

        [TestMethod]
        public void TestSkillRelationships()
        {
            var doc = Get("Skill");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            foreach(var skillItem in entries)
            {
                var singleEntryDoc = GetDocumentForItem(skillItem);
                Assert.IsNotNull(singleEntryDoc);

                var related = GetRelatedLinks(singleEntryDoc.Root);
                Assert.AreEqual(3, related.Count);
                Assert.IsTrue(related.ContainsKey("Parent"));
                Assert.IsTrue(related.ContainsKey("Children"));
                Assert.IsTrue(related.ContainsKey("SkilledPeople"));

                var url = related["SkilledPeople"];
                var xmldoc = Get(url);
                Assert.IsNotNull(xmldoc);
                var relatedItems = xmldoc.Descendants(Atom + "entry");
                Assert.IsNotNull(relatedItems);
                Assert.AreEqual(1, relatedItems.Count());

                //all skills are children of the first skill
                url = related["Children"];
                xmldoc = Get(url);
                Assert.IsNotNull(xmldoc);
                relatedItems = xmldoc.Descendants(Atom + "entry");
                Assert.IsNotNull(relatedItems);
                var childrenCount = relatedItems.Count();
                if(childrenCount == 0)
                {
                    //this is a child.

                    //check parent
                    url = related["Parent"];
                    xmldoc = Get(url);
                    Assert.IsNotNull(xmldoc);
                    relatedItems = xmldoc.Descendants(Atom + "entry");
                    Assert.IsNotNull(relatedItems);
                    Assert.AreEqual(1, relatedItems.Count());

                } else
                {
                    //this is the parent
                    Assert.AreEqual(9, childrenCount);
                }
            }
        }

        [TestMethod]
        public void TestCompanyRelationships()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            var item = entries.First();
            var singleEntryDoc = GetDocumentForItem(item);
            Assert.IsNotNull(singleEntryDoc);

            var related = GetRelatedLinks(singleEntryDoc.Root);
            Assert.AreEqual(1, related.Count);
            Assert.IsTrue(related.ContainsKey("Departments"));

            var deptUrl = related["Departments"];
            var relDeptDoc = Get(deptUrl);
            Assert.IsNotNull(relDeptDoc);

            var relDept = relDeptDoc.Descendants(Atom + "entry");
            Assert.IsNotNull(relDept);
            Assert.AreEqual(0, relDept.Count());



        }

        [TestMethod]
        public void TestJobRoleRelationshipLinks()
        {
            var doc = Get("JobRole");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);

            var relLinksUrl = string.Format("{0}/$links/Persons", url);
            var linksXml = Get(relLinksUrl);
            Assert.IsNotNull(linksXml);
            Assert.IsNotNull(linksXml.Root);
            var uris = linksXml.Root.Elements(Data + "uri");
            Assert.IsNotNull(uris);
            //there are 2 people in each job role
            Assert.AreEqual(2, uris.Count());
        }

        [TestMethod]
        public void TestDepartmentRelationshipLinks()
        {
            var doc = Get("Department");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);

            var relLinksUrl = string.Format("{0}/$links/Persons", url);
            var linksXml = Get(relLinksUrl);
            Assert.IsNotNull(linksXml);
            Assert.IsNotNull(linksXml.Root);
            var uris = linksXml.Root.Elements(Data + "uri");
            Assert.IsNotNull(uris);

            //there are 5 people in each dept
            Assert.AreEqual(5, uris.Count());
        }

        [TestMethod]
        public void TestPeopleRelationshipLinks()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);

            var relLinksUrl = string.Format("{0}/$links/Department", url);
            var linksXml = Get(relLinksUrl);
            Assert.IsNotNull(linksXml);
            var uris = linksXml.Elements(Data + "uri"); //not a collection: expected so no containing node
            Assert.IsNotNull(uris);
            Assert.AreEqual(1, uris.Count());

            relLinksUrl = string.Format("{0}/$links/JobRole", url);
            linksXml = Get(relLinksUrl);
            Assert.IsNotNull(linksXml);
            uris = linksXml.Elements(Data + "uri");
            Assert.IsNotNull(uris);
            Assert.AreEqual(1, uris.Count());

            relLinksUrl = string.Format("{0}/$links/Skills", url);
            linksXml = Get(relLinksUrl);
            Assert.IsNotNull(linksXml);
            uris = linksXml.Root.Elements(Data + "uri");
            Assert.IsNotNull(uris);
            Assert.AreEqual(1, uris.Count());

        }

        [TestMethod]
        public void TestSkillRelationshipLinks()
        {
            var doc = Get("Skill");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);

            var relLinksUrl = string.Format("{0}/$links/SkilledPeople", url);
            var linksXml = Get(relLinksUrl);
            Assert.IsNotNull(linksXml);
            var uris = linksXml.Root.Elements(Data + "uri");
            Assert.IsNotNull(uris);
            Assert.AreEqual(1, uris.Count());

            relLinksUrl = string.Format("{0}/$links/Children", url);
            linksXml = Get(relLinksUrl);
            Assert.IsNotNull(linksXml);
            uris = linksXml.Root.Elements(Data + "uri");
            Assert.IsNotNull(uris);
            var childrenCount = uris.Count();
            if (childrenCount == 0)
            {
                //this is a child.
                //check parent
                relLinksUrl = string.Format("{0}/$links/Parent", url);
                linksXml = Get(relLinksUrl);
                Assert.IsNotNull(linksXml);
                uris = linksXml.Elements(Data + "uri");
                Assert.IsNotNull(uris);
                Assert.AreEqual(1, uris.Count());

            }
            else
            {
                //this is the parent
                Assert.AreEqual(9, childrenCount);
            }

        }

        [TestMethod]
        public void TestGetById()
        {
            var skillDoc = Get("Skill");
            Assert.IsNotNull(skillDoc.Root);
            var skills = skillDoc.Descendants(Atom + "entry");

            foreach (var skill in skills)
            {
                var identity = skill.Element(Atom + "id");
                Assert.IsNotNull(identity);
                var url = identity.Value;
                //is in format http://localhost:8090/odata/Skill('381882c5-066e-4628-9e19-961f648941ff')
                Assert.IsNotNull(url);

                var singleEntryDoc = Get(url);
                Assert.IsNotNull(singleEntryDoc);

                var id = GetPropertyValue(singleEntryDoc.Root, "Id");
                Assert.IsNotNull(id);
                var name = GetPropertyValue(singleEntryDoc.Root, "Name");
                Assert.IsNotNull(name);

                var checkEntry = Skills.Where(s => s.Id.Equals(id)).FirstOrDefault();
                Assert.IsNotNull(checkEntry);
                Assert.AreEqual(checkEntry.Name, name);
            }
        }

        [TestMethod]
        public void TestGetProperty()
        {
            var lookup = Skills.Where(s => s.Name.Equals("Skill7")).FirstOrDefault();
            Assert.IsNotNull(lookup);

            var urlForProperty = string.Format("Skill('{0}')/Name", lookup.Id);
            //go straight to property
            var propertyDoc = Get(urlForProperty);
            Assert.IsNotNull(propertyDoc);
            Assert.IsNotNull(propertyDoc.Root);
            Assert.IsNotNull(propertyDoc.Root.Value);
            Assert.AreEqual(lookup.Name, propertyDoc.Root.Value);
        }

        [TestMethod]
        public void TestGetRelatedProperty()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            var item = entries.First();
            var singleEntryDoc = GetDocumentForItem(item);
            Assert.IsNotNull(singleEntryDoc);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            //is in format http://localhost:8090/odata/Skill('381882c5-066e-4628-9e19-961f648941ff')
            Assert.IsNotNull(url);

            var urlForProperty = string.Format("{0}/Department/Name", url);
            //go straight to property
            var propertyDoc = Get(urlForProperty);
            Assert.IsNotNull(propertyDoc);
            Assert.IsNotNull(propertyDoc.Root);
            Assert.IsNotNull(propertyDoc.Root.Value);

        }

        [TestMethod]
        public void TestGetPropertyValue()
        {
            var lookup = Skills.Where(s => s.Name.Equals("Skill7")).FirstOrDefault();
            Assert.IsNotNull(lookup);

            var urlForProperty = string.Format("Skill('{0}')/Name/$value", lookup.Id);
            //go straight to property
            var value = GetValue(urlForProperty);
            Assert.IsNotNull(value);
            Assert.AreEqual(lookup.Name, value);
        }

        [TestMethod]
        public void TestGetRelatedPropertyValue()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            var item = entries.First();
            var singleEntryDoc = GetDocumentForItem(item);
            Assert.IsNotNull(singleEntryDoc);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);

            var urlForProperty = string.Format("{0}/Department/Name/$value", url);
            //go straight to property
            var value = GetValue(urlForProperty);
            Assert.IsNotNull(value);


        }

        [TestMethod]
        public void TestOrderBy()
        {
            var doc = Get("Skill");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Skill?$orderby=Name%20desc");
            Assert.IsNotNull(doc.Root);

            var ordered = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(ordered);
            Assert.AreEqual(10, ordered.Count(), "Expected number of entries not met");
        }


        [TestMethod]
        public void TestTop()
        {
            var doc = Get("Skill");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Skill?$top=2");
            Assert.IsNotNull(doc.Root);

            var top2 = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(top2);
            Assert.AreEqual(2, top2.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestSkip()
        {
            var doc = Get("Skill");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Skill?$skip=5");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(5, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestPagedRelationship()
        {
            var doc = Get("Department");
            Assert.IsNotNull(doc.Root);
            var entry = doc.Descendants(Atom + "entry").FirstOrDefault();
            Assert.IsNotNull(entry);
            var identity = entry.Element(Atom + "id");
            Assert.IsNotNull(identity);

            doc = Get(String.Format("{0}/Persons?$top=5", identity.Value));
            Assert.IsNotNull(doc);
            var entries = doc.Descendants(Atom + "entry");
            Assert.AreEqual(5, entries.Count());
        }

        #region Filtering

        [TestMethod]
        public void TestFilterEqual()
        {
            // salary = 29000 (should be 1)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary eq 29000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterNotEqual()
        {
            // salary != 29000 (should be 9)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary ne 29000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(9, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterGreaterThan()
        {
            // salary > 29000 (should be 4)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary gt 29000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(4, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterGreaterThanOrEqual()
        {
            // salary >= 29000 (should be 5)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary ge 29000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(5, results.Count(), "Expected number of entries not met");
        }


        [TestMethod]
        public void TestFilterLessThan()
        {
            // salary < 29000 (should be 5)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary lt 29000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(5, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterLessThanOrEqual()
        {
            // salary <= 29000 (should be 6)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary le 29000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(6, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterLogicalAnd()
        {
            //less than 29K and greater than 27.5K = 3 people
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary lt 29000 and Salary gt 27500");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterLogicalOr()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary eq 29000 or Salary eq 27400");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterLogicalNot()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=not(Salary eq 29000)");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(9, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterAdd()
        {
            // salary + 1000 = 30000 (should be 1)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary add 1000 eq 30000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }
        [TestMethod]
        public void TestFilterSubtract()
        {
            // salary - 1000 = 28000 (should be 1)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary sub 1000 eq 28000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterMultiply()
        {
            // salary * 2 = 58000 (should be 1)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary mul 2 eq 58000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }
        [TestMethod]
        public void TestFilterDivide()
        {
            // salary / 2 = 14500 (should be 1)
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary div 2 eq 14500");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [Ignore] //unsupported
        [TestMethod]
        public void TestFilterMod()
        {
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=Salary mod 1500 eq 0");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterPrecedenceGrouping()
        {
            //29000
            // (salary - 1000 ) * 2 = 56000 (should be 1)
            // (salary  * 2) - 1000 = 57000
            var doc = Get("Person");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            //note - I'm not 100% that this is truly testing precedence of brackets
            doc = Get("Person?$filter=(Salary sub 1000) mul 2 eq 56000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");

            doc = Get("Person?$filter=(Salary mul 2) sub 1000 eq 57000");
            Assert.IsNotNull(doc.Root);

            results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringStartsWith()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=startswith(Name, 'Netw') eq true");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringEndsWith()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=endswith(Name, 'Blades') eq true");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringSubstringOf()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=substringof('Networked', Name) eq true");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringLength()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=length(Name) eq 6"); //biblos
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [Ignore] //unsupported
        [TestMethod]
        public void TestFilterStringIndexOf()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=indexof(Name, 'etw') eq 1");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        [Ignore] // BUG 5690 : Currently not working - may be a reflection provider issue
        public void TestFilterStringReplace()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=replace(Name, ' ', '') eq 'NetworkedPlanet'");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringSubstring()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=substring(Name, 1, 2) eq 'et'");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringSubstring2()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=substring(Name, 3) eq 'worked Planet'");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringToLower()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=tolower(Name) eq 'networked planet'");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterStringToUpper()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=toupper(Name) eq 'NETWORKED PLANET'");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

      
        [TestMethod]
        public void TestFilterStringConcat()
        {
            //e.g. http://services.odata.org/Northwind/Northwind.svc/Customers?$filter=concat(concat(City, ', '), Country) eq 'Berlin, Germany'
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=concat(Name, Address) eq 'Networked PlanetOxford, UK'");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        #endregion

        [Ignore]
        [TestMethod]
        public void TestExpand()
        {
            var doc = Get("Department");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            var item = entries.First();
            var url = GetUrlForItem(item);

            var getDepartmentExpandPeople = string.Format("{0}?$expand=Persons", url);

            var expanded = Get(getDepartmentExpandPeople);
            Assert.IsNotNull(expanded);
            Assert.IsNotNull(expanded.Root);
            Assert.AreEqual(Atom + "entry", expanded.Root.Name);
            var id = expanded.Root.Element(Atom + "id");
            Assert.IsNotNull(id);
            Assert.AreEqual(url, id.Value);
            var personLink =
                expanded.Root.Elements(Atom + "link").FirstOrDefault(x => x.Attribute("title").Value.Equals("Persons"));
            Assert.IsNotNull(personLink);
            var persons = personLink.Descendants(Atom + "entry");
            Assert.AreEqual(5, persons.Count());
        }

        [Ignore] //bug 5376 - [documented for now]
        [TestMethod]
        public void TestExpand2()
        {
            var doc = Get("Department");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);

            var item = entries.First();
            var url = GetUrlForItem(item);

            var getDepartmentExpandPeopleExpandSkills = string.Format("{0}?$expand=Persons/Skills", url);

            var expanded = Get(getDepartmentExpandPeopleExpandSkills);
            Assert.IsNotNull(expanded);
            Assert.IsNotNull(expanded.Root);
            Assert.AreEqual(Atom + "entry", expanded.Root.Name);
            var id = expanded.Root.Element(Atom + "id");
            Assert.IsNotNull(id);
            Assert.AreEqual(url, id.Value);
            var personLink =
                expanded.Root.Elements(Atom + "link").FirstOrDefault(x => x.Attribute("title").Value.Equals("Persons"));
            Assert.IsNotNull(personLink);
            var persons = personLink.Descendants(Atom + "entry");
            Assert.IsNotNull(persons);
            //Assert.AreEqual(5, persons.Count());

            var firstPerson = persons.First();
            var skillLink =
                firstPerson.Elements(Atom + "link").FirstOrDefault(x => x.Attribute("title").Value.Equals("Skills"));
            Assert.IsNotNull(skillLink);
            var skills = skillLink.Descendants(Atom + "entry");
            Assert.IsNotNull(skills);
            Assert.AreEqual(1, skills.Count());
        }

        [Ignore]
        [TestMethod]
        public void TestSelect()
        {
            //select only name and salary
            var doc = Get("Person?$select=Age,Salary");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            
            // At least Salary and Name properties should be there (and there should also be some others)
            var expectedProperties = new string[] { "Salary", "Age" };
            CheckProperties(item, expectedProperties, false);
        }

        [TestMethod]
        public void TestSelectAll()
        {
            //select only name and salary
            var doc = Get("Company?$select=*");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();

            // At least Salary and Name properties should be there (and there should also be some others)
            var expectedProperties = new string[] { "Name", "Address", "DateFormed", "SomeDouble", "SomeDecimal" };
            CheckProperties(item, expectedProperties);
        }

        [Ignore]
        [TestMethod]
        public void TestSelectAndExpand()
        {
            var doc = Get("Person?$select=Name,Skills"); //&$expand=Skills
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            // At least Salary and Name properties should be there (and there should also be some others)
            var expectedProperties = new string[] { "Name"};
            CheckProperties(item, expectedProperties, false);
            //check skills link
            var skillsLink =
                item.Elements(Atom + "link").FirstOrDefault(x => x.Attribute("title").Value.Equals("Skills"));
            Assert.IsNotNull(skillsLink);
            var skills = skillsLink.Descendants(Atom + "entry");
            Assert.AreEqual(0, skills.Count());


            var expandedDoc = Get("Person?$select=Name,Skills&$expand=Skills");
            Assert.IsNotNull(expandedDoc.Root);
            entries = expandedDoc.Descendants(Atom + "entry"); //10 persons + 10 skills

            Assert.IsNotNull(entries);
            Assert.AreEqual(20, entries.Count(), "Expected number of entries not met");

            //get an item
            item = entries.First();
            // At least Salary and Name properties should be there (and there should also be some others)
            expectedProperties = new string[] { "Name" };
            CheckProperties(item, expectedProperties, false);
            //check skills link
            skillsLink =
                item.Elements(Atom + "link").FirstOrDefault(x => x.Attribute("title").Value.Equals("Skills"));
            Assert.IsNotNull(skillsLink);
            skills = skillsLink.Descendants(Atom + "entry");
            Assert.AreEqual(1, skills.Count());
        }

        [TestMethod]
        public void TestInlineCount()
        {
            var doc = Get("Person?$inlinecount=allpages");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count());
            Assert.IsNotNull(doc.Root);

            var inlineCountElem = doc.Root.Element(Metadata + "count");
            Assert.IsNotNull(inlineCountElem);
            Assert.IsNotNull(inlineCountElem.Value);
            Assert.AreEqual("10", inlineCountElem.Value);
        }

        [TestMethod]
        public void TestInlineCountAfterFilter()
        {
            var doc = Get("Person?$inlinecount=allpages");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count());

            doc = Get("Person?$inlinecount=allpages&$filter=Salary lt 29000");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(5, results.Count(), "Expected number of entries not met");

            Assert.IsNotNull(doc.Root);

            var inlineCountElem = doc.Root.Element(Metadata + "count");
            Assert.IsNotNull(inlineCountElem);
            Assert.IsNotNull(inlineCountElem.Value);
            Assert.AreEqual("5", inlineCountElem.Value);
        }

        [TestMethod]
        public void TestInlineCountNone()
        {
            var doc = Get("Person?$inlinecount=none");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(entries);
            Assert.AreEqual(10, entries.Count());
            Assert.IsNotNull(doc.Root);

            var inlineCountElem = doc.Root.Element(Metadata + "count");
            Assert.IsNull(inlineCountElem);
        }

        #region date functions

        [TestMethod]
        public void TestFilterDateDay()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=day(DateFormed) eq 1");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }
        
        [TestMethod]
        public void TestFilterDateHour()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=hour(DateFormed) eq 12"); //networkedplanet
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterDateMinute()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=minute(DateFormed) eq 15"); //biblos
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterDateMonth()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=month(DateFormed) eq 2"); //biblos
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterDateSecond()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=second(DateFormed) eq 30"); //harry blades
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterDateYear()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=year(DateFormed) eq 2001"); //biblos
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }


        #endregion

        #region math functions

        [TestMethod]
        public void TestFilterMathRoundDouble()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=round(SomeDouble) eq 33"); 
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }
        
        [TestMethod]
        public void TestFilterMathRoundDecimal()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=round(SomeDecimal) eq 33");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterMathFloorDouble()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=floor(SomeDouble) eq 32");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterMathFloorDecimal()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=floor(SomeDecimal) eq 32");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterMathCeilingDouble()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=ceiling(SomeDouble) eq 33");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }

        [TestMethod]
        public void TestFilterMathCeilingDecimal()
        {
            var doc = Get("Company");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(3, entries.Count(), "Expected number of entries not met");

            doc = Get("Company?$filter=ceiling(SomeDecimal) eq 33");
            Assert.IsNotNull(doc.Root);

            var results = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count(), "Expected number of entries not met");
        }


        #endregion

        #region Formatting

        [TestMethod]
        public void TestFormatAtom()
        {
            var targetUri = new Uri("http://localhost:8090/odata/Department");
            var request = WebRequest.Create(targetUri) as HttpWebRequest;
            Assert.IsNotNull(request);
            var response = request.GetResponse() as HttpWebResponse;
            Assert.IsNotNull(response, "Did not receive an HttpWebResponse");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code in response");
            var contentType = response.ContentType.Split(';')[0];
            Assert.AreEqual("application/atom+xml", contentType);
        }

        [Ignore] //not supported - bug 5328
        [TestMethod]
        public void TestFormatJson()
        {
            var targetUri = new Uri("http://localhost:8090/odata/Department?$format=json");
            var request = WebRequest.Create(targetUri) as HttpWebRequest;
            Assert.IsNotNull(request);
            var response = request.GetResponse() as HttpWebResponse;
            Assert.IsNotNull(response, "Did not receive an HttpWebResponse");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code in response");
            var contentType = response.ContentType.Split(';')[0];
            Assert.AreEqual("application/json", contentType);
        }

        [TestMethod]
        public void TestFormatJsonUsingAcceptHeaders()
        {
            var json = GetJson("Person");
            Assert.IsNotNull(json);
        }

        [Ignore] //not supported - bug 5328
        [TestMethod]
        public void TestFormatXml()
        {
            var targetUri = new Uri("http://localhost:8090/odata/Department?$format=xml");
            var request = WebRequest.Create(targetUri) as HttpWebRequest;
            Assert.IsNotNull(request);
            var response = request.GetResponse() as HttpWebResponse;
            Assert.IsNotNull(response, "Did not receive an HttpWebResponse");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code in response");
            var contentType = response.ContentType.Split(';')[0];
            Assert.AreEqual("application/xml", contentType);
        }

        [Ignore]
        [TestMethod]
        public void TestFormatXmlUsingAcceptHeaders()
        {
            var xml = GetXml("Person");
            Assert.IsNotNull(xml);
        }

#endregion

        #region Data Type Tests 

        [TestMethod]
        public void TestDataTypes()
        {
            var doc = Get("DataTypeTestEntity");
            Assert.IsNotNull(doc.Root);
            var entries = doc.Descendants(Atom + "entry");

            Assert.IsNotNull(entries);
            Assert.AreEqual(1, entries.Count(), "Expected number of entries not met");

            //get an item
            var item = entries.First();
            Assert.IsNotNull(item);
            var identity = item.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            //"SomeChar", "AnotherChar", "NullableChar", "AnotherNullableChar",
            //"SomeUInt", "AnotherUInt", "SomeULong", "AnotherULong", "SomeUShort", "AnotherUShort",
            //"CollectionOfStrings", "CollectionOfDateTimes", "CollectionOfBools", "CollectionOfDecimals", "CollectionOfDoubles", "CollectionOfFloats", "CollectionOfInts", "CollectionOfLong"
            // "SomeByteArray", "SomeEnumeration",

            //Guid not supported

            var expectedProperties = new string[] { "SomeString", "SomeDateTime", "SomeNullableDateTime", "SomeBool", "NullableBool", "SomeByte", "AnotherByte", "NullableByte", "AnotherNullableByte",  "SomeDecimal", "SomeDouble", "SomeFloat", "SomeInt", "SomeNullableInt", "SomeLong", "SomeSByte", "AnotherSByte", "SomeShort", "AnotherShort"  };
            CheckProperties(singleEntryDoc.Root, expectedProperties);

        }
        #endregion

        [TestMethod]
        public void TestSkipToken()
        {
            var doc = Get("Article?$top=20");
            Assert.IsNotNull(doc.Root);

            var top20 = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(top20);
            Assert.AreEqual(20, top20.Count(), "Expected number of entries not met");

            //get an item
            var lastitem = top20.Last();
            Assert.IsNotNull(lastitem);
            var identity = lastitem.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] { "Title", "BodyText" };
            CheckProperties(singleEntryDoc.Root, expectedProperties);

            var id = GetPropertyValue(lastitem, "Id");
            Assert.IsNotNull(id);   

            //SKIPTOKEN
            var nextLink = string.Format("Article?$skiptoken='{0}'", id);
            var nextDoc = Get(nextLink);
            Assert.IsNotNull(nextDoc.Root);

            var next20 = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(next20);
            Assert.AreEqual(20, next20.Count(), "Expected number of entries not met");
          

        }

        [TestMethod]
        public void TestSkipTokenOrdered()
        {
            //?$orderby=Name
            var doc = Get("Article?$orderby=Title&$top=20");
            Assert.IsNotNull(doc.Root);

            var top20 = doc.Descendants(Atom + "entry");
            Assert.IsNotNull(top20);
            Assert.AreEqual(20, top20.Count(), "Expected number of entries not met");

            //get an item
            var lastitem = top20.Last();
            Assert.IsNotNull(lastitem);
            var identity = lastitem.Element(Atom + "id");
            Assert.IsNotNull(identity);
            var url = identity.Value;
            Assert.IsNotNull(url);
            //check we can retrieve the xml for that item
            var singleEntryDoc = Get(url);
            Assert.IsNotNull(singleEntryDoc);

            var expectedProperties = new string[] { "Title", "BodyText" };
            CheckProperties(singleEntryDoc.Root, expectedProperties);

            var lastItemId = GetPropertyValue(lastitem, "Id");
            Assert.IsNotNull(lastItemId);
            var lastItemTitle = GetPropertyValue(lastitem, "Title");
            Assert.IsNotNull(lastItemTitle);
            Assert.AreEqual("Article26", lastItemTitle, "Last item title is not the expected value");

            //SKIPTOKEN
            var nextLink = string.Format("Article?$orderby=Title&$skiptoken='{0}','{1}'", lastItemTitle, lastItemId);
            var nextDoc = Get(nextLink);
            Assert.IsNotNull(nextDoc.Root);

            var next20 = nextDoc.Descendants(Atom + "entry");
            Assert.IsNotNull(next20);
            Assert.AreEqual(20, next20.Count(), "Expected number of entries not met");

            //get an item
            var nextSetFirstItem = next20.First();
            Assert.IsNotNull(nextSetFirstItem);
            var identity2 = nextSetFirstItem.Element(Atom + "id");
            Assert.IsNotNull(identity2);
            var url2 = identity2.Value;
            Assert.IsNotNull(url2);
            //check we can retrieve the xml for that item
            var singleEntryDoc2 = Get(url2);
            Assert.IsNotNull(singleEntryDoc2);

            var nextSetFirstItemId = GetPropertyValue(nextSetFirstItem, "Id");
            Assert.IsNotNull(nextSetFirstItemId);
            var nextSetFirstItemTitle = GetPropertyValue(nextSetFirstItem, "Title");
            Assert.IsNotNull(nextSetFirstItemTitle);
            Assert.AreEqual("Article27", nextSetFirstItemTitle, "Last item title is not the expected value");

        }
    }
}
