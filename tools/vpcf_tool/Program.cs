using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

// Usage:
//   vpcf_tool dump  <file.vpcf_c|file.vtex_c>
//   vpcf_tool vtex  <template.vtex_c> <image.rgba> <size> <out.vtex_c>   (256x256 DXT5, no mips: header is cloned from template)
//   vpcf_tool png   <file.vtex_c> <out.png>
//   vpcf_tool blend <file.vpcf_c> <BLEND_MODE> <overbright>               (edits the first renderer in place)
switch (args[0])
{
    case "dump":
    {
        using var res = new Resource();
        res.Read(args[1]);
        Console.WriteLine($"Type={res.ResourceType}");
        if (res.ExternalReferences != null)
            foreach (var r in res.ExternalReferences.ResourceRefInfoList) Console.WriteLine($"REF {r.Name}");
        if (res.DataBlock is Texture tex) Console.WriteLine($"{tex.Width}x{tex.Height} {tex.Format} mips={tex.NumMipLevels}");
        else Console.WriteLine(res.DataBlock.ToString());
        break;
    }
    case "png":
    {
        using var res = new Resource();
        res.Read(args[1]);
        using var bmp = ((Texture)res.DataBlock).GenerateBitmap();
        using var fs = File.Create(args[2]);
        bmp.Encode(fs, SkiaSharp.SKEncodedImageFormat.Png, 100);
        break;
    }
    case "vtex":
    {
        var template = File.ReadAllBytes(args[1]);
        var rgba = File.ReadAllBytes(args[2]);
        int size = int.Parse(args[3]);
        var encoder = new BcEncoder();
        encoder.OutputOptions.Format = CompressionFormat.Bc3;
        encoder.OutputOptions.GenerateMipMaps = false;
        encoder.OutputOptions.Quality = CompressionQuality.BestQuality;
        var blocks = encoder.EncodeToRawBytes(rgba, size, size, PixelFormat.Rgba32)[0];
        int headerSize = template.Length - blocks.Length;
        if (headerSize <= 0) throw new Exception("template size does not match encoded size");
        var output = new byte[template.Length];
        Array.Copy(template, output, headerSize);
        Array.Copy(blocks, 0, output, headerSize, blocks.Length);
        File.WriteAllBytes(args[4], output);
        Console.WriteLine($"Wrote {args[4]} ({output.Length} bytes, header {headerSize})");
        break;
    }
    case "blend":
    {
        using var res = new Resource();
        res.Read(args[1]);
        var ps = (ParticleSystem)res.DataBlock;
        var rend = ps.Data["m_Renderers"][0];
        rend["m_nOutputBlendMode"] = (KVObject)args[2];
        rend["m_flOverbrightFactor"] = (KVObject)double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
        using var ms = new MemoryStream();
        res.Serialize(ms);
        res.Dispose();
        File.WriteAllBytes(args[1], ms.ToArray());
        Console.WriteLine($"Updated {args[1]}");
        break;
    }
}
