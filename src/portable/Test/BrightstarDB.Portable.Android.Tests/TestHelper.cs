using System;
using System.IO;
using Android;
using Android.Content.Res;

namespace BrightstarDB.Portable.Android.Tests
{
	internal static class TestHelper
	{
		public static MainActivity Context { get; set; }

		public static void CopyFile(string fromPath, string targetFolderPath, string targetFileName)
		{
			if (!Directory.Exists (targetFolderPath)) {
				Directory.CreateDirectory (targetFolderPath);
			}
			using (var input = TestHelper.Context.Assets.Open (fromPath)) {
				using (var output = new FileStream (Path.Combine (targetFolderPath, targetFileName), FileMode.Create)) {
					input.CopyTo (output);
				}
			}
		}
	}
}

