using System.IO;

static internal class TestUtils
{
    public static void ResetDirectory(string dir)
    {
        if (Directory.Exists(dir))
        {
            var dirinfo = new DirectoryInfo(dir);
            foreach(var f in dirinfo.GetFiles()) f.Delete();
            dirinfo.Delete(true);
        }
        Directory.CreateDirectory(dir);
    }
}