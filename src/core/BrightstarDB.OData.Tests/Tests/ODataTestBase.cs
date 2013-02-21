using System;
using System.Collections.Generic;
using System.Data.Services;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.OData.Tests.Tests
{
    public class ODataTestBase
    {
        public static XNamespace App = "http://www.w3.org/2007/app";
        public static XNamespace Atom = "http://www.w3.org/2005/Atom";
        public static XNamespace Metadata = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        public static XNamespace Data = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        public static XNamespace Edm = "http://schemas.microsoft.com/ado/2007/05/edm";
        public static XNamespace Edmx = "http://schemas.microsoft.com/ado/2007/06/edmx";

        private static DataServiceHost _host;
        private static Uri _baseUri;

        public static DataServiceHost StartService( Uri baseUri )
        {
            var factory = new ODataServiceHostFactory();
            _host = factory.CreateServiceHost(baseUri);
            _host.Open();
            _baseUri = new Uri(baseUri + "/");
            return _host;
        }

        public static void StopService()
        {
            _host.Close();
            _host = null;
            BrightstarService.Shutdown();
        }

        public static void DropAndRecreateStore()
        {
            const string storeName = "OdataTests";
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar");
            
            if(client.DoesStoreExist(storeName))
            {
                client.DeleteStore(storeName);
                while(client.DoesStoreExist(storeName))
                {
                    Thread.Sleep(10);
                }
            }
            try
            {
                client.CreateStore(storeName);
            }
            catch (Exception)
            {
                client.DeleteStore(storeName);
                client.CreateStore(storeName);
            }
        }

        public static void GetSkills()
        {
            var context = new MyEntityContext();
            Skills = context.Skills.ToList();
        }

        public XDocument Get(string uri, HttpStatusCode expectCode = HttpStatusCode.OK)
        {
            var targetUri = new Uri(_baseUri, uri);
            var request = WebRequest.Create(targetUri) as HttpWebRequest;
            Assert.IsNotNull(request);
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                Assert.IsNotNull(response, "Did not receive an HttpWebResponse");
                Assert.AreEqual(expectCode, response.StatusCode, "Unexpected status code in response");
                using(var reader = new StreamReader(response.GetResponseStream()))
                {
                    var strResult = reader.ReadToEnd();
                    var doc = XDocument.Parse(strResult);
                    return doc;
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null && wex.Response is HttpWebResponse)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response.StatusCode == expectCode)
                    {
                        return null;
                    }
                    string responseContent = String.Empty;
                    using(var streamReader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        responseContent = streamReader.ReadToEnd();
                    }
                    Assert.Fail("GET failed with response: {0} : {1}", wex.Status, responseContent);
                }
                Assert.Fail("GET failed with exception: {0}.", wex);
            }
            return null;
        }

        public string GetJson(string uri, HttpStatusCode expectCode = HttpStatusCode.OK)
        {
            var targetUri = new Uri(_baseUri, uri);
            var request = WebRequest.Create(targetUri) as HttpWebRequest;
            if(request != null) request.Accept = "application/json";
            Assert.IsNotNull(request);
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                Assert.IsNotNull(response, "Did not receive an HttpWebResponse");
                Assert.AreEqual(expectCode, response.StatusCode, "Unexpected status code in response");
                var contentType = response.ContentType.Split(';')[0];
                Assert.AreEqual("application/json", contentType);

                string result;
                var rs = response.GetResponseStream();
                if (rs == null) return null;
                var streamReader = new StreamReader(rs, true);
                try
                {
                    result = streamReader.ReadToEnd();
                }
                finally
                {
                    streamReader.Close();
                }
                return result;
            }
            catch (WebException wex)
            {
                if (wex.Response != null && wex.Response is HttpWebResponse)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response.StatusCode == expectCode)
                    {
                        return null;
                    }
                }
                Assert.Fail("GET failed with exception: {0}", wex);
            }
            return null;
        }

        public string GetXml(string uri, HttpStatusCode expectCode = HttpStatusCode.OK)
        {
            var targetUri = new Uri(_baseUri, uri);
            var request = WebRequest.Create(targetUri) as HttpWebRequest;
            if (request != null)
            {
                request.Accept = "application/xml";
                request.ContentType = "application/xml";
            }

            Assert.IsNotNull(request);
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                Assert.IsNotNull(response, "Did not receive an HttpWebResponse");
                Assert.AreEqual(expectCode, response.StatusCode, "Unexpected status code in response");
                var contentType = response.ContentType.Split(';')[0];
                Assert.AreEqual("application/xml", contentType);

                string result;
                var rs = response.GetResponseStream();
                if (rs == null) return null;
                var streamReader = new StreamReader(rs, true);
                try
                {
                    result = streamReader.ReadToEnd();
                }
                finally
                {
                    streamReader.Close();
                }
                return result;
            }
            catch (WebException wex)
            {
                if (wex.Response != null && wex.Response is HttpWebResponse)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response.StatusCode == expectCode)
                    {
                        return null;
                    }
                }
                Assert.Fail("GET failed with exception: {0}", wex);
            }
            return null;
        }
       

        public string GetValue(string uri, HttpStatusCode expectCode = HttpStatusCode.OK)
        {
            var targetUri = new Uri(_baseUri, uri);
            var request = WebRequest.Create(targetUri) as HttpWebRequest;
            Assert.IsNotNull(request);
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                Assert.IsNotNull(response, "Did not receive an HttpWebResponse");
                Assert.AreEqual(expectCode, response.StatusCode, "Unexpected status code in response");
                string result;
                var rs = response.GetResponseStream();
                if (rs == null) return null;
                var streamReader = new StreamReader(rs, true);
                try
                {
                    result = streamReader.ReadToEnd();
                }
                finally
                {
                    streamReader.Close();
                }
                return result;
            }
            catch (WebException wex)
            {
                if (wex.Response != null && wex.Response is HttpWebResponse)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response.StatusCode == expectCode)
                    {
                        return null;
                    }
                }
                Assert.Fail("GET failed with exception: {0}", wex);
            }
            return null;
        }

        public static List<ISkill> Skills = new List<ISkill>();

        public static void CreateDataTypeTestData()
        {
            var ctx = new MyEntityContext();

            var entity = ctx.DataTypeTestEntities.Create();
            var now = DateTime.Now;
            entity.SomeDateTime = now;
            entity.SomeDecimal = 3.14m;
            entity.SomeDouble = 3.14;
            entity.SomeFloat = 3.14F;
            entity.SomeInt = 3;
            entity.SomeNullableDateTime = null;
            entity.SomeNullableInt = null;
            entity.SomeString = "test entity";

            entity.SomeByte = (Byte)255;
            entity.AnotherByte = (byte)128;
            //entity.NullableByte = null;
            //entity.AnotherNullableByte = null;
            
            entity.SomeSByte = 127;
            entity.AnotherSByte = 64;

            entity.SomeBool = true;
            entity.SomeLong = 50L;

            entity.SomeSByte = 127;
            entity.AnotherSByte = 64;

            entity.SomeShort = 32767;
            entity.AnotherShort = -32768;

            entity.SomeUShort = 65535;
            entity.AnotherUShort = 52;

            entity.SomeUInt = 4294967295;
            entity.AnotherUInt = 12;

            entity.SomeULong = 18446744073709551615;
            entity.AnotherULong = 52;

            //collections

            for (var i = 0; i < 10; i++)
            {
                var date = now.AddDays(i);
                entity.CollectionOfDateTimes.Add(date);
            }
            for (var i = 0; i < 10; i++)
            {
                var dec = i + .5m;
                entity.CollectionOfDecimals.Add(dec);
            }
            for (var i = 0; i < 10; i++)
            {
                var dbl = i + .5;
                entity.CollectionOfDoubles.Add(dbl);
            }
            for (var i = 0; i < 10; i++)
            {
                var flt = i + .5F;
                entity.CollectionOfFloats.Add(flt);
            }
            for (var i = 0; i < 10; i++)
            {
                entity.CollectionOfInts.Add(i);
            }
            entity.CollectionOfBools.Add(true);
            entity.CollectionOfBools.Add(false);
            for (var i = 0; i < 10; i++)
            {
                var l = i * 100;
                entity.CollectionOfLong.Add(l);
            }
            for (var i = 0; i < 10; i++)
            {
                var s = "word" + i;
                entity.CollectionOfStrings.Add(s);
            }

            ctx.SaveChanges();

        }

        public static List<IProject> CreateProjects()
        {
            var ctx = new MyEntityContext();
            var projects = new List<IProject>();
            var j = 0;
            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            for (int i = 0; i < 260; i++)
            {
                var p = ctx.Projects.Create();
                p.Title = alpha[j] + "_Project_Name " + Guid.NewGuid().ToString();
                //p.Website = new Uri("http://data.gov.uk/");
                p.StartDate = DateTime.Now;
                p.ProjectCode = 1727 + i;
                projects.Add(p);

                j++;
                if (j > 25) j = 0;
            }

            ctx.SaveChanges();
            return projects;
        }

        //connection string is in app.config
        public static void CreateData()
        {
            var ctx = new MyEntityContext();

            var dateFormedBase = new DateTime(2000, 01, 01, 12, 0, 0);
            var biblosFormed =
                dateFormedBase.AddYears(1).AddMonths(1).AddDays(1).AddHours(1).AddMinutes(15).AddSeconds(15); //2nd feb 2001 13:15:15pm
            var hbFormed =
                biblosFormed.AddYears(1).AddMonths(1).AddDays(1).AddHours(1).AddMinutes(15).AddSeconds(15); //3rd mar 2002 14:30:30pm

            //add companies
            var networkedPlanet = ctx.Companies.Create();
            networkedPlanet.Name = "Networked Planet";
            networkedPlanet.Address = "Oxford, UK";
            networkedPlanet.DateFormed = dateFormedBase;
            networkedPlanet.SomeDecimal = 32.3800M;
            networkedPlanet.SomeDouble = 32.3800;

            var company2 = ctx.Companies.Create();
            company2.Name = "Biblos";
            company2.Address = "Stokes Croft, Bristol, UK";
            company2.DateFormed = biblosFormed;
            company2.SomeDecimal = 33.3800M;
            company2.SomeDouble = 33.3800;

            var company3 = ctx.Companies.Create();
            company3.Name = "Harry Blades";
            company3.Address = "Christmas Steps, Bristol, UK";
            company3.DateFormed = hbFormed;
            company3.SomeDecimal = 34.3800M;
            company3.SomeDouble = 34.3800;

            ctx.SaveChanges();

                //skills

                for (int i = 0; i < 10; i++)
                {
                    var s = ctx.Skills.Create();
                    s.Name = "Skill" + i;
                    Skills.Add(s);
                }
            ctx.SaveChanges();

            //add some child/parent skill relationships
            //first skill has 5 children, so each of those will have a parent. Leaves 5 skills as standalone
            var parentskill = Skills[0];
            foreach(var skill in ctx.Skills)
            {
                if(skill.Id.Equals(parentskill.Id)) continue;
                parentskill.Children.Add(skill);
            }
            ctx.SaveChanges();

            //persons
            var people = new List<IPerson>();
            for (int i = 0; i < 10; i++)
            {
                var p = ctx.Persons.Create();
                p.Name = "Person" + i;
                p.Salary = 27000 + (i * 400);
                p.DateOfBirth = DateTime.Now;
                p.EmployeeId = i;
                p.Age = 20 + (i / 20);
                p.CollectionOfStrings = new string[] { i.ToString(), i + 1.ToString(), i + 2.ToString() };
                var s = Skills[i];
                p.Skills.Add(s);
                people.Add(p);
            }

            ctx.SaveChanges();
            
            //departments
            var depts = new List<IDepartment>();
            for (int i = 0; i < 2; i++)
            {
                var dep = ctx.Departments.Create();
                dep.Name = "Department" + i;
                dep.DeptId = i;
                depts.Add(dep);
            }
            ctx.SaveChanges();

            var roles = new List<IJobRole>();
            //Create Roles
            var jobRoledevelopment = ctx.JobRoles.Create();
            jobRoledevelopment.Description = "development";
            roles.Add(jobRoledevelopment);

            var jobRolesales = ctx.JobRoles.Create();
            jobRolesales.Description = "sales";
            roles.Add(jobRolesales);

            var jobRolemarketing = ctx.JobRoles.Create();
            jobRolemarketing.Description = "marketing";
            roles.Add(jobRolemarketing);

            var jobRolemanagement = ctx.JobRoles.Create();
            jobRolemanagement.Description = "management";
            roles.Add(jobRolemanagement);

            var jobRoleadministration = ctx.JobRoles.Create();
            jobRoleadministration.Description = "administration";
            roles.Add(jobRoleadministration);

            ctx.SaveChanges();

            #region depts
            //5 employees per department
            int e = 0; int d = 0;
            var peopleLookups = new List<double>();
            while (e < 10)
            {
                var department = depts.Where(de => de.DeptId == d).First();

                var p = people.Where(pe => pe.EmployeeId == e).First();
                p.Department = department;
                p.JobRole = roles[0];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeId == e).First();
                p.Department = department;
                p.JobRole = roles[1];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeId == e).First();
                p.Department = department;
                p.JobRole = roles[2];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeId == e).First();
                p.Department = department;
                p.JobRole = roles[3];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeId == e).First();
                p.Department = department;
                p.JobRole = roles[4];
                e = e + 1;

                d = d + 1;

            }
            ctx.SaveChanges();

            #endregion

            //Website website1 = ctx.Websites.Create();
            //website1.Name = "website1";
            //website1.URL = "http://website1.com";

            //Website website2 = ctx.Websites.Create();
            //website2.Name = "website2";
            //website2.URL = "http://website2.com";

            //ctx.SaveChanges();

            #region articles
            //articles
            for (int i = 0; i < 100; i++)
            {
                var art = ctx.Articles.Create();
                art.Title = "Article" + i;
                art.BodyText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis feugiat eleifend tempus. Maecenas mattis luctus volutpat. Morbi id felis diam. Morbi pulvinar tortor id nisl sagittis at bibendum turpis volutpat. Nulla gravida, elit et lobortis imperdiet, nulla sapien semper elit, eget posuere ipsum libero ac mi. Phasellus in risus nisi, sed aliquet nunc. Nunc auctor, justo non tristique dictum, lacus ligula pulvinar mauris, a sagittis lacus lectus id quam. Vivamus eget ante elit. Phasellus at hendrerit nunc. Vestibulum ac vehicula neque. Suspendisse ullamcorper scelerisque erat, non lacinia purus pretium quis. Praesent lacinia pellentesque ante id faucibus. Nunc eu enim eget erat convallis pulvinar. Curabitur ornare nisi dapibus massa sagittis non dictum augue pellentesque.Aliquam auctor, libero eu lobortis adipiscing, lorem nunc varius ante, at auctor sapien nunc sed augue. Phasellus ac leo nibh. Vivamus elit odio, accumsan at semper et, vestibulum a magna. Fusce pretiu";
                var p = people.Where(guy => guy.EmployeeId == (i / 10)).SingleOrDefault();
                //var p = people.Where(guy => guy.EmployeeNumber == i).SingleOrDefault();

                art.Publisher = p;
                //if ((i % 2) == 0)
                //{
                //    art.Website = website1;
                //}
                //else
                //{
                //    art.Website = website2;
                //}
            }

            ctx.SaveChanges();
            #endregion

            CreateDataTypeTestData();
            CreateProjects();
        }

      
    }
}
