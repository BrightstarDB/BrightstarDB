using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace BrightstarDB.Portable.Store.Tests
{
    internal static class TestHelper
    {
        public static async void CopyFile(string fromPath, string targetFolderPath, string targetFileName)
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync(fromPath);
            IStorageFolder folder;
            try
            {
                folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(targetFolderPath);
            }
            catch (FileNotFoundException)
            {
                folder = null;
            }

            if (folder == null)
            {
                folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(targetFolderPath);
            }

            await file.CopyAsync(folder, targetFileName, NameCollisionOption.ReplaceExisting);
        }
    }
}
