using System.IO;

namespace NetworkedPlanet.Brightstar.Polaris.ViewModel
{
    public class DirectoryStoreSource : StoreSource
    {
        private readonly DirectoryInfo _dir;

        public override bool IsLocal { get { return true; } }
        public override string Location
        {
            get { return _dir.FullName; }
        }

        public DirectoryStoreSource(string dirPath)
        {
            _dir = new DirectoryInfo(dirPath);
            foreach(var childDirectory in _dir.EnumerateDirectories())
            {
                var masterfile = Path.Combine(childDirectory.FullName, "masterfile.bs");
                if(File.Exists(masterfile))
                {
                    Stores.Add(new LocalStore(this, childDirectory.Name, childDirectory.FullName));
                }
            }
        }
    }
}