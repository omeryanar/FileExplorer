using System;
using System.Collections.Generic;
using static Vanara.PInvoke.Shell32;

namespace FileExplorer.Core
{
    public static class Extensions
    {
        public static readonly char[] InvalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();

        public static readonly char[] SearchWildcards = new char[] { '*', '?' };

        public static bool ContainsInvalidFileNameCharacters(this string path)
        {
            if (path == null)
                return false;

            return path.IndexOfAny(InvalidFileNameChars) != -1;
        }

        public static string RemoveInvalidFileNameCharacters(this string path)
        {
            if (path == null)
                return null;

            return String.Join(String.Empty, path.Split(InvalidFileNameChars));
        }

        public static string RemoveSearchWildcards(this string text)
        {
            if (text == null)
                return null;

            return String.Join(String.Empty, text.Split(SearchWildcards));
        }

        public static string ToggleCase(this string text)
        {
            if (text == null)
                return null;

            char[] characters = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
                characters[i] = Char.IsLower(text[i]) ? Char.ToUpperInvariant(text[i]) : Char.ToLowerInvariant(text[i]);

            return new String(characters);
        }

        public static string SentenceCase(this string text)
        {
            if (text == null)
                return null;

            char[] characters = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
                characters[i] = i == 0 ? Char.ToUpperInvariant(text[i]) : Char.ToLowerInvariant(text[i]);

            return new String(characters);
        }

        public static string TitleCase(this string text)
        {
            if (text == null)
                return null;

            char[] characters = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
                characters[i] = i == 0 || Char.IsWhiteSpace(text[i-1]) ? Char.ToUpperInvariant(text[i]) : Char.ToLowerInvariant(text[i]);

            return new String(characters);
        }

        public static string Join(this IEnumerable<String> values, string separator)
        {
            return String.Join(separator, values);
        }

        public static string[] Split(this string text, string separator)
        {
            return text?.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool OrdinalEquals(this string value1, string value2)
        {
            return String.Compare(value1, value2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool OrdinalContains(this string value1, string value2)
        {
            return value1.IndexOf(value2, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool OrdinalStartsWith(this string value1, string value2)
        {
            return value1.StartsWith(value2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool OrdinalEndsWith(this string value1, string value2)
        {
            return value1.EndsWith(value2, StringComparison.OrdinalIgnoreCase);
        }

        public static SHIL ToSHIL(this IconSize iconSize)
        {
            switch (iconSize)
            {
                case IconSize.Small:
                    return SHIL.SHIL_SMALL;
                case IconSize.Medium:
                    return SHIL.SHIL_LARGE;
                case IconSize.Large:
                    return SHIL.SHIL_EXTRALARGE;
                case IconSize.ExtraLarge:
                    return SHIL.SHIL_JUMBO;
                default:
                    return SHIL.SHIL_SMALL;
            }
        }
    }
}
