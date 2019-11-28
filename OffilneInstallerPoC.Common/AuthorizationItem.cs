using System;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix;

namespace OfflineInstallerPoC.Common
{
	[StructLayout (LayoutKind.Sequential, CharSet = CharSet.None, Pack = 2)]
	public struct AuthorizationItem
	{
		//[MarshalAs (UnmanagedType.LPStr)]
		public IntPtr Name;
		public UIntPtr ValueLength;
		public IntPtr Value;
		public uint Flags;

		public AuthorizationItem (string name, string value, uint flags)
		{
			Name = UnixMarshal.StringToHeap (name, Encoding.UTF8);
			Flags = flags;
			if (value == null) {
				Value = IntPtr.Zero;
				ValueLength = new UIntPtr (0);
			} else {
				var v = ToUtf8 (value);
				Value = CoreFoundation.CreateString (v);
				ValueLength = new UIntPtr ((uint)CoreFoundation.CFStringGetLength (this.Value));
			}
		}

		static string ToUtf8 (string s)
		{
			var bytes = Encoding.UTF8.GetBytes (s);
			return Encoding.UTF8.GetString (bytes);
		}
	}
}

