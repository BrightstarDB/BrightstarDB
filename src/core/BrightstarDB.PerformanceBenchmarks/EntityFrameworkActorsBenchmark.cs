using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.PerformanceBenchmarks.Models;
using BrightstarDB.Client;

namespace BrightstarDB.PerformanceBenchmarks
{
    public class EntityFrameworkFoafBenchmark : BenchmarkBase
    {
        private const int BatchSize = 1000;
        private IFoafPerson[] _last10 = new IFoafPerson[10];
        private string _storeConnectionString;
        private int _personCount;

        public override void Initialize(string connectionString, int testScale)
        {
            base.Initialize(connectionString, testScale);
            _storeConnectionString = connectionString + ";storeName=" + StoreName;
        }
        public override void Setup()
        {
            // Create IFoafPerson entities in batches of 1000
            // Create 10,000 entities per unit of test scale
            var start = DateTime.UtcNow;
            int cycleCount = TestScale*10;
            _personCount = cycleCount * BatchSize;
            for (var i = 0; i < cycleCount; i++)
            {
                CreateBatch(i);
            }
            var end = DateTime.UtcNow;
            Report.LogOperationCompleted("populate",
                                         String.Format("Create {0} person records with 10 foaf:knows links each",
                                                       _personCount),
                                         cycleCount, end.Subtract(start).TotalMilliseconds);
        }

        public override void RunMix()
        {
            TryOperation(LinqFindById, "linq-find-by-id", "Retrieve a single entity by its ID using a LINQ query.");
            TryOperation(SparqlFindById, "sparql-find-by-id",
                         "Retreive all properties of a single entity using a SPARQL query.");
            TryOperation(LinqFindByName, "linq-find-by-name",
                         "Retrieve all entities with a particular GivenName property value.");
        }

        public void TryOperation(Func<int> a, string name, string description)
        {
            try
            {
                DateTime start = DateTime.UtcNow;
                var cycleCount = a();
                var end = DateTime.UtcNow;
                Report.LogOperationCompleted(name, description, cycleCount, end.Subtract(start).TotalMilliseconds);
            }
            catch (Exception e)
            {
                Report.LogOperationException(name, description, e.ToString());
            }
        }

        public override void CleanUp()
        {
            // Nothing to clean up
        }

        private void CreateBatch(int batchNumber)
        {
            using (var context = new MyEntityContext(_storeConnectionString))
            {
                for (int i = 0; i < BatchSize; i++)
                {

                    CreatePerson(context, (batchNumber*BatchSize) + i);
                }
                context.SaveChanges();
            }
        }

        private void CreatePerson(MyEntityContext context, int personNumber)
        {
            var givenName = Firstnames[personNumber%Firstnames.Count];
            var familyName = Surnames[(personNumber/Firstnames.Count)%Surnames.Count];
            var p = new FoafPerson
                {
                    Id = personNumber.ToString(),
                    Name = givenName + " " + familyName,
                    GivenName = givenName,
                    FamilyName = familyName,
                    Organisation = Organizations[personNumber%Organizations.Count],
                    Age = 18 + (personNumber%60),
                };
            context.FoafPersons.Add(p);
            foreach (var friend in _last10)
            {
                if (friend != null)
                {
                    p.Knows.Add(friend);
                }
            }

            _last10[personNumber%10] = p;
        }

        #region Query Operations
        private int LinqFindById()
        {
            using (var context = new MyEntityContext(_storeConnectionString))
            {
                const int cycles = 1000;
                var rng = new Random();
                for (int i = 0; i < cycles; i++)
                {
                    var personId = rng.Next(_personCount);
                    var person = context.FoafPersons.FirstOrDefault(p => p.Id.Equals(personId.ToString()));
                    if (person == null)
                    {
                        throw new BenchmarkAssertionException("Expected LINQ query to return a non-null result.");
                    }
                }
                return cycles;
            }
            
        }

