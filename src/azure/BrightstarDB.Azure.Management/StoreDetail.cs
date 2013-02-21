using BrightstarDB.Azure.Management.Accounts;

namespace BrightstarDB.Azure.Management
{
    public class StoreDetail
    {
        public StoreDetail(){}
        public StoreDetail(IStore s)
        {
            Id = s.Id;
            SizeLimit = s.SizeLimit;
            CurrentSize = s.CurrentSize;
            Label = s.Label;
        }

        public string Id { get; set; }
        public int SizeLimit { get; set; }
        public int CurrentSize { get; set; }
        public string Label { get; set; }
    }
}