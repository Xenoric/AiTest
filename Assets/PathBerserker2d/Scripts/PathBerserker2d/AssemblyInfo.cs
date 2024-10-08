using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PathBerserker2d.Editor")]
[assembly: InternalsVisibleTo("PathBerserker2d.Upgrade")]
[assembly: AssemblyVersion("2.1")]

internal static class AssemblyInfo
{
    public static string Version => typeof(AssemblyInfo).Assembly.GetName().Version.ToString();
}