        private int SparqlFindById()
        {
            const int cycles = 1000;
            const string queryTemplate = "select * WHERE {{ <http://www.brightstardb.com/people/{0}> ?p ?o }}";
            var rng = new Random();
            for (var i = 0; i < cycles; i++)
            {
                var results = Service.ExecuteQuery(StoreName, String.Format(queryTemplate, rng.Next(_personCount)));
                XDocument resultsDoc = XDocument.Load(results);
                if (!resultsDoc.SparqlResultRows().Any())
                {
                    throw new BenchmarkAssertionException("Expected SPARQL query to return some rows.");
                }
            }
            return cycles;
        }

        private int LinqFindByName()
        {
            const int cycles = 1000;
            var rng = new Random();
            using (var context = new MyEntityContext(_storeConnectionString))
            {
                for (var i = 0; i < cycles; i++)
                {
                    var targetName = Firstnames[rng.Next(Firstnames.Count)];
                    var results = context.FoafPersons.Where(p => p.GivenName.Equals(targetName)).ToList();
                    if (results.Count == 0)
                    {
                        throw new BenchmarkAssertionException(
                            "Expected at least one result from LINQ query on GivenName");
                    }
                }
            }
            return cycles;
        }


        #endregion

        #region Names
        // Top 100 girls baby names 2012 and top 100 boys baby names 2012 (both from ONS)
        private static readonly List<string> Firstnames = new List<string>
            {

                "AARON",
                "ABIGAIL",
                "ADAM",
                "AIDEN",
                "AISHA",
                "ALEX",
                "ALEXANDER",
                "ALFIE",
                "ALICE",
                "AMBER",
                "AMELIA",
                "AMELIE",
                "AMY",
                "ANNA",
                "ANNABELLE",
                "ARCHIE",
                "ARTHUR",
                "AVA",
                "BAILEY",
                "BELLA",
                "BENJAMIN",
                "BETHANY",
                "BLAKE",
                "BOBBY",
                "BROOKE",
                "CAITLIN",
                "CALEB",
                "CALLUM",
                "CAMERON",
                "CHARLES",
                "CHARLIE",
                "CHARLOTTE",
                "CHLOE",
                "CONNOR",
                "DAISY",
                "DANIEL",
                "DARCEY",
                "DAVID",
                "DEXTER",
                "DYLAN",
                "EDWARD",
                "ELEANOR",
                "ELIJAH",
                "ELIZA",
                "ELIZABETH",
                "ELLA",
                "ELLIE",
                "ELLIOT",
                "ELLIOTT",
                "ELLIS",
                "ELSIE",
                "EMILIA",
                "EMILY",
                "EMMA",
                "ERIN",
                "ESME",
                "ETHAN",
                "EVA",
                "EVAN",
                "EVELYN",
                "EVIE",
                "FAITH",
                "FINLAY",
                "FINLEY",
                "FLORENCE",
                "FRANCESCA",
                "FRANKIE",
                "FREDDIE",
                "FREDERICK",
                "FREYA",
                "GABRIEL",
                "GEORGE",
                "GEORGIA",
                "GRACE",
                "GRACIE",
                "HANNAH",
                "HARLEY",
                "HARRIET",
                "HARRISON",
                "HARRY",
                "HARVEY",
                "HEIDI",
                "HENRY",
                "HOLLIE",
                "HOLLY",
                "HUGO",
                "IMOGEN",
                "ISAAC",
                "ISABEL",
                "ISABELLA",
                "ISABELLE",
                "ISLA",
                "ISOBEL",
                "IVY",
                "JACK",
                "JACOB",
                "JAKE",
                "JAMES",
                "JAMIE",
                "JASMINE",
                "JAYDEN",
                "JENSON",
                "JESSICA",
                "JOSEPH",
                "JOSHUA",
                "JUDE",
                "JULIA",
                "KAI",
                "KATIE",
                "KAYDEN",
                "KEIRA",
                "KIAN",
                "KYLE",
                "LACEY",
                "LAYLA",
                "LEAH",
                "LEO",
                "LEON",
                "LEWIS",
                "LEXI",
                "LIAM",
                "LILLY",
                "LILY",
                "LOGAN",
                "LOLA",
                "LOUIE",
                "LOUIS",
                "LUCA",
                "LUCAS",
                "LUCY",
                "LUKE",
                "LYDIA",
                "MADDISON",
                "MADISON",
                "MAISIE",
                "MARIA",
                "MARTHA",
                "MARYAM",
                "MASON",
                "MATILDA",
                "MATTHEW",
                "MAX",
                "MAYA",
                "MEGAN",
                "MIA",
                "MICHAEL",
                "MILLIE",
                "MOHAMMAD",
                "MOHAMMED",
                "MOLLIE",
                "MOLLY",
                "MUHAMMAD",
                "NATHAN",
                "NIAMH",
                "NOAH",
                "OLIVER",
                "OLIVIA",
                "OLLIE",
                "OSCAR",
                "OWEN",
                "PAIGE",
                "PHOEBE",
                "POPPY",
                "REUBEN",
                "RHYS",
                "RILEY",
                "ROBERT",
                "RORY",
                "ROSE",
                "ROSIE",
                "RUBY",
                "RYAN",
                "SAMUEL",
                "SARA",
                "SARAH",
                "SCARLETT",
                "SEBASTIAN",
                "SETH",
                "SIENNA",
                "SKYE",
                "SOFIA",
                "SONNY",
                "SOPHIA",
                "SOPHIE",
                "STANLEY",
                "SUMMER",
                "TAYLOR",
                "THEO",
                "THEODORE",
                "THOMAS",
                "TILLY",
                "TOBY",
                "TOMMY",
                "TYLER",
                "VIOLET",
                "WILLIAM",
                "WILLOW",
                "ZACHARY",
                "ZARA",
                "ZOE"
            };

