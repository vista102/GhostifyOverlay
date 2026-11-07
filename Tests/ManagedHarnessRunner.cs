using System;
using System.IO;
using System.Reflection;

// Runs managed harnesses under Mono without starting Unity or installing a mod.
internal static class ManagedHarnessRunner
{
    private static int Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                string name = new AssemblyName(eventArgs.Name).Name;
                // Use Mono's BCL, never Unity's incompatible mscorlib/System DLLs.
                if (name == "mscorlib" || name == "netstandard" || name == "System" || name.StartsWith("System.")) return null;
                string managed = Environment.GetEnvironmentVariable("GHOSTIFY_TEST_MANAGED");
                foreach (string directory in new[] { managed, Path.Combine(managed, "UnityModManager") })
                {
                    string path = Path.Combine(directory, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            var assembly = Assembly.LoadFrom(args[0]);
            var method = assembly.GetType(args[1], true).GetMethod("Run");
            var parameters = method.GetParameters();
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Assembly))
            {
                string managed = Environment.GetEnvironmentVariable("GHOSTIFY_TEST_MANAGED");
                Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
                Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp-firstpass.dll"));
            }
            object[] arguments = parameters.Length == 0 ? null : new[] {
                parameters[0].ParameterType == typeof(Assembly) ? (object)Assembly.LoadFrom(args[2]) : args[2]
            };
            var results = (string[])method.Invoke(null, arguments);
            Console.WriteLine(args[1] + ": " + results.Length + " checks passed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error is TargetInvocationException ? error.InnerException : error);
            return 1;
        }
    }
}
