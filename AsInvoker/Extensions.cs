using System.Text;

namespace AsInvoker
{
    static class Extensions
    {
        public static string ToUtf8NoBom(this byte[] data)
        {
            var bomUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            var xml = Encoding.UTF8.GetString(data);
            if (xml.StartsWith(bomUtf8))
            {
                return xml.Replace(bomUtf8, "");
            }
            return xml;
        }
    }
}
