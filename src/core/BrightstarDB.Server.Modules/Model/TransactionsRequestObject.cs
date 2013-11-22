namespace BrightstarDB.Server.Modules.Model
{
    public class TransactionsRequestObject
    {
        public string StoreName { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}
