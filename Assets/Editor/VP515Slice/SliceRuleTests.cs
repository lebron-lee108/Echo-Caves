using System;
using System.Reflection;
using System.Text;

namespace VP515.Slice
{
    // No Unity/NUnit dependency: also compiled by the PowerShell Add-Type harness.
    public static class SliceRuleTests
    {
        public static string Run()
        {
            var report = new StringBuilder(); int passed = 0, failed = 0;
            Type type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                type = type ?? assembly.GetType("VP515.Slice.SliceRules");
            Action<string, Action<object>> test = (name, body) => {
                try { Check(type != null, "SliceRules missing"); body(Activator.CreateInstance(type));
                    passed++; report.AppendLine("PASS " + name); }
                catch (Exception e) { failed++; report.AppendLine("FAIL " + name + ": " + (e.InnerException ?? e).Message); }
            };
            test("damage and exact 0.75s invulnerability boundary", r => {
                Check((bool)Call(r,"Damage",20f,0f),"first hit rejected"); Near(r,"Health",80);
                Check(!(bool)Call(r,"Damage",20f,0.749f),"early hit accepted"); Near(r,"Health",80);
                Check((bool)Call(r,"Damage",20f,0.75f),"boundary hit rejected"); Near(r,"Health",60);
            });
            test("invalid damage cannot heal or poison health", r => {
                foreach (float amount in new[] { 0f, -5f, float.NaN, float.PositiveInfinity })
                    Check(!(bool)Call(r,"Damage",amount,0f),"invalid damage accepted");
                Check(!(bool)Call(r,"Damage",5f,float.NaN),"invalid time accepted"); Near(r,"Health",100);
            });
            test("death blocks form, attack, healing and damage", r => {
                Call(r,"ToggleForm"); Call(r,"CollectDew"); Call(r,"Damage",150f,0f);
                Near(r,"Health",0); Check(!Flag(r,"Butterfly"),"dead butterfly persists");
                Check(!(bool)Call(r,"ToggleForm"),"dead toggle"); Check((int)Call(r,"BeginAttack")==0,"dead attack");
                Check(!(bool)Call(r,"UseDew"),"dead healing"); Check(!(bool)Call(r,"Damage",1f,2f),"dead damage");
                Call(r,"Tick",10f,true); Near(r,"Energy",100);
            });
            test("flying drains 5 per second and auto returns at zero", r => {
                Check((bool)Call(r,"ToggleForm"),"cannot fly"); Call(r,"Tick",3f,true); Near(r,"Energy",85);
                Call(r,"Tick",17f,true); Near(r,"Energy",0); Check(!Flag(r,"Butterfly"),"no auto return");
                Check(!(bool)Call(r,"ToggleForm"),"zero energy flight"); Call(r,"Tick",2f,true); Near(r,"Energy",20);
                Call(r,"Tick",20f,true); Near(r,"Energy",100);
            });
            test("pause freezes both flight drain and body refill", r => {
                Call(r,"ToggleForm"); Call(r,"Tick",3f,false); Near(r,"Energy",100);
                Call(r,"Tick",2f,true); Call(r,"ToggleForm"); Call(r,"Tick",4f,false); Near(r,"Energy",90);
            });
            test("invalid dt does not advance simulation", r => {
                Call(r,"ToggleForm"); foreach(float dt in new[] {-1f,float.NaN,float.PositiveInfinity}) Call(r,"Tick",dt,true);
                Near(r,"Energy",100);
            });
            test("sigil requires flying and occupied shell plate, then latches", r => {
                Check(!(bool)Call(r,"ActivateSigil",true),"body unlocked"); Call(r,"ToggleForm");
                Check(!(bool)Call(r,"ActivateSigil",false),"empty plate unlocked");
                Check((bool)Call(r,"ActivateSigil",true),"valid sigil rejected"); Call(r,"ToggleForm");
                Check(Flag(r,"GateUnlocked"),"unlock lost on return");
            });
            test("dew only consumed when healing, capped at 100", r => {
                Check(!(bool)Call(r,"UseDew"),"empty inventory consumed"); Call(r,"CollectDew");
                Check(!(bool)Call(r,"UseDew"),"full health consumed"); Near(r,"DewCount",1);
                Call(r,"Damage",60f,0f); Check((bool)Call(r,"UseDew"),"healing rejected"); Near(r,"Health",75); Near(r,"DewCount",0);
                Call(r,"CollectDew"); Call(r,"UseDew"); Near(r,"Health",100);
            });
            test("one token hits once and exactly three attacks kill", r => {
                int a=(int)Call(r,"BeginAttack"); Check(a>0,"invalid attack token");
                Check((bool)Call(r,"HitEnemy",a),"first hit rejected"); Check(!(bool)Call(r,"HitEnemy",a),"duplicate hit");
                Check(!Flag(r,"EnemyDefeated"),"one hit killed"); Near(r,"EnemyHealth",2);
                int b=(int)Call(r,"BeginAttack"); Check(b!=a,"token reused");
                Check(!(bool)Call(r,"HitEnemy",a),"stale token hit"); Call(r,"HitEnemy",b); Near(r,"EnemyHealth",1);
                Call(r,"HitEnemy",Call(r,"BeginAttack")); Check(Flag(r,"EnemyDefeated"),"third hit did not kill");
                Check(!(bool)Call(r,"HitEnemy",(int)Call(r,"BeginAttack")),"dead enemy hit");
            });
            test("flight invalidates pending attack and cannot start another", r => {
                int token=(int)Call(r,"BeginAttack"); Call(r,"ToggleForm");
                Check((int)Call(r,"BeginAttack")==0,"butterfly attack"); Call(r,"ToggleForm");
                Check(!(bool)Call(r,"HitEnemy",token),"preflight token still valid");
                Check(!(bool)Call(r,"HitEnemy",0),"zero token hit");
            });
            test("respawn preserves progress but restart clears all", r => {
                Call(r,"ToggleForm"); Call(r,"ActivateSigil",true); Call(r,"CollectDew"); Call(r,"Damage",100f,0f);
                Call(r,"Respawn"); Check(Flag(r,"GateUnlocked"),"respawn lost gate"); Near(r,"DewCount",1); Near(r,"Health",100);
                Call(r,"Reset"); Check(!Flag(r,"GateUnlocked")&&!Flag(r,"EnemyDefeated")&&!Flag(r,"Butterfly"),"restart flags");
                Near(r,"DewCount",0); Near(r,"EnemyHealth",3); Near(r,"Health",100); Near(r,"Energy",100);
                Check((bool)Call(r,"Damage",1f,0f),"restart retained invulnerability");
            });
            return report.Append("RESULT ").Append(passed).Append(" passed, ").Append(failed).Append(" failed").ToString();
        }
        static object Call(object r,string name,params object[] args) { return r.GetType().GetMethod(name).Invoke(r,args); }
        static object Value(object r,string name) { var t=r.GetType(); var f=t.GetField(name); return f!=null?f.GetValue(r):t.GetProperty(name).GetValue(r,null); }
        static bool Flag(object r,string name) { return (bool)Value(r,name); }
        static void Near(object r,string name,float want) { Check(Math.Abs(Convert.ToSingle(Value(r,name))-want)<0.0001f,name+" expected "+want+", got "+Value(r,name)); }
        static void Check(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }
    }
}
