namespace BrightstarDB.Polaris.ViewModel
{
    public class RdfFormatViewModel
    {
        public RdfFormat Format { get; private set; }
        public string DisplayName { get; private set; }

        public RdfFormatViewModel(RdfFormat fmt)
        {
            Format = fmt;
            DisplayName = $"{fmt.DisplayName} ({fmt.DefaultExtension})";
        }
    }
}