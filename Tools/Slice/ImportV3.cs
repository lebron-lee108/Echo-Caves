UnityEditor.AssetDatabase.Refresh();
foreach(var name in new[]{"ForestVistaV3","RootBridgeV3","TravelerWalkV3","LeavesV3","ThornlingFramesV3"})
{
    string path="Assets/_VP515/Art/Slice/"+name+".png";
    if(!System.IO.File.Exists(path))continue;
    var im=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    im.textureType=UnityEditor.TextureImporterType.Sprite;
    im.spriteImportMode=name=="TravelerWalkV3"||name=="LeavesV3"||name=="ThornlingFramesV3"?UnityEditor.SpriteImportMode.Multiple:UnityEditor.SpriteImportMode.Single;
    im.spritePixelsPerUnit=name=="TravelerWalkV3"?225:100;
    im.filterMode=UnityEngine.FilterMode.Bilinear;im.mipmapEnabled=false;im.alphaIsTransparency=true;
    im.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;im.maxTextureSize=4096;
    im.isReadable=true;
    var settings=new UnityEditor.TextureImporterSettings();im.ReadTextureSettings(settings);
    settings.spriteAlignment=(int)UnityEngine.SpriteAlignment.Custom;
    settings.spritePivot=name=="RootBridgeV3"?new UnityEngine.Vector2(.5f,.786f):new UnityEngine.Vector2(.5f,.5f);im.SetTextureSettings(settings);
    im.SaveAndReimport();
    if(im.spriteImportMode!=UnityEditor.SpriteImportMode.Multiple)continue;
    var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
    var fac=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();fac.Init();
    var provider=fac.GetSpriteEditorDataProviderFromObject(im);provider.InitSpriteEditorDataProvider();
    var cap=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if(cap==null||!cap.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))throw new System.Exception("No sprite edit capability");
    var list=new System.Collections.Generic.List<UnityEditor.SpriteRect>();
    var pairs=new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
    var old=provider.GetSpriteRects();
    bool enemy=name=="ThornlingFramesV3";
    int cols=name=="TravelerWalkV3"||enemy?4:2,rows=2,w=texture.width/cols,h=texture.height/rows;
    var pixels=texture.GetPixels32();
    for(int i=0;i<cols*rows;i++)
    {
        string label=(name=="TravelerWalkV3"?"Walk":enemy?"Thornling":"Leaf")+i;
        int x=(i%cols)*w,y=(rows-1-i/cols)*h,minY=h,maxY=0,minX=w,maxX=0;
        for(int sy=0;sy<h;sy++)for(int sx=0;sx<w;sx++)if(pixels[(y+sy)*texture.width+x+sx].a>100){minY=System.Math.Min(minY,sy);maxY=System.Math.Max(maxY,sy);minX=System.Math.Min(minX,sx);maxX=System.Math.Max(maxX,sx);}
        var id=UnityEditor.GUID.Generate();foreach(var previous in old)if(previous.name==label)id=previous.spriteID;
        var pivot=name=="TravelerWalkV3"?new UnityEngine.Vector2(.56f,(minY+1)/(float)h):enemy?new UnityEngine.Vector2(.5f,0):new UnityEngine.Vector2(.5f,.5f);
        var rect=enemy?new UnityEngine.Rect(x+minX,y+minY,maxX-minX+1,maxY-minY+1):new UnityEngine.Rect(x,y,w,h);
        list.Add(new UnityEditor.SpriteRect{name=label,rect=rect,pivot=pivot,alignment=UnityEngine.SpriteAlignment.Custom,spriteID=id});
        pairs.Add(new UnityEditor.SpriteNameFileIdPair(label,id));
    }
    provider.SetSpriteRects(list.ToArray());
    var names=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if(names==null)throw new System.Exception("Missing name/fileID provider");
    names.SetNameFileIdPairs(pairs);provider.Apply();im.SaveAndReimport();
}
return "Imported versioned atmospheric background, bridge and animation/particle sprites";
