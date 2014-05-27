using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Config
{
    /// <summary>
    /// Represents the contents of the "brightstar" configuration section in an app.config or
    /// web.config file.
    /// </summary>
    public class BrightstarConfiguration
    {
        /// <summary>
        /// Get or set the page cache preload configuration
        /// </summary>
        public PageCachePreloadConfiguration PreloadConfiguration { get; set; }
    }
}
