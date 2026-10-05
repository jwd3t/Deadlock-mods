using System;
using System.IO;
using ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

class Program
{
    static void Main(string[] args)
    {
        var vpkPath = args.Length > 0 ? args[0] : @"C:\Users\juan\Documents\GitHub\deadlock-mods\mods\Sekiro_Deathblow_Mod\pak01_dir.vpk";
        using var pkg = new Package();
        pkg.Read(vpkPath);

        Console.WriteLine($"Verifying {vpkPath} ({new FileInfo(vpkPath).Length} bytes):");
        foreach (var typeList in pkg.Entries.Values)
        {
            foreach (var entry in typeList)
            {
                var fullPath = $"{entry.DirectoryName}/{entry.FileName}.{entry.TypeName}";
                pkg.ReadEntry(entry, out var bytes);
                using var ms = new MemoryStream(bytes);
                using var res = new Resource();
                res.Read(ms);

                if (res.DataBlock is Texture tex)
                {
                    using var bmp = tex.GenerateBitmap();
                    Console.WriteLine($"  OK -> {fullPath} [{tex.Width}x{tex.Height} {tex.Format}, Center={bmp.GetPixel(128, 128)}]");
                }
                else if (res.DataBlock is Sound snd)
                {
                    Console.WriteLine($"  OK -> {fullPath} [Sound {snd.SoundType} {snd.Duration:F3}s]");
                }
                else if (res.DataBlock is ParticleSystem ps)
                {
                    Console.WriteLine($"  OK -> {fullPath} [Particle {ps.Data["_class"]}]");
                }
                else
                {
                    Console.WriteLine($"  OK -> {fullPath} [{res.DataBlock?.GetType().Name}]");
                }
            }
        }
        Console.WriteLine("All files in Sekiro_Deathblow_Mod verified successfully!");
    }
}
