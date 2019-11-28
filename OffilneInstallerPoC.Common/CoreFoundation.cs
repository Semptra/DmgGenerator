using System;
using System.Runtime.InteropServices;

namespace OfflineInstallerPoC.Common
{
	public static class CoreFoundation
	{
		const string CFLib = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";

		[DllImport (CFLib)]
		static extern IntPtr CFStringCreateWithCString (IntPtr alloc, string str, int encoding);

		public static IntPtr CreateString (string s)
		{
			// The magic value is "kCFStringENcodingUTF8"
			return CFStringCreateWithCString (IntPtr.Zero, s, 0x08000100);
		}

		[DllImport (CFLib, CharSet = CharSet.Unicode)]
		public extern static int CFStringGetLength (IntPtr handle);
	}
}