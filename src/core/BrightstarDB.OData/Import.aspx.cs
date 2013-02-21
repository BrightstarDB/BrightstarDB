using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.OData
{
    public partial class Import : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // make some data
            var context = new MyEntityContext();

            //var p1 = context.Persons.Create();
            //p1.Name = "Kal";

            //var s1 = context.Skills.Create();
            //s1.Name = "nosql";
            //p1.Skills.Add(s1);

            if (context.Persons.Count() > 0) return;

            for (var i = 0; i < 25; i++)
            {
                var employee = context.Persons.Create();
                employee.Name = Firstnames[i] + " " + Surnames[i];
                employee.Email = Firstnames[i].ToLower() + Surnames[i].ToLower() + "@company.com";
                employee.EmployeeNumber = i;
            }

            var it = context.Departments.Create();
            it.Name = "IT";
            for (var i = 0; i < 5; i++)
            {
                var empCode = i;
                var member = context.Persons.Where(emp => emp.EmployeeNumber.Equals(empCode)).FirstOrDefault();
                if (member == null) continue;
                it.Members.Add(member);
            }

            var marketing = context.Departments.Create();
            marketing.Name = "Marketing";
            for (var i = 5; i < 10; i++)
            {
                var empCode = i;
                var member = context.Persons.Where(emp => emp.EmployeeNumber.Equals(empCode)).FirstOrDefault();
                if (member == null) continue;
                marketing.Members.Add(member);
            }

            var sales = context.Departments.Create();
            sales.Name = "Sales";
            for (var i = 10; i < 20; i++)
            {
                var empCode = i;
                var member = context.Persons.Where(emp => emp.EmployeeNumber.Equals(empCode)).FirstOrDefault();
                if (member == null) continue;
                sales.Members.Add(member);
            }

            var cs = context.Departments.Create();
            cs.Name = "Customer Services";
            for (var i = 10; i < 20; i++)
            {
                var empCode = i;
                var member = context.Persons.Where(emp => emp.EmployeeNumber.Equals(empCode)).FirstOrDefault();
                if (member == null) continue;
                cs.Members.Add(member);
            }

            context.SaveChanges();
        }

        private static readonly List<string> Firstnames = new List<string>()
                                 {
                                     "Jen",
                                     "Kal",
                                     "Gra",
                                     "Andy",
                                     "Jessica",
                                     "Adam",
                                     "Trevor",
                                     "Morris",
                                     "Paul",
                                     "Jane",
                                     "Elliot",
                                     "Annie",
                                     "Rob",
                                     "Mark",
                                     "Tim",
                                     "Gemma",
                                     "Clare",
                                     "Anna",
                                     "Tessa",
                                     "Julia",
                                     "David",
                                     "Andrew",
                                     "Charlie",
                                     "Aled",
                                     "Alex"
                                 };
        private static readonly List<string> Surnames = new List<string>()
                                 {
                                     "Wilson",
                                     "Foster",
                                     "Green",
                                     "Fahy",
                                     "Goldsack",
                                     "Webb",
                                     "Fernley",
                                     "McKee",
                                     "Hughes",
                                     "Wong",
                                     "Sully",
                                     "Hague",
                                     "Boyce",
                                     "Pegeot",
                                     "Chappell",
                                     "East",
                                     "Tate",
                                     "Wade",
                                     "Lloyd",
                                     "Hopwseith",
                                     "Matthews",
                                     "Lacey",
                                     "Skipper",
                                     "Chandler",
                                     "Jones"
                                 };
    }
}