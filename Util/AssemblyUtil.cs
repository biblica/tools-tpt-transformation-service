using System;
using System.Reflection;

namespace TptMain.Util
{
    public static class AssemblyUtil
    {
        public static Version GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}
