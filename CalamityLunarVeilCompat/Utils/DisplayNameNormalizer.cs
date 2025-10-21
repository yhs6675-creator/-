using System.Globalization;
using System.Text;

namespace CLVCompat.Utils
{
    public static class DisplayNameNormalizer
    {
        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            s = s.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            s = s.Replace("·", " ").Replace("•", " ").Replace("・", " ");
            s = s.Replace("[", " ").Replace("]", " ")
                 .Replace("(", " ").Replace(")", " ")
                 .Replace("{", " ").Replace("}", " ")
                 .Replace(":", " ").Replace(";", " ").Replace("—", "-");

            var sb = new StringBuilder(s.Length);
            bool prevSpace = false;

            foreach (var ch in s)
            {
                bool space = char.IsWhiteSpace(ch);
                if (space)
                {
                    if (!prevSpace)
                        sb.Append(' ');
                }
                else
                {
                    sb.Append(ch);
                }

                prevSpace = space;
            }

            return sb.ToString().Trim();
        }
    }
}
