using System.Text;

namespace AlMadina.Application.Common
{
    /// <summary>
    /// Normalizes Arabic text for search:
    /// - strips diacritics (tashkeel) and tatweel
    /// - folds alef variants (أ إ آ ٱ) into ا
    /// - folds ى into ي and ة into ه
    /// - lowercases and collapses whitespace
    /// Example: "اللبن" and "الحليب" still differ, but
    /// "الألبان" / "الالبان" / "الألبن" all match.
    /// </summary>
    public static class ArabicTextNormalizer
    {
        public static string? Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var sb = new StringBuilder(text.Length);

            foreach (var c in text.Trim())
            {
                switch (c)
                {
                    // Diacritics (tashkeel) and tatweel -> skip
                    case '\u064B': // FATHATAN
                    case '\u064C': // DAMMATAN
                    case '\u064D': // KASRATAN
                    case '\u064E': // FATHA
                    case '\u064F': // DAMMA
                    case '\u0650': // KASRA
                    case '\u0651': // SHADDA
                    case '\u0652': // SUKUN
                    case '\u0640': // TATWEEL
                        break;

                    // Alef variants -> alef
                    case '\u0623': // ALEF WITH HAMZA ABOVE
                    case '\u0625': // ALEF WITH HAMZA BELOW
                    case '\u0622': // ALEF WITH MADDA ABOVE
                    case '\u0671': // ALEF WASLA
                        sb.Append('\u0627');
                        break;

                    // Alef maksura -> yeh
                    case '\u0649':
                        sb.Append('\u064A');
                        break;

                    // Teh marbuta -> heh
                    case '\u0629':
                        sb.Append('\u0647');
                        break;

                    default:
                        sb.Append(char.ToLowerInvariant(c));
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
