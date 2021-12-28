using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SMSMService
{
    public static class Native
    {
        /// <summary>Gets the full path of the given executable filename as if the user had entered this executable in a shell. If the filename can't be found by Windows, null is returned.</summary>
        /// <param name="exeName">The name of the executable to search for</param>
        /// <returns>The full path if successful, or null otherwise</returns>
        public static string? GetFullPathFromWindows(string exeName) // From https://stackoverflow.com/a/52435685
        {
            if (exeName.Length >= MAX_PATH) { throw new ArgumentException($"The executable name '{exeName}' must have less than {MAX_PATH} characters.", nameof(exeName)); }

            StringBuilder sb = new(exeName, MAX_PATH);
            return PathFindOnPath(sb, null) ? sb.ToString() : null;
        }

        // https://docs.microsoft.com/en-us/windows/desktop/api/shlwapi/nf-shlwapi-pathfindonpathw
        // https://www.pinvoke.net/default.aspx/shlwapi.PathFindOnPath
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[]? ppszOtherDirs);

        // from MAPIWIN.h :
        private const int MAX_PATH = 260;
    }
}
