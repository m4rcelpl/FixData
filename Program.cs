using CoenM.ExifToolLib;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FixData
{
    internal class Program
    {
        private static readonly DateTime UnixStartDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static AsyncExifTool asyncExifTool;

        private static async System.Threading.Tasks.Task Main(string[] args)
        {
            try
            {
                string GoogleTakeoutPath = string.Empty;
                bool Repeat = true;
                bool ExifToolPresent = false;
                AsyncExifToolConfiguration config;

                string ExifToolPath = $"{Directory.GetCurrentDirectory()}\\exiftool.exe";

                if (File.Exists(ExifToolPath))
                {
                    // we need to tell AsyncExifTool where  exiftool executable is located.
                    var exifToolPath = ExifToolPath;

                    // What encoding should AsyncExifTool use to decode the resulting bytes
                    var exifToolResultEncoding = Encoding.UTF8;

                    // Construction of the ExifToolConfiguration
                    config = new AsyncExifToolConfiguration(exifToolPath, exifToolResultEncoding, null);

                    asyncExifTool = new AsyncExifTool(config);

                    try
                    {
                        asyncExifTool.Initialize();
                        var version = await asyncExifTool.ExecuteAsync(new[] { "-ver" });
                        Console.WriteLine($"Exiftool found, loaded version: {version}");
                        ExifToolPresent = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Exiftool found but got error. Try another version");
                        Console.WriteLine(ex);
                        await asyncExifTool.DisposeAsync();
                    }
                }
                else
                {
                    Console.WriteLine("Exiftool not found. If you want support to change date in metadata like EXIF, download tool on https://exiftool.org/ (Windows Executable) change name to exiftool.exe and copy to the same folder as FixData executable." + Environment.NewLine);
                }

                do
                {
                    Console.WriteLine("Type patch to folder from Google Takeout");
                    GoogleTakeoutPath = Console.ReadLine();

                    if (Directory.Exists(GoogleTakeoutPath))
                        Repeat = false;
                    else
                        Console.WriteLine($"Folder [{GoogleTakeoutPath}] not exist{Environment.NewLine}");
                } while (Repeat);

                Repeat = true;

                Console.WriteLine();
                Console.WriteLine("!WARNING!");
                Console.WriteLine("This operation can't be undone. Remember to make backup. If you want continue press any key.");
                Console.WriteLine();
                Console.ReadKey();

                string[] files = Directory.GetFiles(@GoogleTakeoutPath,
                "*.json",
                SearchOption.AllDirectories);

                int count = 0;

                foreach (string file in files)
                {
                    if (Path.GetFileName(file) == "metadane.json")
                        continue;

                    try
                    {
                        using FileStream fs = File.OpenRead(file);
                        GoogleJsonData googleJsonData = await JsonSerializer.DeserializeAsync<GoogleJsonData>(fs);
                        DateTime correctDateTime = UnixStartDate.AddSeconds(Convert.ToDouble(googleJsonData?.photoTakenTime?.timestamp));

                        Console.WriteLine($"Set Date: {correctDateTime} for file: {googleJsonData?.title}");

                        var picture = Path.Combine(Path.GetDirectoryName(file), googleJsonData?.title);

                        if (ExifToolPresent)
                        {
                            var result = await asyncExifTool.ExecuteAsync(new[] { $"-DateTimeOriginal={correctDateTime:yyyy:MM:dd HH:mm:ss}", $"-CreateDate={correctDateTime:yyyy:MM:dd HH:mm:ss}", "-m", "-overwrite_original", picture });

                            Console.WriteLine(result);
                        }

                        File.SetLastAccessTime(picture, correctDateTime);
                        File.SetCreationTime(picture, correctDateTime);
                        File.SetLastWriteTime(picture, correctDateTime);
                        count++;
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while processing file");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex?.InnerException?.Message);
                    }
                }

                Console.WriteLine($"Date changed in {count} files");
                await asyncExifTool.DisposeAsync();
                Console.ReadKey();
            }
            finally
            {
                await asyncExifTool.DisposeAsync();
            }
        }
    }
}