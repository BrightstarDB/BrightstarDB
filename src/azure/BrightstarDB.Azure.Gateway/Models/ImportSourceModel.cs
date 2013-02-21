using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BrightstarDB.Azure.Gateway.Models
{
    public class ImportSourceModel
    {
        [HiddenInput(DisplayValue = false)]
        public string StoreId { get; set; }

        [Required(ErrorMessage = "A source address must be specified")]
        [Display(Name = "Source Address", Description = "The URL from which the data will be imported")]
        public string SourceAddress { get; set; }

        [Display(Name="Use GZip", Description = "Check this box to read source data that is compressed with GZIP")]
        public bool UseGZip { get; set; }

        [Display(Name="Azure Storage Connection String",
            Description = "If the data is held in a non-public Azure blob, provide the full connection string for the storage account.")]
        public string StorageConnectionString { get; set; }
    }
}