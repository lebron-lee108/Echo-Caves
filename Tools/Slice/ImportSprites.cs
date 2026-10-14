// Standalone live-Editor snippet. It is not compiled into the project.
UnityEditor.AssetDatabase.Refresh();
var root = "Assets/_VP515/Art/Slice/";
var entries = new[] { "ForestVista", "SceneryAtlas", "TravelerFrames", "Thornling" };
foreach (var entry in entries)
{
    var path = root + entry + ".png";
    var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
    if (importer == null) throw new System.Exception("Missing image importer: " + path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = entry == "SceneryAtlas" || entry == "TravelerFrames" ? UnityEditor.SpriteImportMode.Multiple : UnityEditor.SpriteImportMode.Single;
    importer.spritePixelsPerUnit = entry == "TravelerFrames" ? 225 : entry == "Thornling" ? 400 : 100;
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.maxTextureSize = 2048;
    importer.SaveAndReimport();
    if (importer.spriteImportMode != UnityEditor.SpriteImportMode.Multiple) continue;

    var factories = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
    factories.Init();
    var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
    if (provider == null) throw new System.Exception("No sprite provider for " + path);
    provider.InitSpriteEditorDataProvider();
    var capability = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if (capability == null || !capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))
        throw new System.Exception("Sprite slicing not supported; aborted: " + path);

    string[] names;
    UnityEngine.Rect[] rects;
    UnityEngine.Vector2[] pivots;
    if (entry == "TravelerFrames")
    {
        names = new[] { "GirlIdle", "GirlWalkA", "GirlWalkB", "GirlJump", "GirlWindup", "GirlSlash", "GirlLand", "GirlHurt" };
        rects = new[] {
            new UnityEngine.Rect(0,512,384,512), new UnityEngine.Rect(384,512,384,512),
            new UnityEngine.Rect(768,512,384,512), new UnityEngine.Rect(1152,512,384,512),
            new UnityEngine.Rect(0,0,384,512), new UnityEngine.Rect(384,0,500,512),
            new UnityEngine.Rect(850,0,302,512), new UnityEngine.Rect(1152,0,384,512)
        };
        pivots = new[] { new UnityEngine.Vector2(.50f,.08f),new UnityEngine.Vector2(.50f,.08f),new UnityEngine.Vector2(.50f,.08f),new UnityEngine.Vector2(.50f,.08f),
            new UnityEngine.Vector2(.50f,.15f),new UnityEngine.Vector2(.384f,.15f),new UnityEngine.Vector2(.39f,.15f),new UnityEngine.Vector2(.50f,.15f) };
    }
    else
    {
        names = new[] { "DeadTree", "RootPlatform", "RockPlatform", "GoldenGrass", "CheckpointLantern", "RuneGate", "SigilStone", "Butterfly" };
        rects = new[] {
            new UnityEngine.Rect(0,407,435,480),new UnityEngine.Rect(438,440,490,215),
            new UnityEngine.Rect(928,433,463,220),new UnityEngine.Rect(1395,425,379,230),
            new UnityEngine.Rect(50,0,345,400),new UnityEngine.Rect(439,0,475,438),
            new UnityEngine.Rect(925,0,420,373),new UnityEngine.Rect(1345,0,429,405)
        };
        pivots = new[] {new UnityEngine.Vector2(.5f,0),new UnityEngine.Vector2(.5f,1),new UnityEngine.Vector2(.5f,1),new UnityEngine.Vector2(.5f,0),
            new UnityEngine.Vector2(.5f,0),new UnityEngine.Vector2(.5f,0),new UnityEngine.Vector2(.5f,0),new UnityEngine.Vector2(.5f,.5f)};
    }
    var old = provider.GetSpriteRects();
    var result = new System.Collections.Generic.List<UnityEditor.SpriteRect>();
    var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
    for (int i=0;i<names.Length;i++)
    {
        var id = UnityEditor.GUID.Generate();
        foreach (var previous in old) if (previous.name == names[i]) id = previous.spriteID;
        result.Add(new UnityEditor.SpriteRect { name=names[i], rect=rects[i], pivot=pivots[i], alignment=UnityEngine.SpriteAlignment.Custom, spriteID=id });
        pairs.Add(new UnityEditor.SpriteNameFileIdPair(names[i],id));
    }
    provider.SetSpriteRects(result.ToArray());
    var nameProvider = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if (nameProvider == null) throw new System.Exception("No name/fileID provider, slicing aborted: " + path);
    nameProvider.SetNameFileIdPairs(pairs);
    provider.Apply();
    importer.SaveAndReimport();
}
return "Imported original bilinear sprites and two verified atlases";
