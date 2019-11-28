using System;

namespace OfflineInstallerPoC.Common
{
	[Flags]
	public enum AuthorizationFlags : uint
	{
		Defaults           = 0,
		InteractionAllowed = 1 << 0,
		ExtendedRights     = 1 << 1,
		PartialRights      = 1 << 2,
		DestroyRights      = 1 << 3,
		PreAuthorize       = 1 << 4,
		FlagNoData         = 1 << 20
	}
}