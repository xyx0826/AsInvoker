using System.Text;

namespace AsInvoker
{
    static class Extensions
    {
        // The UTF-8 Byte-Order Mark that we don't want
        private static readonly string Utf8Bom
            = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

        public static string ToUtf8NoBom(this byte[] data)
        {
            var s = Encoding.UTF8.GetString(data);
            if (s.StartsWith(Utf8Bom))
            {
                return s.Remove(0, Utf8Bom.Length);
            }
            return s;
        }
    }
}
