using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EchoCaves.Editor
{
    public static class EchoEnvironmentArt
    {
        public const string Root="Assets/_EchoCaves/";
        public static readonly Dictionary<string,Sprite> Sprites=new Dictionary<string,Sprite>();
        public static Material RockMaterial;
        public static void Prepare()
        {
            foreach(var d in new[]{"Art","Scene","Scenes","Materials","Meshes","Prefabs"})Directory.CreateDirectory(Root+d);
            AssetDatabase.Refresh();
            foreach(var f in Directory.GetFiles(Root+"Art","*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(f.Replace('\\','/'));
                if(ti==null)throw new Exception("Art importer missing: "+f);
                ti.textureType=TextureImporterType.Sprite;
                // Never collapse a sliced atlas back to Single: that discards persistent subasset IDs.
                bool atlas=new[]{"CaveProps","CampModules","Structures","GroundRim","CeilingRim","RadioEquipment"}.Contains(Path.GetFileNameWithoutExtension(f));
                if(!atlas)ti.spriteImportMode=SpriteImportMode.Single;
                ti.spritePixelsPerUnit=16;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;
                ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=4096;
                ti.alphaIsTransparency=true;ti.npotScale=TextureImporterNPOTScale.None;ti.isReadable=true;
                ti.wrapMode=f.Contains("RockTile")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }
            Slice("CaveProps",4,4,new[]{"Mushroom","Lantern","Crate","Fern","Stalactite","Rubble","MossLip","Vine","Valve","Speaker","Fan","Gate","Timber","RadioDesk","ControlBox","Sign"});
            Slice("CampModules",2,1,new[]{"RadioHut","ToolShed"});
            if(File.Exists(Root+"Art/Structures.png"))Slice("Structures",2,2,new[]{"BridgeShort","BridgeLong","Catwalk","LadderArt"},new[]{new Rect(0,.56f,.46f,.44f),new Rect(.46f,.56f,.54f,.44f),new Rect(0,0,.68f,.56f),new Rect(.69f,0,.31f,.56f)});
            foreach(var key in new[]{"GroundRim","CeilingRim"})if(File.Exists(Root+"Art/"+key+".png"))Slice(key,1,1,new[]{key});
            if(File.Exists(Root+"Art/RadioEquipment.png"))Slice("RadioEquipment",2,2,new[]{"RadioPlayer","Transmitter","Workbench","TapeShelf"},new[]{new Rect(0,.5f,.61f,.5f),new Rect(.61f,.5f,.39f,.5f),new Rect(0,0,.61f,.5f),new Rect(.61f,0,.39f,.5f)});
            Sprites.Clear();
            foreach(var f in Directory.GetFiles(Root+"Art","*.png"))foreach(var s in AssetDatabase.LoadAllAssetsAtPath(f.Replace('\\','/')).OfType<Sprite>())Sprites[s.name]=s;
            var path=Root+"Materials/Rock.mat";
            RockMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!RockMaterial){RockMaterial=new Material(Shader.Find("EchoCaves/PixelRock"));RockMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Art/RockTile.png");AssetDatabase.CreateAsset(RockMaterial,path);}
            var whitePath=Root+"Art/Pixel.asset";
            var white=AssetDatabase.LoadAssetAtPath<Texture2D>(whitePath);
            if(!white){white=new Texture2D(1,1,TextureFormat.RGBA32,false);white.name="Pixel";white.filterMode=FilterMode.Point;white.SetPixel(0,0,Color.white);white.Apply();AssetDatabase.CreateAsset(white,whitePath);}
            var spritePath=Root+"Art/PixelSprite.asset";
            var pixel=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if(!pixel){pixel=Sprite.Create(white,new Rect(0,0,1,1),Vector2.one*.5f,1);pixel.name="PixelSprite";AssetDatabase.CreateAsset(pixel,spritePath);}
            Sprites["Pixel"]=pixel;
        }
        static void Slice(string file,int columns,int rows,string[] names,Rect[] customCells=null)
        {
            string p=Root+"Art/"+file+".png";
            var ti=(TextureImporter)AssetImporter.GetAtPath(p);if(!ti)throw new Exception("Missing "+p);
            ti.spriteImportMode=SpriteImportMode.Multiple;ti.SaveAndReimport();
            var factory=new SpriteDataProviderFactories();factory.Init();var dp=factory.GetSpriteEditorDataProviderFromObject(ti);
            if(dp==null)throw new Exception("No sprite provider");dp.InitSpriteEditorDataProvider();
            var ec=dp.GetDataProvider<ISpriteFrameEditCapability>();
            if(ec==null||!ec.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))throw new Exception("Sprite slicing unsupported, aborted");
            ti.GetSourceTextureWidthAndHeight(out int w,out int h);var rects=new List<SpriteRect>();
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);raw.LoadImage(File.ReadAllBytes(p));var pixels=raw.GetPixels32();
            var old=dp.GetSpriteRects();
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                int k=row*columns+col;var existing=old.FirstOrDefault(x=>x.name==names[k]);
                int bx=col*w/columns,by=(rows-1-row)*h/rows,ex=(col+1)*w/columns,ey=(rows-row)*h/rows;
                if(customCells!=null){var cell=customCells[k];bx=Mathf.FloorToInt(cell.x*w);by=Mathf.FloorToInt(cell.y*h);ex=Mathf.Min(w,Mathf.CeilToInt(cell.xMax*w));ey=Mathf.Min(h,Mathf.CeilToInt(cell.yMax*h));}
                int minX=ex,minY=ey,maxX=bx,maxY=by;
                for(int yy=by;yy<ey;yy++)for(int xx=bx;xx<ex;xx++)if(pixels[yy*w+xx].a>100){minX=Math.Min(minX,xx);minY=Math.Min(minY,yy);maxX=Math.Max(maxX,xx);maxY=Math.Max(maxY,yy);}
                if(maxX<=minX||maxY<=minY)throw new Exception("Empty atlas cell "+names[k]);
                var rect=new Rect(Math.Max(bx,minX-2),Math.Max(by,minY-2),Math.Min(ex,maxX+3)-Math.Max(bx,minX-2),Math.Min(ey,maxY+3)-Math.Max(by,minY-2));
                rects.Add(new SpriteRect{name=names[k],rect=rect,alignment=SpriteAlignment.Center,pivot=Vector2.one*.5f,spriteID=existing!=null?existing.spriteID:GUID.Generate()});
            }
            dp.SetSpriteRects(rects.ToArray());var ids=dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if(ids!=null)ids.SetNameFileIdPairs(rects.Select(x=>new SpriteNameFileIdPair(x.name,x.spriteID)));
            dp.Apply();ti.SaveAndReimport();UnityEngine.Object.DestroyImmediate(raw);
        }
        public static GameObject Node(string name,Transform parent,float x=0,float y=0)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(x,y,0);return g;}
        public static SpriteRenderer Art(string key,Transform parent,float x,float y,float width,float height,int order=10,Color? tint=null)
        {
            if(!Sprites.TryGetValue(key,out var sprite))throw new Exception("Missing sprite "+key);
            var g=Node(key,parent,x,y);var sr=g.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;sr.color=tint??Color.white;
            g.transform.localScale=new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1);return sr;
        }
        public static SpriteRenderer Pixel(string name,Transform parent,float x,float y,float w,float h,Color color,int order=20)
        {var sr=Art("Pixel",parent,x,y,w,h,order,color);sr.name=name;return sr;}
        public static void Line(string name,Transform parent,Vector2 a,Vector2 b,float thickness,Color color,int order=20)
        {var d=b-a;var sr=Pixel(name,parent,(a.x+b.x)/2,(a.y+b.y)/2,d.magnitude,thickness,color,order);sr.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
        public static GameObject Rock(string name,Transform parent,Vector2[] top,float depth,int order=10,bool ceiling=false)
        {
            var g=Node(name,parent);var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            float bottom=ceiling?top.Max(p=>p.y)+depth:top.Min(p=>p.y)-depth;
            for(int i=0;i<top.Length;i++){verts.Add(top[i]);verts.Add(new Vector3(top[i].x,bottom));uv.Add(top[i]/8);uv.Add(new Vector2(top[i].x,bottom)/8);}
            for(int i=0;i<top.Length-1;i++){int a=i*2;tris.AddRange(new[]{a,a+1,a+2,a+2,a+1,a+3});}
            var mesh=new Mesh{name=name};mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();mesh.RecalculateNormals();
            string asset=Root+"Meshes/"+name+".asset";
            if(AssetDatabase.LoadAssetAtPath<Mesh>(asset))throw new Exception("Mesh output already exists: "+asset);
            AssetDatabase.CreateAsset(mesh,asset);g.AddComponent<MeshFilter>().sharedMesh=mesh;
            var mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=RockMaterial;mr.sortingOrder=order;
            var collider=g.AddComponent<PolygonCollider2D>();var outline=top.ToList();outline.Add(new Vector2(top.Last().x,bottom));outline.Add(new Vector2(top.First().x,bottom));collider.points=outline.ToArray();
            return g;
        }
    }
}