        // Most common surnames in greater london area
        private static readonly List<string> Surnames = new List<string>
            {
                "Brown",
                "Smith",
                "Patel",
                "Jones",
                "Williams",
                "Johnson",
                "Taylor",
                "Thomas",
                "Roberts",
                "Khan",
                "Lewis",
                "Jackson",
                "Clarke",
                "James",
                "Phillips",
                "Wilson",
                "Ali",
                "Mason",
                "Mitchell",
                "Rose",
                "Davis",
                "Davies",
                "Rodriguez",
                "Cox",
                "Alexander",
                "Morgan",
                "Moore",
                "Mills",
                "King",
                "Adams",
                "Garcia",
                "White",
                "Stone",
                "Edwards",
                "Watson",
                "Malley",
                "Walker",
                "Austin",
                "Pearce",
                "Reid",
                "Simon",
                "Bennett",
                "Ahmed",
                "Thompson",
                "Power",
                "Mcdonald",
                "O'neill",
                "Singh",
                "Gill",
                "Young",
                "Saunders",
                "Lopez",
                "Ward",
                "Bull",
                "Kennedy",
                "Harris",
                "Price",
                "Evans",
                "Allen",
                "Green",
                "Bright",
                "Russell",
                "Mann",
                "Hall",
                "Kaur",
                "Dixon",
                "Ahmad",
                "Barker",
                "Fernandez",
                "Campbell",
                "Chapman",
                "Sheppard",
                "Harrison",
                "Giblin",
                "Palmer",
                "Dupont",
                "Woods",
                "Begum",
                "Murray",
                "Wright",
                "Arnold",
                "Wood",
                "Baker",
                "Mustafa",
                "George",
                "Richards",
                "Hill",
                "Scott",
                "Lynch",
                "Holt",
                "Mathey",
                "Donnelly",
                "Stubbs",
                "Dean",
                "Cole",
                "Islam",
                "Blanchet",
                "Doyle",
                "Sutton",
                "Ellis",
                "Barry",
                "Nelson",
                "Brennan",
                "Gilbert",
                "Booth",
                "Black",
                "Shah",
                "Hussein",
                "Hunt",
                "Knight",
                "Brady",
                "Pearson",
                "Banks",
                "Burrows",
                "Dervish",
                "Oliver",
                "Roche",
                "Kavanagh",
                "Bass",
                "Simons",
                "Adler",
                "Searle",
                "John",
                "Sanchez",
                "Turner",
                "Cooke",
                "Lane",
                "Pasfield",
                "Wilkinson",
                "Perry",
                "Blake",
                "Day",
                "Brooks",
                "Bouvet",
                "Kelliher",
                "Byrne",
                "Hussain",
                "Harper",
                "Moyon",
                "Jordan",
                "Adanet",
                "Daniel",
                "Cooper",
                "Gardner",
                "Love",
                "Farrell",
                "Chelmy",
                "Chan",
                "Michael",
                "May"
            };
        #endregion

