using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.PerformanceTests.Model;

namespace BrightstarDB.PerformanceTests
{
    public class PerformanceTestBase
    {
        #region Store Setup
        protected static MyEntityContext CreateStore(string connectionString, string storeName)
        {
            var context = new MyEntityContext(connectionString);
            var personCount = context.Persons.Count();
            if (personCount > 0 && personCount != 1000)
            {
                // Store needs to be deleted and created again
                var client = BrightstarService.GetClient(connectionString);
                client.DeleteStore(storeName);
                client.CreateStore(storeName);
                context = new MyEntityContext(connectionString);
                personCount = 0;
            }

            if (personCount == 0)
            {
                CreateData(context);
            }
            return context;
        }


        private static void CreateData(MyEntityContext ctx)
        {
            //skills
            var skills = new List<Skill>();
            for (int i = 0; i < 1000; i++)
            {
                Skill s = ctx.Skills.Create();
                s.Title = "Skill" + i;
                skills.Add(s);
            }
            ctx.SaveChanges();

            //persons
            var people = new List<Person>();
            for (int i = 0; i < 1000; i++)
            {
                Person p = ctx.Persons.Create();
                p.Fullname = "Person" + i;
                p.Salary = 270000 + (i * 200);
                p.DateOfBirth = DateTime.Now;
                p.EmployeeNumber = i;
                p.Age = 20 + (i / 20);
                p.EmployeeNumber = i;
                Skill s = skills[i];
                p.Skills.Add(s);
                people.Add(p);
            }

            ctx.SaveChanges();

            //departments
            var depts = new List<Department>();
            for (int i = 0; i < 100; i++)
            {
                Department dep = ctx.Departments.Create();
                dep.Name = "Department" + i;
                dep.DeptId = i;
                depts.Add(dep);
            }
            ctx.SaveChanges();

            var roles = new List<JobRole>();
            //Create Roles
            JobRole jobRoledevelopment = ctx.JobRoles.Create();
            jobRoledevelopment.Description = "development";
            roles.Add(jobRoledevelopment);

            JobRole jobRolesales = ctx.JobRoles.Create();
            jobRolesales.Description = "sales";
            roles.Add(jobRolesales);

            JobRole jobRolemarketing = ctx.JobRoles.Create();
            jobRolemarketing.Description = "marketing";
            roles.Add(jobRolemarketing);

            JobRole jobRolemanagement = ctx.JobRoles.Create();
            jobRolemanagement.Description = "management";
            roles.Add(jobRolemanagement);

            JobRole jobRoleadministration = ctx.JobRoles.Create();
            jobRoleadministration.Description = "administration";
            roles.Add(jobRoleadministration);

            ctx.SaveChanges();

            #region depts
            //100 employees per department
            int e = 0; int d = 0;
            while (e < 1000)
            {
                Department department = depts.Where(de => de.DeptId == d).First();

                var p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[0];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[0];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[1];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[1];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[2];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[2];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[3];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[3];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[4];
                e = e + 1;

                p = people.Where(pe => pe.EmployeeNumber == e).First();
                p.Department = department;
                p.JobRole = roles[4];
                e = e + 1;

                d = d + 1;

            }
            ctx.SaveChanges();

            #endregion

            Website website1 = ctx.Websites.Create();
            website1.Name = "website1";
            website1.Url = "http://website1.com";

            Website website2 = ctx.Websites.Create();
            website2.Name = "website2";
            website2.Url = "http://website2.com";

            ctx.SaveChanges();

            #region articles
            //articles
            for (int i = 0; i < 10000; i++)
            {
                Article art = ctx.Articles.Create();
                art.Title = "Article" + i;
                art.BodyText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis feugiat eleifend tempus. Maecenas mattis luctus volutpat. Morbi id felis diam. Morbi pulvinar tortor id nisl sagittis at bibendum turpis volutpat. Nulla gravida, elit et lobortis imperdiet, nulla sapien semper elit, eget posuere ipsum libero ac mi. Phasellus in risus nisi, sed aliquet nunc. Nunc auctor, justo non tristique dictum, lacus ligula pulvinar mauris, a sagittis lacus lectus id quam. Vivamus eget ante elit. Phasellus at hendrerit nunc. Vestibulum ac vehicula neque. Suspendisse ullamcorper scelerisque erat, non lacinia purus pretium quis. Praesent lacinia pellentesque ante id faucibus. Nunc eu enim eget erat convallis pulvinar. Curabitur ornare nisi dapibus massa sagittis non dictum augue pellentesque.Aliquam auctor, libero eu lobortis adipiscing, lorem nunc varius ante, at auctor sapien nunc sed augue. Phasellus ac leo nibh. Vivamus elit odio, accumsan at semper et, vestibulum a magna. Fusce pretium massa sed velit rutrum elementum. Cras ut dui quis elit gravida cursus. Aliquam tempor, nunc vel aliquam gravida, tortor urna aliquam eros, sed venenatis purus libero quis nisl. Quisque vulputate ultricies mi, nec lacinia tellus vulputate eu. Sed laoreet lacinia erat vitae auctor. Duis posuere dictum gravida. Morbi urna felis, rutrum volutpat tempor eget, porttitor at diam. Cras at urna diam. Ut nisl lorem, bibendum sed aliquet a, suscipit at lorem. In scelerisque, lorem ac tincidunt ornare, neque diam volutpat purus, eleifend ultricies odio erat eu ligula. Cras diam nisl, porttitor at commodo vitae, blandit semper ipsum. Nam tempor mattis lacus ac tempor.Aliquam erat volutpat. Praesent sed ligula diam, non tristique metus. Duis accumsan est nec quam ullamcorper eget vestibulum ligula facilisis. Sed malesuada lectus sit amet arcu sollicitudin tempor. Nam quis leo massa. Sed feugiat odio vel neque dictum venenatis. Mauris fermentum egestas erat nec semper. Aliquam dolor est, viverra ac egestas in, egestas vitae ligula. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae;Vestibulum tempor mi et erat eleifend sit amet rhoncus enim dictum. Mauris ultricies tortor vitae orci porttitor convallis lacinia dui auctor. Nulla facilisi. Nulla ligula urna, pharetra vel lacinia vitae, blandit id nunc. Morbi sed lacus id turpis mollis tempus. Suspendisse scelerisque elit id lorem interdum non pellentesque nulla laoreet. Suspendisse id ipsum libero. Duis rhoncus tincidunt libero a feugiat. Nunc tempus mauris venenatis justo dapibus in ultrices tortor euismod. Vestibulum pellentesque ante eget purus pretium at congue erat iaculis. Duis purus urna, placerat ac luctus et, luctus et tortor. Donec eu felis purus.Nunc sed blandit mauris. Aliquam sit amet neque velit, eget consequat nisi. Nunc facilisis, ante et bibendum porta, odio libero facilisis nunc, eget hendrerit lectus augue scelerisque ante. Maecenas fermentum dictum mollis. Morbi scelerisque urna sed quam tincidunt vehicula. Sed pharetra venenatis tellus sed laoreet. Integer ante libero, placerat vitae dignissim sed, eleifend ac erat. Sed eleifend viverra justo, vitae fringilla felis semper sed. Nam iaculis sagittis augue, non blandit arcu viverra vel. Phasellus tellus felis, dictum vel imperdiet vitae, aliquet a erat. Sed tempor suscipit condimentum.";
                var p = people.Where(guy => guy.EmployeeNumber == (i / 10)).SingleOrDefault();

                art.Publisher = p;
                if ((i % 2) == 0)
                {
                    art.Website = website1;
                }
                else
                {
                    art.Website = website2;
                }
            }

            ctx.SaveChanges();
            #endregion
        }
        #endregion
    }
}