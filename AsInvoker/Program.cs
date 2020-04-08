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
        private static string _fileName;
        private static IntPtr _manifestName;
        private static XmlDocument _manifest;

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintHelp();
                return;
            }

            var fileName = args[0];
            if (!File.Exists(fileName))
            {
                PrintHelp();
                Console.WriteLine($"Error: the specified file {fileName} does not exist.");
                return;
            }

            DeEscalate(fileName);
        }

        static void PrintHelp()
        {
            Console.WriteLine("AsInvoker executable de-escalation tool");
            Console.WriteLine("Usage: AsInvoker.exe exe_to_deescalate.exe");
        }

        static bool EnumResourceNameCallback(IntPtr hModule, ResourceType lpszType, IntPtr lpszName, IntPtr lParam)
        {
            var hResInfo = FindResource(hModule, lpszName, lpszType);
            var cbResource = SizeofResource(hModule, hResInfo);
            var hResData = LoadResource(hModule, hResInfo);
            var pResource = LockResource(hResData);

            // Read the manifest into a XmlDocument
            var manifest = new byte[cbResource];
            Marshal.Copy(pResource, manifest, 0, (int)cbResource);
            _manifest = new XmlDocument();
            _manifest.LoadXml(Encoding.UTF8.GetString(manifest));
            _manifestName = lpszName;
            return false;   // stop enumeration
        }

        static void PatchManifest()
        {
            // assembly.trustInfo.security.requestedPrivileges.requestedExecutionLevel
            var elems = _manifest.GetElementsByTagName("requestedExecutionLevel");
            if (elems.Count == 0)
            {
                Console.WriteLine("Error: the executable doesn't seem to have requested administrator privilege.");
                return;
            }

            // Patch each requestedExecutionLevel (should be only one)
            foreach (XmlNode elem in elems)
            {
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

            // Update manifest resource
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

        static void DeEscalate(string fileName)
        {
            _fileName = fileName;
            var hLib = LoadLibraryEx(fileName, IntPtr.Zero, LoadLibraryFlags.AsDatafile);
            EnumResourceNames(hLib, ResourceType.Manifest, EnumResourceNameCallback, IntPtr.Zero);
            if (!FreeLibrary(hLib))
            {
                Console.WriteLine($"Error: FreeLibrary error {Marshal.GetLastWin32Error()}");
            }

            if (_manifest != null)
            {
                PatchManifest();
            }
        }
    }
}
