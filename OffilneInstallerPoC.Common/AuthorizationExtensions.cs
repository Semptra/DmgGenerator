using System;

namespace OfflineInstallerPoC.Common
{
	public static class AuthorizationExtensions
	{
		public static string ToMessage (this AuthorizationError error)
		{
			switch (error) {
				case AuthorizationError.Success:
					return "Success";
				case AuthorizationError.InvalidSet:
					return "The authorization rights are invalid.";
				case AuthorizationError.InvalidRef:
					return "The authorization reference is invalid.";
				case AuthorizationError.InvalidTag:
					return "The authorization tag is invalid.";
				case AuthorizationError.InvalidPointer:
					return "The returned authorization is invalid.";
				case AuthorizationError.Denied:
					return "The authorization was denied.";
				case AuthorizationError.Canceled:
					return "The authorization was cancelled by the user.";
				case AuthorizationError.InteractionNotAllowed:
					return "The authorization was denied since no user interaction was possible.";
				case AuthorizationError.Internal:
					return "Unable to obtain authorization for this operation.";
				case AuthorizationError.ExternalizeNotAllowed:
					return "The authorization is not allowed to be converted to an external format.";
				case AuthorizationError.InternalizeNotAllowed:
					return "The authorization is not allowed to be created from an external format.";
				case AuthorizationError.InvalidFlags:
					return "The provided option flag(s) are invalid for this authorization operation.";
				case AuthorizationError.ToolExecuteFailure:
					return "The specified program could not be executed.";
				case AuthorizationError.ToolEnvironmentError:
					return "An invalid status was returned during execution of a privileged tool.";
				case AuthorizationError.BadAddress:
					return "The requested socket address is invalid (must be 0-1023 inclusive).";

				default:
					return "Unknown";
			}
		}

		public static string ErrAuthorizationToMessage (this int error)
		{
			if (!Enum.IsDefined (typeof (AuthorizationError), error))
				return "Unknown";
			return ((AuthorizationError)error).ToMessage ();
		}

		public static bool AsErrAuthorization (this int error, out AuthorizationError authErr)
		{
			if (!Enum.IsDefined (typeof (AuthorizationError), error)) {
				authErr = AuthorizationError.Success;
				return false;
			}
			authErr = (AuthorizationError)error;
			return true;
		}
	}
}