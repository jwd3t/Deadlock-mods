using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;

// Usage:
//   vpcf_tool dump  <file.vpcf_c|file.vtex_c>
//   vpcf_tool vtex  <template.vtex_c> <image.rgba> <size> <out.vtex_c>   (256x256 DXT5, no mips: header is cloned from template)
//   vpcf_tool png   <file.vtex_c> <out.png>
//   vpcf_tool deathblow-anim <symbol.vpcf_c> <radius>
//   vpcf_tool timing <file.vpcf_c> <lifetime> <fadeInFraction> <fadeOutFraction>
//   vpcf_tool scan <pak01_dir.vpk> <key>...   (stats of particle fields across the game)
//   vpcf_tool extract <pak01_dir.vpk> <internal path> <out>
//   vpcf_tool sheet-vtex <template.vtex_c> <image.rgba> <size> <cols> <rows> <cell> <margin> <frames> <out.vtex_c>
//   vpcf_tool parent-offset <melee_parry_debuff.vpcf_c> <z>
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
        // Sekiro deathblow dot: deathblow-anim <symbol.vpcf_c> <radius>
        // Two renderers on one particle, matching 1080p Sekiro footage (see tools/make_deathblow_texture.py):
        // a MOD2X tint that hides the scene's green/blue under the dot, and the measured emitted light as
        // ADD. Textures: materials/particle/sekiro_deathblow_{tint,dot}.vtex. Both ignore depth and scene
        // lighting. Lifetime 1.0 so the Simple ops' fractions are seconds; no C_OP_Decay: the dot lives
        // until the stun's end cap, then fades out over 0.2 s (Valve's area_leash_h pattern). Entry: 0.12 s
        // fade-in contracting from 1.3x. Constant size afterwards (as in the footage); a slow spin keeps
        // the grain shimmering instead of sitting still.
        using var res = new Resource();
        res.Read(args[1]);
        var data = ((ParticleSystem)res.DataBlock).Data;
        double radius = double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
        const string tex = "materials/particle/sekiro_deathblow_";

        static KVObject Obj(params (string Key, KVObject Value)[] fields) =>
            KVObject.Collection(fields.Select(f => new KeyValuePair<string, KVObject>(f.Key, f.Value)));
        static KVObject Literal(double v) => Obj(("m_nType", (KVObject)"PF_TYPE_LITERAL"), ("m_flLiteralValue", (KVObject)v));
        static KVObject InitFloat(int field, KVObject input) =>
            Obj(("_class", (KVObject)"C_INIT_InitFloat"), ("m_InputValue", input), ("m_nOutputField", (KVObject)field));
        static KVObject Sprite(string texture, string blend, double overbright, double radiusScale, double alphaScale)
        {
            var t = (KVObject)texture;
            t.Flag = KVFlag.Resource;
            return Obj(
                ("_class", (KVObject)"C_OP_RenderSprites"),
                ("m_bUseYawWithNormalAligned", (KVObject)false),
                ("m_nOrientationType", (KVObject)0),
                ("m_nOutputBlendMode", (KVObject)blend),
                ("m_flOverbrightFactor", (KVObject)overbright),
                ("m_bDisableZBuffering", (KVObject)true),
                ("m_nFeatheringMode", (KVObject)"PARTICLE_DEPTH_FEATHERING_OFF"),
                ("m_flSelfIllumAmount", (KVObject)1.0),
                ("m_flRadiusScale", (KVObject)radiusScale),
                ("m_flAlphaScale", (KVObject)alphaScale),
                ("m_vecTexturesInput", KVObject.Array(new[] { Obj(("m_hTexture", t)) })));
        }

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
            Obj(("_class", (KVObject)"C_OP_FadeInSimple"), ("m_flFadeInTime", (KVObject)0.12)),
            Obj(("_class", (KVObject)"C_OP_InterpolateRadius"), ("m_flStartTime", (KVObject)0.0), ("m_flEndTime", (KVObject)0.12),
                ("m_flStartScale", (KVObject)1.3), ("m_flEndScale", (KVObject)1.0)),
            Obj(("_class", (KVObject)"C_OP_SpinUpdate")),
            Obj(("_class", (KVObject)"C_OP_LerpEndCapScalar"), ("m_flLerpTime", (KVObject)0.2), ("m_nFieldOutput", (KVObject)7), ("m_flOutput", (KVObject)0.0)),
            Obj(("_class", (KVObject)"C_OP_EndCapTimedDecay"), ("m_flDecayTime", (KVObject)0.2)),
        });

        data["m_Renderers"] = KVObject.Array(new[]
        {
            Sprite(tex + "tint.vtex", "PARTICLE_OUTPUT_BLEND_MODE_MOD2X", 1.0, 1.0, 1.0),
            Sprite(tex + "dot.vtex", "PARTICLE_OUTPUT_BLEND_MODE_ADD", 2.0, 1.0, 1.0),
        });

        var refs = res.ExternalReferences.ResourceRefInfoList;
        refs.RemoveAll(r => r.Name.StartsWith(tex) && !r.Name.EndsWith("tint.vtex") && !r.Name.EndsWith("dot.vtex"));
        foreach (var name in new[] { "tint", "dot" })
            if (!refs.Any(r => r.Name == tex + name + ".vtex"))
                refs.Add(new ValveResourceFormat.Blocks.ResourceExtRefList.ResourceReferenceInfo { Id = 0, Name = tex + name + ".vtex" });

        using var ms = new MemoryStream();
        res.Serialize(ms);
        res.Dispose();
        File.WriteAllBytes(args[1], ms.ToArray());
        Console.WriteLine($"Updated {args[1]}");
        break;
    }
    case "parent-offset":
    {
        // parent-offset <melee_parry_debuff.vpcf_c> <z>: moves the debuff's anchor particle (whose position
        // C_OP_SetChildControlPoints hands to the children as CP3) by z world units from the "aim"
        // attachment. Vanilla uses +70 to float the stun stars over the head.
        using var res = new Resource();
        res.Read(args[1]);
        var data = ((ParticleSystem)res.DataBlock).Data;
        double z = double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
        var box = data["m_Initializers"].Select(kv => kv.Value).First(i => (string)i["_class"] == "C_INIT_CreateWithinBox");
        foreach (var key in new[] { "m_vecMin", "m_vecMax" })
            box[key]["m_vLiteralValue"] = KVObject.Array(new[] { (KVObject)0.0, (KVObject)0.0, (KVObject)z });
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
    case "sheet-vtex":
    {
        // Animated DXT5 flipbook (VTexExtraData.SHEET), built at the binary level because VRF cannot
        // serialize textures. Layout and timing quirks follow Deadlock-Modding-Skill SKILL.md section 2B:
        // TotalTime = frames - 1 and DisplayTime = 1 (0 on the last frame), in tick units.
        // sheet-vtex <template.vtex_c> <image.rgba> <size> <cols> <rows> <cell> <margin> <frames> <out.vtex_c>
        // The RED2 block is copied from the template.
        var template = File.ReadAllBytes(args[1]);
        var rgba = File.ReadAllBytes(args[2]);
        int size = int.Parse(args[3]), cols = int.Parse(args[4]), rows = int.Parse(args[5]);
        int cell = int.Parse(args[6]), margin = int.Parse(args[7]), frames = int.Parse(args[8]);
        if (frames > cols * rows) throw new ArgumentException("more frames than cells");

        byte[] red2 = null;
        int blockCount = BitConverter.ToInt32(template, 12);
        for (int b = 0, pos = 16; b < blockCount; b++, pos += 12)
            if (System.Text.Encoding.ASCII.GetString(template, pos, 4) == "RED2")
                red2 = template[(pos + 4 + BitConverter.ToInt32(template, pos + 4))..][..BitConverter.ToInt32(template, pos + 8)];
        if (red2 == null) throw new Exception("template has no RED2 block");

        using var sheetMs = new MemoryStream();
        using (var w = new BinaryWriter(sheetMs, System.Text.Encoding.UTF8, true))
        {
            w.Write(8u); w.Write(1u); w.Write(0u);
            w.Write(true); w.Write(false); w.Write(false); w.Write(false);
            long posFramesRel = sheetMs.Position; w.Write(0);
            w.Write((uint)frames); w.Write((float)(frames - 1));
            long posNameRel = sheetMs.Position; w.Write(0);
            long posFloatParamsRel = sheetMs.Position; w.Write(0); w.Write(0u);
            long namePos = sheetMs.Position; w.Write(System.Text.Encoding.UTF8.GetBytes("CDmeSheetSequence\0"));
            void Patch(long at, long target) { long cur = sheetMs.Position; sheetMs.Position = at; w.Write((int)(target - at)); sheetMs.Position = cur; }
            Patch(posNameRel, namePos);
            Patch(posFloatParamsRel, sheetMs.Position);
            Patch(posFramesRel, sheetMs.Position);
            var imgRel = new long[frames];
            for (int f = 0; f < frames; f++) { w.Write(f == frames - 1 ? 0f : 1f); imgRel[f] = sheetMs.Position; w.Write(0); w.Write(1u); }
            for (int f = 0; f < frames; f++)
            {
                Patch(imgRel[f], sheetMs.Position);
                float x0 = (float)(margin + (f % cols) * cell) / size, x1 = (float)(margin + (f % cols + 1) * cell) / size;
                float y0 = (float)(margin + (f / cols) * cell) / size, y1 = (float)(margin + (f / cols + 1) * cell) / size;
                w.Write(x0); w.Write(y0); w.Write(x1); w.Write(y1); w.Write(x0); w.Write(y0); w.Write(x1); w.Write(y1);
            }
        }
        var sheet = sheetMs.ToArray();

        var encoder = new BcEncoder();
        encoder.OutputOptions.Format = CompressionFormat.Bc3;
        encoder.OutputOptions.GenerateMipMaps = false;
        encoder.OutputOptions.Quality = CompressionQuality.BestQuality;
        var pixels = encoder.EncodeToRawBytes(rgba, size, size, PixelFormat.Rgba32)[0];

        using var dataMs = new MemoryStream();
        using (var d = new BinaryWriter(dataMs, System.Text.Encoding.UTF8, true))
        {
            d.Write((ushort)1); d.Write((ushort)0);
            d.Write(0.1f); d.Write(0.01f); d.Write(0.01f); d.Write(0.1f);
            d.Write((ushort)size); d.Write((ushort)size); d.Write((ushort)1);
            d.Write((byte)2); d.Write((byte)1); d.Write(0u);
            d.Write(8u); d.Write(1u);
            d.Write(2u); d.Write(8u); d.Write((uint)sheet.Length);
            d.Write(sheet);
            while (dataMs.Position % 16 != 0) d.Write((byte)0);
        }
        var data = dataMs.ToArray();

        using var outMs = new MemoryStream();
        using (var o = new BinaryWriter(outMs, System.Text.Encoding.UTF8, true))
        {
            int red2Offset = 16 + 2 * 12;
            int dataOffset = red2Offset + red2.Length;
            int pad = (16 - dataOffset % 16) % 16;
            dataOffset += pad;
            o.Write((uint)(dataOffset + data.Length)); o.Write((ushort)12); o.Write((ushort)1); o.Write(8u); o.Write(2u);
            o.Write(System.Text.Encoding.ASCII.GetBytes("RED2")); o.Write((uint)(red2Offset - 20)); o.Write((uint)red2.Length);
            o.Write(System.Text.Encoding.ASCII.GetBytes("DATA")); o.Write((uint)(dataOffset - 32)); o.Write((uint)data.Length);
            o.Write(red2);
            for (int i = 0; i < pad; i++) o.Write((byte)0);
            o.Write(data);
            o.Write(pixels);
        }
        File.WriteAllBytes(args[9], outMs.ToArray());
        Console.WriteLine($"Wrote {args[9]} ({outMs.Length} bytes, {frames} frames {cols}x{rows} of {cell}px)");
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
