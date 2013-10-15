using System.Collections.Generic;

namespace BrightstarDB.Dto
{
    /// <summary>
    /// A wrapper DTO containing only a list of stores
    /// </summary>
    public class StoresResponseModel
    {
        /// <summary>
        /// The store list
        /// </summary>
        public List<StoreResponseModel> Stores { get; set; }
    }
}
