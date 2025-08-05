using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Bars;
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

        public static Key RealKey(this KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.System:
                    return e.SystemKey;

                case Key.ImeProcessed:
                    return e.ImeProcessedKey;

                case Key.DeadCharProcessed:
                    return e.DeadCharProcessedKey;

                default:
                    return e.Key;
            }
        }

        public static ImageSource IconBitmap(this string uriString, double size)
        {
            BitmapDecoder decoder = IconBitmapDecoder.Create(new Uri(uriString), BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
            List<BitmapFrame> frames = decoder.Frames.OrderBy(x => x.Width).ToList();

            ImageSource imageSource = frames.FirstOrDefault(x => x.Width >= size * Dpi);
            if (imageSource == null)
                imageSource = frames.Last();

            return imageSource;
        }

        public static double Dpi
        {
            get
            {
                if (dpi == 0)
                {
                    using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        dpi = g.DpiX / 96;
                    }
                }

                return dpi;
            }
        }
        private static double dpi = 0;
    }

    public static class IconGlyph
    {
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.RegisterAttached("Size", typeof(double), typeof(IconGlyph), new PropertyMetadata(0.0, SourcePropertyChanged));

        public static double GetSize(DependencyObject obj)
        {
            return (double)obj.GetValue(SizeProperty);
        }

        public static void SetSize(DependencyObject obj, double value)
        {
            obj.SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(string), typeof(IconGlyph), new PropertyMetadata(null, SourcePropertyChanged));

        public static string GetSource(DependencyObject obj)
        {
            return (string)obj.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject obj, string value)
        {
            obj.SetValue(SourceProperty, value);
        }

        private static void SourcePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            string uriString = String.Format(GlyphPath, "ICO", e.NewValue?.ToString());
            if (obj is BitmapImage bitmap)
            {
                bitmap.UriSource = new Uri(uriString);
                return;
            }

            double width = 16;
            double size = GetSize(obj);
            if (size > 0)
                width = size;

            switch (obj)
            {
                case GalleryItem galleryItem:
                    galleryItem.Glyph = uriString.IconBitmap(galleryItem.Group.Gallery.ItemGlyphSize.Width);
                    break;

                case BarItem barItem:
                    barItem.Glyph = uriString.IconBitmap(width);
                    barItem.MediumGlyph = uriString.IconBitmap(width * 1.5);
                    barItem.LargeGlyph = barItem.GlyphSize != GlyphSize.Small ? uriString.IconBitmap(width * 2.0) : null;
                    
                    break;

                case System.Windows.Controls.Image image:
                    image.Width = width;
                    image.Height = width;
                    image.Source = uriString.IconBitmap(width);
                    break;
            }
        }

        private const string GlyphPath = "pack://application:,,,/Assets/{0}/{1}.{0}";
    }
}
