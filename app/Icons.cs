using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DwnoEvoHelper;

/// Digimon-Bilder, mitgeliefert in der EXE (Resources\icons.zip, 128 px, Name = Icon-Code wie "a001").
public static class Icons
{
    static readonly Dictionary<string, ImageSource?> Cache = new();
    static ZipArchive? _zip;

    static ZipArchive Zip => _zip ??= new ZipArchive(
        Assembly.GetExecutingAssembly().GetManifestResourceStream("icons.zip")!, ZipArchiveMode.Read);

    public static ImageSource? Get(string? code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        if (Cache.TryGetValue(code!, out var img)) return img;

        ImageSource? result = null;
        try
        {
            var entry = Zip.GetEntry(code + ".jpg");
            if (entry != null)
            {
                using var ms = new MemoryStream();
                using (var s = entry.Open()) s.CopyTo(ms);
                ms.Position = 0;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                result = bmp;
            }
        }
        catch
        {
            // fehlendes/defektes Bild → Platzhalter
        }
        Cache[code!] = result;
        return result;
    }
}
