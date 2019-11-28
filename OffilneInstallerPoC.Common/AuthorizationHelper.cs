using System;
using System.Linq;

namespace OfflineInstallerPoC.Common
{
    public static class AuthorizationHelper
    {
        static AuthorizationServices authorizationServices;

        public static string Run(bool needsPrivileges, out int exitCode, string command, params string[] args)
        {
            return Run<object>(null, null, needsPrivileges, out exitCode, command, args);
        }

        public static string Run<T>(Action<string, T> lineProcessor, T state, bool needsPrivileges, out int exitCode, string command, params string[] args)
        {
            return AuthorizationHelper.RunWithPrivileges(lineProcessor, state, needsPrivileges, out exitCode, command, args);
        }

        public static string RunWithPrivileges(out int exitCode, string command, params string[] args)
        {
            return RunWithPrivileges<object>(null, null, true, out exitCode, command, args);
        }

        public static string RunWithPrivileges<T>(Action<string, T> lineProcessor, T state, bool needsPrivileges, out int exitCode, string command, params string[] args)
        {
            if (args != null && args.Any())
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Any(arg => char.IsWhiteSpace(arg)))
                    {
                        args[i] = $"'{args[i]}'";
                    }
                }
            }

            var allargs = string.Join(" ", args);
            exitCode = int.MinValue;
            string output;

            if (needsPrivileges)
            {
                Console.WriteLine($"Running '{command} {allargs}' with privileges");
                if (authorizationServices == null)
                {
                    Console.WriteLine("Preauthenticating the command");
                    authorizationServices = new AuthorizationServices("Visual Studio Installer needs to install software with administrative privileges.");
                }

                output = authorizationServices.Run(lineProcessor, state, true, out exitCode, command, args);
            }
            else
            {
                Console.WriteLine($"Running '{command} {allargs}' without root privileges");
                output = ProcessWrapper.Invoke(command, allargs, out exitCode);
            }

            if (exitCode != 0)
            {
                Console.WriteLine($"Command '{command}' failed with error code '{exitCode}'. Output: {output}");
            }

            return output;
        }
    }
}
