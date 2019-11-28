using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace OfflineInstallerPoC.Common
{
    public static class ProcessWrapper
    {
        public static string Invoke(string filename,
                                    string arguments,
                                    int timeout = -1,
                                    CancellationTokenSource cancellationTokenSource = null)
        {
            return Invoke(filename, arguments, out _, timeout, cancellationTokenSource);
        }

        public static string Invoke(string filename,
                                    string arguments,
                                    out int exitCode,
                                    int timeout = -1,
                                    CancellationTokenSource cancellationTokenSource = null)
        {
            var output = new StringBuilder();
            using var cancellationToken = cancellationTokenSource ?? new CancellationTokenSource(timeout);
            using var stdoutCompleted = new ManualResetEvent(false);
            using var stderrCompleted = new ManualResetEvent(false);
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    ErrorDialog = false,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            };

            cancellationToken.Token.Register(() => {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Empty
                }
                finally
                {
                    process.Dispose();
                }
            });

            process.OutputDataReceived += (_, args) => {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
                else
                {
                    stdoutCompleted.Set();
                }
            };

            process.ErrorDataReceived += (_, args) => {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
                else
                {
                    stderrCompleted.Set();
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            // It is crucial to wait for the output handlers to finish gathering data
            // or otherwise it may end up locking the calling process up
            stdoutCompleted.WaitOne(TimeSpan.FromSeconds(30));
            stderrCompleted.WaitOne(TimeSpan.FromSeconds(30));

            exitCode = process.ExitCode;

            return output.ToString();
        }

        public static void Invoke(string filename,
                                  string arguments,
                                  Action<string> lineProcessor)
        {
            using var stdoutCompleted = new ManualResetEvent(false);
            using var stderrCompleted = new ManualResetEvent(false);
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    ErrorDialog = false,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            };

            process.OutputDataReceived += (_, args) => {
                if (args.Data != null)
                {
                    lineProcessor(args.Data);
                }
                else
                {
                    stdoutCompleted.Set();
                }
            };

            process.ErrorDataReceived += (_, args) => {
                if (args.Data != null)
                {
                    lineProcessor(args.Data);
                }
                else
                {
                    stderrCompleted.Set();
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            // It is crucial to wait for the output handlers to finish gathering data
            // or otherwise it may end up locking the calling process up
            stdoutCompleted.WaitOne(TimeSpan.FromSeconds(30));
            stderrCompleted.WaitOne(TimeSpan.FromSeconds(30));
        }
    }
}
