var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/_VP515/Art/Slice/TravelerFrames.png");
var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
var capability=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(capability==null||!capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect))throw new System.Exception("Sprite frame edits unsupported; aborted");
var outline=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteOutlineDataProvider>();
if(outline==null)throw new System.Exception("Outline provider unavailable; aborted");
var rects=provider.GetSpriteRects();UnityEditor.GUID id=default;bool found=false;
foreach(var rect in rects)if(rect.name=="GirlSlash"){id=rect.spriteID;found=true;}
if(!found)throw new System.Exception("Slash sprite missing");
// Keep the over-cell sword in the upper half; exclude the next pose's boot in the lower right.
outline.SetOutlines(id,new System.Collections.Generic.List<UnityEngine.Vector2[]>{new[]{
new UnityEngine.Vector2(-250,-256),new UnityEngine.Vector2(186,-256),new UnityEngine.Vector2(186,0),
new UnityEngine.Vector2(250,0),new UnityEngine.Vector2(250,256),new UnityEngine.Vector2(-250,256)}});
var settings=new UnityEditor.TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=UnityEngine.SpriteMeshType.Tight;importer.SetTextureSettings(settings);
provider.Apply();importer.SaveAndReimport();
return "Cleaned slash geometry without changing source raster or clipping upper sword";
