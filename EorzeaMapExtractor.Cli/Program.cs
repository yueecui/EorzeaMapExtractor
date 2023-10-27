using SaintCoinach;
using SaintCoinach.Cmd;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EorzeaMapExtractor.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("No enough arguments given.");
                PrintUsageAndExit();
                return;
            }
            var gameDir = args[0];

            var realm = new ARealmReversed(gameDir, "SaintCoinach.History.zip", SaintCoinach.Ex.Language.ChineseSimplified, "app_data.sqlite");
            realm.Packs.GetPack(new SaintCoinach.IO.PackIdentifier("exd", SaintCoinach.IO.PackIdentifier.DefaultExpansion, 0)).KeepInMemory = true;

            var func = args[1];
            var destDir = args[2];

            Directory.CreateDirectory(destDir);

            switch (func)
            {
                case "csv":
                    ExportSheets(realm, destDir);
                    break;
                case "map":
                    ExportMaps(realm, destDir);
                    break;
                case "icon":
                    ExportIcons(realm, destDir);
                    break;
                default:
                    Console.WriteLine($"Given function '{func}' is not supported.");
                    PrintUsageAndExit();
                    return;
            }

#if DEBUG
            Console.WriteLine("Process finished.");
            Console.ReadKey();
#endif
        }

        static void ExportSheets(ARealmReversed realm, string dest)
        {
            var sheets = (new []{ "Map", "MapMarker", "MapMarkerRegion", "MapSymbol" }).Select(_ => realm.GameData.FixName(_));
            var success = 1;

            foreach (var name in sheets)
            {
                Console.WriteLine($"[{success}/{sheets.Count()}] Exporting {name} ...");
                var target = new FileInfo(Path.Combine(dest, $"{name}.csv"));
                if (!target.Directory.Exists)
                {
                    target.Directory.Create();
                }

                var sheet = realm.GameData.GetSheet(name);
                ExdHelper.SaveAsCsv(sheet, SaintCoinach.Ex.Language.None, target.FullName, false);
                success++;
            }
        }

        static void ExportMaps(ARealmReversed realm, string dest)
        {
            var format = ImageFormat.Png;
            var allMaps = realm.GameData.GetSheet<SaintCoinach.Xiv.Map>().Where(m => m.PlaceName != null);
            var totalMaps = allMaps.Count();
            var padLength = totalMaps.ToString().Length;
            var totalMapsStr = totalMaps.ToString().PadLeft(padLength);

            var count = 0;
            foreach (var map in allMaps)
            {
                count++;
                var fileName = map.Id.ToString().Replace("/", "_") + ".png";
                Console.Write($"[{count.ToString().PadLeft(padLength)}/{totalMapsStr}] Exporting Map :{fileName}: ...");
                var outFile = new FileInfo(Path.Combine(dest, fileName));
                if (outFile.Exists)
                {
                    Console.WriteLine(" already exists.");
                    continue;
                }

                if (!outFile.Directory.Exists)
                {
                    outFile.Directory.Create();
                }

                var img = map.MediumImage;
                if (img == null)
                {
                    Console.WriteLine(" no image found.");
                    continue;
                }

                img.Save(outFile.FullName, format);
                Console.WriteLine(" OK.");
            }
        }

        static void ExportIcons(ARealmReversed realm, string dest)
        {
            for (int i = 0; i < 999999; i++)
            {
                foreach (var version in new[] { "", "/hq" })
                {
                    var filePath = string.Format("ui/icon/{0:D3}000{1}/{2:D6}.tex", i / 1000, version, i);
                    var destPath = string.Format("{0:D3}000{1}/{2:D6}.png", i / 1000, version, i);
                    if (realm.Packs.TryGetFile(filePath, out var file))
                    {
                        if (file is SaintCoinach.Imaging.ImageFile imgFile)
                        {
                            var img = imgFile.GetImage();
                            var target = new FileInfo(Path.Combine(dest, destPath));
                            if (!target.Directory.Exists)
                            {
                                target.Directory.Create();
                            }

                            img.Save(target.FullName);
                        }
                    }
                }
            }
        }

        static void PrintUsageAndExit(int code = 1)
        {
            Console.WriteLine("Usage: extractor.exe [gameDir] [function] [destDir]");
            Environment.Exit(code);
        }
    }
}
