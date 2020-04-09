using System;
using System.Runtime.InteropServices;

namespace AsInvoker
{
    static class Interop
    {
        #region LoadLibrary
        public enum LoadLibraryFlags : uint
        {
            AsDatafile = 2
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);
        #endregion

        #region EnumResourceNames
        public enum ResourceType : uint
        {
            Manifest = 24
        }

        public delegate bool EnumResNameProc(IntPtr hModule, ResourceType lpszType, IntPtr lpszName, IntPtr lParam);

        public delegate bool EnumResLangProc(IntPtr hModule, ResourceType lpszType, IntPtr lpszName, ushort wIdLanguage, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern bool EnumResourceLanguages(IntPtr hModule, ResourceType lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern bool EnumResourceNames(IntPtr hModule, ResourceType dwID, EnumResNameProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, ResourceType lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll")]
        public static extern bool FreeResource(IntPtr hResData);
        #endregion

        #region UpdateResource
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr BeginUpdateResource(string pFileName, [MarshalAs(UnmanagedType.Bool)]bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UpdateResource(IntPtr hUpdate, ResourceType lpType, IntPtr lpName, ushort wLanguage, IntPtr lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool EndUpdateResource(IntPtr hUpdate, [MarshalAs(UnmanagedType.Bool)]bool fDiscard);
        #endregion
    }
}
