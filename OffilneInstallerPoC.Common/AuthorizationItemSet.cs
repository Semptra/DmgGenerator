using System;
using System.Runtime.InteropServices;

namespace OfflineInstallerPoC.Common
{
	[StructLayout (LayoutKind.Sequential)]
	public class AuthorizationItemSet
	{
		public UInt32 Count;
		public IntPtr Items;

		public AuthorizationItemSet (AuthorizationItem [] items)
		{
			if (items == null) {
				Items = IntPtr.Zero;
				Count = 0;
			} else {
				Count = (UInt32)items.Length;
				Items = IntPtr.Zero;
				MarshalItems (items);
			}
		}

		void MarshalItems (AuthorizationItem [] items)
		{
			var itemSize = Marshal.SizeOf (typeof (AuthorizationItem));
			Items = Marshal.AllocHGlobal ((int)(itemSize * Count));
			var cur = Items;

			for (uint i = 0; i < Count; i++) {
				if (i > 0) {
					cur = IntPtr.Add (cur, itemSize);
				}

				var item = items [i];
				Marshal.StructureToPtr (item, cur, false);
			}
		}
	}
}