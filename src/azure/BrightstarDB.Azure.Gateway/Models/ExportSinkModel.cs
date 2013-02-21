using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BrightstarDB.Azure.Gateway.Models
{
    public class ExportSinkModel
    {
        [HiddenInput(DisplayValue = false)]
        public string StoreId { get; set; }

        [Required(ErrorMessage = "An Azure Blob Container address must be specified")]
        [Display(Name = "Container Address", Description = "The URL of the blob container that the data will be exported to")]
        public string ContainerAddress { get; set; }

        [Required(ErrorMessage = "Please provide a name for the blob to be written")]
        [Display(Name = "Blob Name",
            Description = "The name of the blob to write data to. If this blob already exists, it will be overwritten.")
        ]
        public string BlobName { get; set; }

        [Display(Name = "Compress With GZip", Description = "Check this box to compress the exported data with GZIP")]
        public bool UseGZip { get; set; }

        [Required(ErrorMessage = "Please provide the connection string to use to access the target blob container")]
        [Display(Name = "Azure Storage Connection String",
            Description = "Provide the full connection string for the storage account that holds the blob container.")]
        public string StorageConnectionString { get; set; }
    }
}