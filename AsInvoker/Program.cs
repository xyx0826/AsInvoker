using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using static AsInvoker.Interop;

namespace AsInvoker
{
    class Program
    {
        #region Fields
        // The name of the module
        private static string _fileName;

        // The name (usually an integer) of the manifest
        private static IntPtr _manifestName;

        // The extracted or default application manifest
        private static XmlDocument _manifest;

        private static bool _manifestFound;
        #endregion

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                // Module not specified
                PrintHelp();
                return;
            }

            var fileName = args[0];
            if (!File.Exists(fileName))
            {
                // Module doesn't exist
                PrintHelp();
                Console.WriteLine($"Error: the specified file {fileName} does not exist.");
                return;
            }

            _fileName = fileName;
            DeEscalate();
        }

        static void PrintHelp()
        {
            Console.WriteLine("AsInvoker executable de-escalation tool");
            Console.WriteLine("Usage: AsInvoker.exe exe_to_deescalate.exe");
        }

        /// <summary>
        /// Called by <see cref="EnumResourceNames(IntPtr, ResourceType, EnumResNameProc, IntPtr)"/>.
        /// </summary>
        /// <param name="hModule">The handle to the module.</param>
        /// <param name="lpszType">The type of the resource.</param>
        /// <param name="lpszName">The name of the resource.</param>
        /// <param name="lParam">Additional parameters (null).</param>
        /// <returns></returns>
        static bool EnumResourceNameCallback(IntPtr hModule, ResourceType lpszType, IntPtr lpszName, IntPtr lParam)
        {
            // Load the resource
            var hResInfo = FindResource(hModule, lpszName, lpszType);
            var cbResource = SizeofResource(hModule, hResInfo);
            var hResData = LoadResource(hModule, hResInfo);
            var pResource = LockResource(hResData);

            // Read the manifest into a XmlDocument
            var manifest = new byte[cbResource];
            Marshal.Copy(pResource, manifest, 0, (int)cbResource);
            _manifest = new XmlDocument();
            _manifest.LoadXml(manifest.ToUtf8NoBom());
            _manifestName = lpszName;

            _manifestFound = true;
            return false;   // stop enumeration
        }

        //static bool EnumResourceLanguageCallback(IntPtr hModule, ResourceType lpszType, IntPtr lpszName, ushort wIdLanguage, IntPtr lParam)
        //{
        //    return true;    // keep enumerating
        //}

        /// <summary>
        /// Load the module, find manifests, and get them patched.
        /// </summary>
        static void DeEscalate()
        {
            // Load the module
            var hModule = LoadLibraryEx(_fileName, IntPtr.Zero, LoadLibraryFlags.AsDatafile);
            if (hModule == IntPtr.Zero)
            {
                Console.WriteLine($"Error: LoadLibraryEx error {Marshal.GetLastWin32Error()}");
                return;
            }

            // Try to find the manifest resource
            EnumResourceNames(hModule, ResourceType.Manifest, EnumResourceNameCallback, IntPtr.Zero);

            // Release module handle and patch the manifest
            if (!FreeLibrary(hModule))
            {
                Console.WriteLine($"Error: FreeLibrary error {Marshal.GetLastWin32Error()}");
                return;
            }

            PatchManifest();
        }

        /// <summary>
        /// Modifies the retrieved (or default) manifest to remove administrator requests.
        /// </summary>
        static void PatchManifest()
        {
            if (!_manifestFound)
            {
                Console.WriteLine("No manifest found. Creating a default one.");
                _manifest = new XmlDocument();
                _manifest.LoadXml(Resources.DefaultManifest);
                _manifestName = (IntPtr)1;
            }

            // assembly.trustInfo.security.requestedPrivileges.requestedExecutionLevel
            var elems = _manifest.GetElementsByTagName("requestedExecutionLevel");
            foreach (XmlNode elem in elems)
            {
                // Patch each requestedExecutionLevel (should be only one)
                foreach (XmlAttribute attr in elem.Attributes)
                {
                    if (attr.Name == "level" && attr.Value == "requireAdministrator")
                    {
                        // level="requireAdministrator" => level="asInvoker"
                        Console.WriteLine("Found level=requireAdministrator.");
                        attr.InnerText = "asInvoker";
                    }
                    if (attr.Name == "uiAccess" && attr.Value == "true")
                    {
                        // uiAccess="true" => uiAccess="false"
                        Console.WriteLine("Found uiAccess=true.");
                        attr.InnerText = "false";
                    }
                }
            }

            UpdateManifest();
        }

        /// <summary>
        /// Back up the module before patched.
        /// </summary>
        static void DoBackup()
        {
            var bak = _fileName + ".bak";
            if (File.Exists(bak))
            {
                Console.WriteLine($"Overwrite {bak}? [Yes/No] (Y)");
                var line = Console.ReadLine().ToUpper();
                if (line != "Y" && line != "YES")
                {
                    Console.WriteLine("No backup");
                    return;
                }
            }

            try
            {
                File.Copy(_fileName, bak, true);
                Console.WriteLine($"Backup {_fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Backup {_fileName} failed => {ex.Message}");
            }
        }

        /// <summary>
        /// Writes the patched manifest to the module.
        /// </summary>
        static void UpdateManifest()
        {
            DoBackup();

            var hUpdate = BeginUpdateResource(_fileName, false);
            var newManifest = Encoding.UTF8.GetBytes(_manifest.InnerXml);
            var newManifestHandle = GCHandle.Alloc(newManifest, GCHandleType.Pinned);
            var pNewManifest = newManifestHandle.AddrOfPinnedObject();
            if (UpdateResource(hUpdate, ResourceType.Manifest, _manifestName, 0, pNewManifest, (uint)newManifest.Length))
            {
                Console.WriteLine("Updated manifest resource.");
            }
            else
            {
                Console.WriteLine($"Error: UpdateResource create error {Marshal.GetLastWin32Error()}");
                newManifestHandle.Free();
                return;
            }
            if (EndUpdateResource(hUpdate, false))
            {
                Console.WriteLine("Saved changes.");
            }
            else
            {
                Console.WriteLine($"Error: EndUpdateResource error {Marshal.GetLastWin32Error()}");
            }

            newManifestHandle.Free();
        }
    }
}
