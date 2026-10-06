using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

// Usage:
//   vpcf_tool dump  <file.vpcf_c|file.vtex_c>
//   vpcf_tool vtex  <template.vtex_c> <image.rgba> <size> <out.vtex_c>   (256x256 DXT5, no mips: header is cloned from template)
//   vpcf_tool png   <file.vtex_c> <out.png>
//   vpcf_tool deathblow-anim <symbol.vpcf_c> <radius> <glow texture resource path>
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
            foreach (var r in res.ExternalReferences.ResourceRefInfoList) Console.WriteLine($"REF {r.Name} id={r.Id:X16}");
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
        // Sekiro deathblow dot: deathblow-anim <symbol.vpcf_c> <radius> <glow texture path>
        // Follows Valve's buff pattern (area_leash_h): no C_OP_Decay, so the dot lives until the stun's
        // end cap, then fades out over 0.2 s (LerpEndCapScalar on alpha + EndCapTimedDecay). Lifetime is
        // 1.0 so the Simple ops' fractions are seconds. Spawns growing from 0.55x with a 0.25 s fade-in,
        // spins slowly from a random angle so the fluffy edge moves, and draws an additive red glow
        // behind the dot. Both layers ignore depth and scene lighting so the body cannot hide or tint them.
        using var res = new Resource();
        res.Read(args[1]);
        var data = ((ParticleSystem)res.DataBlock).Data;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        double radius = double.Parse(args[2], inv);
        string glowTexture = args[3];

        static KVObject Obj(params (string Key, KVObject Value)[] fields) =>
            KVObject.Collection(fields.Select(f => new KeyValuePair<string, KVObject>(f.Key, f.Value)));
        static KVObject Literal(double v) => Obj(("m_nType", (KVObject)"PF_TYPE_LITERAL"), ("m_flLiteralValue", (KVObject)v));
        static KVObject InitFloat(int field, KVObject input) =>
            Obj(("_class", (KVObject)"C_INIT_InitFloat"), ("m_InputValue", input), ("m_nOutputField", (KVObject)field));

        var keep = data["m_Initializers"].Select(kv => kv.Value).Where(i => (string)i["_class"] != "C_INIT_InitFloat").ToList();
        var inits = new List<KVObject>
        {
            InitFloat(1, Literal(1.0)),
            InitFloat(3, Literal(radius)),
            InitFloat(4, Obj(("m_nType", (KVObject)"PF_TYPE_RANDOM_UNIFORM"), ("m_flRandomMin", (KVObject)0.0), ("m_flRandomMax", (KVObject)6.283185))),
            InitFloat(5, Literal(0.5)), // roll speed, rad/s (~30 deg/s)
        };
        inits.AddRange(keep);
        data["m_Initializers"] = KVObject.Array(inits);
        data["m_flConstantRadius"] = (KVObject)radius;

        var position = data["m_Operators"].Select(kv => kv.Value).First(o => (string)o["_class"] == "C_OP_PositionLock");
        data["m_Operators"] = KVObject.Array(new[]
        {
            position,
            Obj(("_class", (KVObject)"C_OP_FadeInSimple"), ("m_flFadeInTime", (KVObject)0.25)),
            Obj(("_class", (KVObject)"C_OP_InterpolateRadius"), ("m_flStartTime", (KVObject)0.0), ("m_flEndTime", (KVObject)0.25),
                ("m_flStartScale", (KVObject)0.55), ("m_flEndScale", (KVObject)1.0)),
            Obj(("_class", (KVObject)"C_OP_SpinUpdate")),
            Obj(("_class", (KVObject)"C_OP_LerpEndCapScalar"), ("m_flLerpTime", (KVObject)0.2), ("m_nFieldOutput", (KVObject)7), ("m_flOutput", (KVObject)0.0)),
            Obj(("_class", (KVObject)"C_OP_EndCapTimedDecay"), ("m_flDecayTime", (KVObject)0.2)),
        });

        var dot = data["m_Renderers"][0];
        dot["m_nOutputBlendMode"] = (KVObject)"PARTICLE_OUTPUT_BLEND_MODE_ALPHA";
        dot["m_flOverbrightFactor"] = (KVObject)1.0;
        dot["m_bDisableZBuffering"] = (KVObject)true;
        dot["m_nFeatheringMode"] = (KVObject)"PARTICLE_DEPTH_FEATHERING_OFF";
        dot["m_flSelfIllumAmount"] = (KVObject)1.0;

        var glowRef = (KVObject)glowTexture.Replace(".vtex_c", ".vtex");
        glowRef.Flag = KVFlag.Resource;
        var glow = Obj(
            ("_class", (KVObject)"C_OP_RenderSprites"),
            ("m_bUseYawWithNormalAligned", (KVObject)false),
            ("m_nOrientationType", (KVObject)0),
            ("m_nOutputBlendMode", (KVObject)"PARTICLE_OUTPUT_BLEND_MODE_ADD"),
            ("m_flOverbrightFactor", (KVObject)1.0),
            ("m_bDisableZBuffering", (KVObject)true),
            ("m_nFeatheringMode", (KVObject)"PARTICLE_DEPTH_FEATHERING_OFF"),
            ("m_flSelfIllumAmount", (KVObject)1.0),
            ("m_flRadiusScale", (KVObject)1.8),
            ("m_flAlphaScale", (KVObject)0.6),
            ("m_vecTexturesInput", KVObject.Array(new[] { Obj(("m_hTexture", glowRef)) })));
        data["m_Renderers"] = KVObject.Array(new[] { glow, dot });

        var refs = res.ExternalReferences.ResourceRefInfoList;
        if (!refs.Any(r => r.Name == glowTexture.Replace(".vtex_c", ".vtex")))
            refs.Add(new ValveResourceFormat.Blocks.ResourceExtRefList.ResourceReferenceInfo { Id = 0, Name = glowTexture.Replace(".vtex_c", ".vtex") });

        using var ms = new MemoryStream();
        res.Serialize(ms);
        res.Dispose();
        File.WriteAllBytes(args[1], ms.ToArray());
        Console.WriteLine($"Updated {args[1]}");
        break;
    }
    case "timing":
    {
        // Sets particle lifetime (C_INIT_InitFloat field 1) and the Simple fade in/out fractions.
        using var res = new Resource();
        res.Read(args[1]);
        var data = ((ParticleSystem)res.DataBlock).Data;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        double lifetime = double.Parse(args[2], inv), fadeIn = double.Parse(args[3], inv), fadeOut = double.Parse(args[4], inv);
        foreach (var init in data["m_Initializers"].Select(kv => kv.Value))
            if ((string)init["_class"] == "C_INIT_InitFloat" && OutputField(init) == 1)
                init["m_InputValue"]["m_flLiteralValue"] = (KVObject)lifetime;
        foreach (var op in data["m_Operators"].Select(kv => kv.Value))
        {
            if ((string)op["_class"] == "C_OP_FadeInSimple") op["m_flFadeInTime"] = (KVObject)fadeIn;
            if ((string)op["_class"] == "C_OP_FadeOutSimple") op["m_flFadeOutTime"] = (KVObject)fadeOut;
        }
        using var ms = new MemoryStream();
        res.Serialize(ms);
        res.Dispose();
        File.WriteAllBytes(args[1], ms.ToArray());
        Console.WriteLine($"Updated {args[1]}");
        break;
    }
    case "extract":
    {
        // Copies one file out of a VPK: extract <pak01_dir.vpk> <internal/path.vpcf_c> <out>
        using var pkg = new ValvePak.Package();
        pkg.Read(args[1]);
        var entry = pkg.FindEntry(args[2]) ?? throw new FileNotFoundException(args[2]);
        pkg.ReadEntry(entry, out var bytes);
        File.WriteAllBytes(args[3], bytes);
        break;
    }
    case "scan":
    {
        // Finds game particles whose decompiled text contains each key; prints counts, values and examples.
        using var pkg = new ValvePak.Package();
        pkg.Read(args[1]);
        var keys = args.Skip(2).ToArray();
        var hits = keys.ToDictionary(k => k, _ => new Dictionary<string, List<string>>());
        foreach (var entry in pkg.Entries.TryGetValue("vpcf_c", out var list) ? list : new())
        {
            pkg.ReadEntry(entry, out var bytes);
            string text;
            try { using var ms = new MemoryStream(bytes); using var r = new Resource(); r.Read(ms); text = r.DataBlock.ToString(); }
            catch { continue; }
            foreach (var k in keys)
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, $@"{k} = ([^

]*)"))
                {
                    var v = m.Groups[1].Value.Trim();
                    if (!hits[k].TryGetValue(v, out var files)) hits[k][v] = files = new();
                    files.Add(entry.GetFullPath());
                }
        }
        int top = int.TryParse(Environment.GetEnvironmentVariable("SCAN_TOP"), out var t) ? t : 8;
        foreach (var (k, vals) in hits)
        {
            Console.WriteLine($"== {k}");
            foreach (var (v, files) in vals.OrderByDescending(x => x.Value.Count).Take(top))
                Console.WriteLine($"  {files.Count,6}x {v}   e.g. {string.Join(", ", files.Distinct().Take(2))}");
        }
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
