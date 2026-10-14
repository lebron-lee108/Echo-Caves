using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VP515.Slice;
namespace VP515.Editor
{
    public static class SliceArtUpgrade
    {
        public static string Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before art changes");
            var b=UnityEngine.Object.FindFirstObjectByType<SliceBootstrap>();
            if(!b||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=SliceBuilder.ScenePath)throw new Exception("Open current slice");
            SliceBuilder.ValidateBindings(b);
            string art="Assets/_VP515/Art/Slice/";
            var vista=AssetDatabase.LoadAssetAtPath<Sprite>(art+"ForestVistaV3.png");
            var bridge=AssetDatabase.LoadAssetAtPath<Sprite>(art+"RootBridgeV3.png");
            if(!vista||!bridge)throw new Exception("Import V3 assets first");
            var walkFrames=AssetDatabase.LoadAllAssetsAtPath(art+"TravelerWalkV3.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            var enemyFrames=AssetDatabase.LoadAllAssetsAtPath(art+"ThornlingFramesV3.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            var leaves=AssetDatabase.LoadAllAssetsAtPath(art+"LeavesV3.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            SliceBuilder.RequireBank(walkFrames,8,"walk source");SliceBuilder.RequireBank(enemyFrames,8,"enemy source");SliceBuilder.RequireBank(leaves,4,"foliage source");
            string materialPath="Assets/_VP515/Materials/SliceBridgeBlend.mat";
            var bridgeMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!bridgeMaterial){var shader=Shader.Find("VP515/BridgeEdgeBlend");if(!shader)throw new Exception("Import bridge edge shader first");bridgeMaterial=new Material(shader);AssetDatabase.CreateAsset(bridgeMaterial,materialPath);}
            var world=b.transform.Find("World");var camera=b.CameraRig.GetComponent<Camera>();camera.orthographicSize=5.8f;b.CameraRig.Height=1.3f;b.CameraRig.ResetView();
            foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>(true)){
                if(sr.name=="ForestVista"){sr.sprite=vista;sr.color=Color.white;sr.transform.position=new Vector3(10,1.6f,0);sr.transform.localScale=Vector3.one*(28.2f/vista.bounds.size.x);sr.GetComponent<SliceParallax>().FollowFactor=.92f;}
                if(sr.name=="RootRoad"){sr.enabled=false;}
                if(sr.name=="Grass"){sr.enabled=false;}
                if(sr.name=="FarTrunk"){sr.color=new Color(.2f,.26f,.29f,.14f);sr.GetComponent<SliceParallax>().FollowFactor=.62f;}
                if(sr.name=="NearTrunk"){sr.color=new Color(.025f,.035f,.045f,.6f);sr.transform.localScale=Vector3.one*(6.7f/sr.sprite.bounds.size.x);}
                if(sr.name=="WorldTree"){sr.color=new Color(.4f,.46f,.50f,.86f);}
            }
            for(int i=0;i<3;i++){
                string name="ContinuousRootBridge"+i;var tr=world.Find(name);var go=tr?tr.gameObject:new GameObject(name);go.transform.SetParent(world,false);
                go.transform.position=new Vector3(12+i*22,-.02f,0);var sr=go.GetComponent<SpriteRenderer>();if(!sr)sr=go.AddComponent<SpriteRenderer>();sr.sprite=bridge;sr.sortingOrder=5;sr.sharedMaterial=bridgeMaterial;
                go.transform.localScale=Vector3.one*(26/bridge.bounds.size.x);sr.flipX=i%2==1;
            }
            // The opening is now one continuous bridge; preserve the two jumpable stones.
            if(!world.Find("BridgeGapSupport")){
                var go=new GameObject("BridgeGapSupport");go.transform.SetParent(world,false);go.transform.position=new Vector3(11.9f,-.3f);go.layer=LayerMask.NameToLayer("TerrainSolid");go.AddComponent<BoxCollider2D>().size=new Vector2(2.2f,.6f);
            }
            b.WalkFrames=walkFrames;b.EnemyFrames=enemyFrames;
            if(b.EnemyFrames.Length==8)b.EnemyArt=b.EnemyFrames[0];
            var environment=b.GetComponent<SliceEnvironment>();if(!environment)environment=b.gameObject.AddComponent<SliceEnvironment>();environment.View=camera;environment.Leaves=leaves;
            EditorUtility.SetDirty(b);EditorSceneManager.MarkSceneDirty(b.gameObject.scene);EditorSceneManager.SaveScene(b.gameObject.scene);AssetDatabase.SaveAssets();
            return "Applied cold/warm forest layers, continuous rock-root bridge, compact camera, eight-frame walk, three foliage depth bands";
        }
    }
}
