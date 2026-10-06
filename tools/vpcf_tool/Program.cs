using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

// Usage:
//   vpcf_tool dump  <file.vpcf_c|file.vtex_c>
//   vpcf_tool vtex  <template.vtex_c> <image.rgba> <size> <out.vtex_c>   (256x256 DXT5, no mips: header is cloned from template)
//   vpcf_tool png   <file.vtex_c> <out.png>
//   vpcf_tool deathblow-anim <symbol.vpcf_c> <lifetime> <radius>
//   vpcf_tool timing <file.vpcf_c> <lifetime> <fadeInFraction> <fadeOutFraction>
//   vpcf_tool scan <pak01_dir.vpk> <key>...   (stats of particle fields across the game)
//   vpcf_tool extract <pak01_dir.vpk> <internal path> <out>
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
    case "deathblow-anim":
    {
        // Sekiro deathblow dot, matched to two footage clips: ~0.12 s fade-in settling from 1.25x size,
        // ~0.18 s fade-out while expanding to 1.3x. Drawn on top of the body (no depth test, no
        // feathering) and self-illuminated so scene lighting cannot wash it out.
        // Simple fade/interpolate times are fractions of the lifetime.
        using var res = new Resource();
        res.Read(args[1]);
        var data = ((ParticleSystem)res.DataBlock).Data;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        double lifetime = double.Parse(args[2], inv), radius = double.Parse(args[3], inv);
        double fadeIn = 0.12 / lifetime, fadeOut = 0.18 / lifetime;

        static KVObject Op(params (string Key, KVObject Value)[] fields) =>
            KVObject.Collection(fields.Select(f => new KeyValuePair<string, KVObject>(f.Key, f.Value)));

        foreach (var init in data["m_Initializers"].Select(kv => kv.Value))
        {
            if ((string)init["_class"] == "C_INIT_InitFloat" && OutputField(init) == 1)
                init["m_InputValue"]["m_flLiteralValue"] = (KVObject)lifetime;
            if ((string)init["_class"] == "C_INIT_InitFloat" && OutputField(init) == 3)
                init["m_InputValue"]["m_flLiteralValue"] = (KVObject)radius;
        }
        data["m_flConstantRadius"] = (KVObject)radius;

        var ops = data["m_Operators"].Select(kv => kv.Value).Where(o => (string)o["_class"] is not ("C_OP_FadeInSimple" or "C_OP_FadeOutSimple" or "C_OP_InterpolateRadius")).ToList();
        int decay = ops.FindIndex(o => (string)o["_class"] == "C_OP_Decay");
        ops.InsertRange(decay < 0 ? ops.Count : decay, new[]
        {
            Op(("_class", (KVObject)"C_OP_FadeInSimple"), ("m_flFadeInTime", (KVObject)fadeIn)),
            Op(("_class", (KVObject)"C_OP_FadeOutSimple"), ("m_flFadeOutTime", (KVObject)fadeOut)),
            Op(("_class", (KVObject)"C_OP_InterpolateRadius"), ("m_flStartTime", (KVObject)0.0), ("m_flEndTime", (KVObject)fadeIn),
               ("m_flStartScale", (KVObject)1.25), ("m_flEndScale", (KVObject)1.0)),
            Op(("_class", (KVObject)"C_OP_InterpolateRadius"), ("m_flStartTime", (KVObject)(1.0 - fadeOut)), ("m_flEndTime", (KVObject)1.0),
               ("m_flStartScale", (KVObject)1.0), ("m_flEndScale", (KVObject)1.3)),
        });
        data["m_Operators"] = KVObject.Array(ops);

        var rend = data["m_Renderers"][0];
        rend["m_bDisableZBuffering"] = (KVObject)true;
        rend["m_nFeatheringMode"] = (KVObject)"PARTICLE_DEPTH_FEATHERING_OFF";
        rend["m_flSelfIllumAmount"] = (KVObject)1.0;

        using var ms = new MemoryStream();
        res.Serialize(ms);
        res.Dispose();
        File.WriteAllBytes(args[1], ms.ToArray());
        Console.WriteLine($"Updated {args[1]}");
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

// C_INIT_InitFloat omits m_nOutputField when it is the default (3 = radius).
static int OutputField(KVObject init)
{
    try { return (int)init["m_nOutputField"]; }
    catch (KeyNotFoundException) { return 3; }
}
