namespace BrightstarDB.Server.Modules.Model
{
    public interface IPagedResultModel
    {
        string FirstPageLink { get; set; }
        string PreviousPageLink { get; set; }
        string NextPageLink { get; set; }
        dynamic RequestProperties { get; set; }
    }
}