        // Taken from FTSE-250 index (http://en.wikipedia.org/wiki/FTSE_250_Index)
        private static readonly List<string> Organizations = new List<string>
            {
                "3i Infrastructure",
                "Aberforth Smaller Companies Trust",
                "Afren",
                "African Barrick Gold",
                "Alent",
                "Al Noor Hospitals",
                "Alliance Trust",
                "AMEC",
                "Amlin",
                "AO World",
                "Ashmore Group",
                "WS Atkins",
                "Aveva",
                "BBA Aviation",
                "BH Global",
                "BH Macro",
                "BTG",
                "Balfour Beatty",
                "Bankers Investment Trust",
                "Bank of Georgia Holdings",
                "A.G. Barr",
                "Beazley Group",
                "Bellway",
                "Berendsen",
                "Berkeley Group Holdings",
                "Betfair Group",
                "Big Yellow Group",
                "BlackRock World Mining Trust",
                "Bluecrest Allblue Fund",
                "Bodycote",
                "Booker Group",
                "Bovis Homes Group",
                "Brewin Dolphin Holdings",
                "Brit",
                "British Empire Securities and General Trust",
                "Britvic",
                "N Brown Group",
                "Bwin.Party Digital Entertainment",
                "CSR",
                "Cable & Wireless Communications",
                "Cairn Energy",
                "Caledonia Investments",
                "Capital & Counties Properties",
                "Caracal Energy",
                "Carillion",
                "Carphone Warehouse",
                "Catlin Group",
                "Centamin",
                "Cineworld",
                "City of London Investment Trust",
                "Close Brothers Group",
                "Cobham",
                "Colt Group",
                "Computacenter",
                "Countrywide",
                "Cranswick",
                "Crest Nicholson",
                "Croda International",
                "Daejan Holdings",
                "Dairy Crest Group",
                "DCC",
                "De La Rue",
                "Debenhams",
                "Dechra Pharmaceuticals",
                "Derwent London",
                "Dignity",
                "Diploma",
                "Direct Line Group",
                "Dixons Retail",
                "Domino Printing Sciences",
                "Domino's Pizza",
                "Drax Group",
                "Dunelm Group",
                "Edinburgh Investment Trust",
                "Electra Private Equity",
                "Electrocomponents",
                "Elementis",
                "EnQuest",
                "Enterprise Inns",
                "Entertainment One",
                "Essentra",
                "Esure",
                "Euromoney Institutional Investor",
                "Evraz",
                "Exova",
                "F&C Commercial Property Trust",
                "Fenner",
                "Ferrexpo",
                "Fidelity China Special Situations",
                "Fidelity European Values",
                "Fidessa Group",
                "FirstGroup",
                "Fisher (James) & Sons",
                "Foreign & Colonial Investment Trust",
                "Foxtons",
                "Galliford Try",
                "Genesis Emerging Markets Fund",
                "Genus",
                "Go-Ahead Group",
                "Grafton Group",
                "Grainger",
                "Great Portland Estates",
                "Greencore",
                "Greene King",
                "HICL Infrastructure Company",
                "Halfords Group",
                "Halma",
                "Hansteen Holdings",
                "Hays",
                "HellermannTyton",
                "Henderson Group",
                "Hikma Pharmaceuticals",
                "Hiscox",
                "Home Retail Group",
                "Homeserve",
                "Howden Joinery",
                "Hunting",
                "ICAP",
                "IG Group Holdings",
                "IP Group",
                "ITE Group",
                "Imagination Technologies Group",
                "Inchcape",
                "Infinis Energy",
                "Informa",
                "Inmarsat",
                "Intermediate Capital Group",
                "International Personal Finance",
                "International Public Partnerships",
                "Interserve",
                "Investec",
                "JD Sports",
                "JPMorgan American Investment Trust",
                "JPMorgan Emerging Markets Investment Trust",
                "Jardine Lloyd Thompson",
                "John Laing Infrastructure Fund",
                "Jupiter Fund Management",
                "Just-Eat",
                "Just Retirement",
                "Kazakhmys",
                "Keller",
                "Kennedy Wilson Europe Real Estate",
                "Kentz Corporation",
                "Kier Group",
                "Ladbrokes",
                "Laird",
                "Lancashire Holdings",
                "Law Debenture",
                "LondonMetric Property",
                "Lonmin",
                "Man Group",
                "Marston's",
                "Melrose Industries",
                "Mercantile Investment Trust",
                "Merlin Entertainments",
                "Michael Page International",
                "Micro Focus International",
                "Millennium & Copthorne Hotels",
                "Mitchells & Butlers",
                "MITIE Group",
                "Moneysupermarket.com Group",
                "Monks Investment Trust",
                "Morgan Advanced Materials",
                "Murray International Trust",
                "National Express Group",
                "NB Global",
                "NMC Health",
                "Northgate",
                "Ocado Group",
                "Ophir Energy",
                "Oxford Instruments",
                "PZ Cussons",
                "Pace",
                "Paragon Group of Companies",
                "PayPoint",
                "Pennon Group",
                "Perform Group",
                "Perpetual Income & Growth Investment Trust",
                "Personal Assets Trust",
                "Petra Diamonds",
                "Pets at Home",
                "Phoenix Group Holdings",
                "Playtech",
                "Polar Capital Technology Trust",
                "Polymetal",
                "Poundland",
                "Premier Farnell",
                "Premier Oil",
                "Provident Financial",
                "QinetiQ",
                "RIT Capital Partners",
                "RPC Group",
                "RPS Group",
                "Rank Group",
                "Rathbone Brothers",
                "Redefine International",
                "Redrow",
                "Regus",
                "Renishaw",
                "Rentokil Initial",
                "Restaurant Group",
                "Rightmove",
                "Riverstone Energy",
                "Rotork",
                "SIG plc",
                "SVG Capital",
                "Savills",
                "Scottish Investment Trust",
                "Scottish Mortgage Investment Trust",
                "Segro",
                "Senior",
                "Serco",
                "Shaftesbury",
                "Smith (DS)",
                "SOCO International",
                "Spectris",
                "Spirax-Sarco Engineering",
                "Spirent",
                "St. Modwen Properties",
                "Stagecoach Group",
                "SuperGroup",
                "Synergy Health",
                "Synthomer",
                "TalkTalk Group",
                "Tate & Lyle",
                "Taylor Wimpey",
                "Ted Baker",
                "Telecity Group",
                "Telecom Plus",
                "Temple Bar Investment Trust",
                "Templeton Emerging Markets Investment Trust",
                "Thomas Cook Group",
                "TR Property Investment Trust (two listings, both ordinary & sigma shares)",
                "Tullett Prebon",
                "UBM",
                "UDG Healthcare",
                "UK Commercial Property Trust",
                "Ultra Electronics Holdings",
                "Unite Group",
                "Vedanta Resources",
                "Vesuvius",
                "Victrex",
                "W H Smith",
                "Wetherspoon (J D)",
                "William Hill",
                "Witan Investment Trust",
                "Wood Group",
                "Workspace Group",
                "Worldwide Healthcare Trust",
                "Xaar",
            };
    }
}
