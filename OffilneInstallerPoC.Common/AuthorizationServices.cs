using System;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix.Native;

namespace OfflineInstallerPoC.Common
{
	public class AuthorizationServices : IDisposable
	{
		readonly AuthorizationFlags flags;
		readonly AuthorizationItemSet rights;
		readonly AuthorizationItemSet environment;

		IntPtr authorizationRef;
		bool authorized;

		// Docs: http://developer.apple.com/library/mac/#documentation/Security/Reference/authorization_ref/Reference/reference.html

		const string SecurityLib = "/System/Library/Frameworks/Security.framework/Versions/Current/Security";

		[DllImport (SecurityLib, EntryPoint = "AuthorizationCreate")]
		static extern int Create (AuthorizationItemSet rights, AuthorizationItemSet environment, AuthorizationFlags flags, out IntPtr authorization);

		[DllImport (SecurityLib, EntryPoint = "AuthorizationFree")]
		static extern int Free (IntPtr authorization, AuthorizationFlags flags);

		[DllImport (SecurityLib, EntryPoint = "AuthorizationCopyRights")]
		static extern int CopyRights (IntPtr authorization, AuthorizationItemSet rights, AuthorizationItemSet environment, AuthorizationFlags flags, IntPtr authorizedRights);

		// Note that this is deprecated as of OSX 10.7 - need to use BAS instead
		// TODO: switch to BAS or similar http://developer.apple.com/library/mac/#samplecode/BetterAuthorizationSample/Introduction/Intro.html#//apple_ref/doc/uid/DTS10004207

		[DllImport (SecurityLib, EntryPoint = "AuthorizationExecuteWithPrivileges", CharSet = CharSet.Ansi)]
		static extern int ExecuteWithPrivileges (IntPtr authorization,
							   string pathToTool,
							   AuthorizationFlags options,
							   string [] arguments,
							   ref IntPtr communicationsPipe);

		[DllImport (SecurityLib, EntryPoint = "AuthorizationExecuteWithPrivileges", CharSet = CharSet.Ansi)]
		static extern int ExecuteWithPrivileges (IntPtr authorization,
							   string pathToTool,
							   AuthorizationFlags options,
							   string [] arguments,
							   IntPtr communicationsPipe);

		public AuthorizationServices (string prompt)
		{
			flags = AuthorizationFlags.ExtendedRights |
				AuthorizationFlags.InteractionAllowed |
				AuthorizationFlags.PreAuthorize;
			int status = Create (null, null, flags, out authorizationRef);
			if (status != 0) {
				var message = $"Could not obtain authorization service reference. {status.ErrAuthorizationToMessage ()}";
				throw new InvalidOperationException (message);
			}

			if (authorizationRef == IntPtr.Zero)
				throw new InvalidOperationException ("Could not obtain authorization service reference. No obvious cause.");

			AuthorizationItem [] rightItems = { new AuthorizationItem (AuthorizationTags.AuthorizationRightExecute, null, 0) };
			rights = new AuthorizationItemSet (rightItems);

			if (!string.IsNullOrEmpty (prompt)) {
				AuthorizationItem [] envItems = { new AuthorizationItem (AuthorizationTags.AuthorizationEnvironmentPrompt, prompt, 0) };
				environment = new AuthorizationItemSet (envItems);
			} else {
				environment = null;
			}
		}

		~AuthorizationServices ()
		{
			Dispose (false);
		}

		public string Run<T> (Action<string, T> lineProcessor, T state, bool captureOutput, out int exitCode, string command, params string [] args)
		{
			if (authorizationRef == IntPtr.Zero)
				throw new InvalidOperationException ("Command cannot be executed because authorization service reference is missing.");

			if (string.IsNullOrEmpty (command))
				throw new ArgumentNullException (nameof (command));

			int status;
			if (!authorized) {
				status = CopyRights (authorizationRef, rights, environment, flags, IntPtr.Zero);
				if (status != 0) {
					throw new Exception($"Could not copy authorization rights. {status.ErrAuthorizationToMessage()}");
                }

				authorized = true;
			}

			string result;
			if (captureOutput) {
				var stream = IntPtr.Zero;
				try {
					status = ExecuteWithPrivileges (authorizationRef, command, AuthorizationFlags.Defaults, args, ref stream);
					result = ReadOutput (stream, lineProcessor, state);
				} finally {
					if (stream != IntPtr.Zero) {
						Syscall.fclose (stream);
					}
				}
			} else {
				status = ExecuteWithPrivileges (authorizationRef, command, AuthorizationFlags.Defaults, args, IntPtr.Zero);
				result = null;
			}

			Console.WriteLine ("Waiting for the child process to finish running with root privileges.");
			if (Syscall.wait (out int exit_status) == -1) {
				var errno = Syscall.GetLastError ();
				// On OSX 10.8+ things changed a bit - the child no longer exists when we
				// come back from reading the output, but if status == 0 and the error is
				// "no child" we can ignore it and proceed. This is safe for OSX versions
				// earlier than 10.8
				if (captureOutput && status == 0 && errno != Errno.ECHILD)
					throw new InvalidOperationException ($"Wait on child process ended with error. {Syscall.strerror (errno)}");
			}

			if (!Syscall.WIFEXITED (exit_status))
				throw new InvalidOperationException ("Child process did not terminate normally.");

			exitCode = Syscall.WEXITSTATUS (exit_status);
			if (status != 0) {
				var message = $"Could not execute command.. {status.ErrAuthorizationToMessage ()}";
				if (status.AsErrAuthorization (out AuthorizationError error) && error == AuthorizationError.Canceled) {
					throw new Exception (message);
				} else {
					throw new InvalidOperationException (message);
				}
			}

			return result;
		}

		string ReadOutput<T> (IntPtr stream, Action<string, T> lineProcessor, T state)
		{
			if (stream == IntPtr.Zero)
				return null;

			var stringBuilder = lineProcessor == null ? new StringBuilder () : null;
			var line = new StringBuilder (1024);
			do {
				if (Syscall.fgets (line, stream) == null || Syscall.ferror (stream) != 0)
					break;

				if (lineProcessor == null) {
					stringBuilder.Append (line.ToString ());
				} else {
					lineProcessor (line.ToString (), state);
				}

				line.Length = 0;
			} while (Syscall.feof (stream) == 0);

			return stringBuilder?.ToString ();
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (authorizationRef != IntPtr.Zero) {
				Free (authorizationRef, AuthorizationFlags.Defaults);
				authorizationRef = IntPtr.Zero;
			}
		}
	}
}