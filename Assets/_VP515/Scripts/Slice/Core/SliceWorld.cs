using System;
using System.Collections.Generic;
using UnityEngine;

namespace VP515.Slice
{
    // Parent supplies colliders/transforms. No scene discovery, UI or presentation dependencies.
    public sealed class SliceWorld : MonoBehaviour
    {
        public Collider2D PressurePlate;
        public Transform Sigil;
        public Collider2D Gate;
        public float InteractionRadius=1.25f;
        public event Action GateOpened;
        SlicePlayer player; SliceRules rules;
        readonly HashSet<GameObject> collected=new HashSet<GameObject>();
        public void Initialize(SlicePlayer target,SliceRules state)
        {
            if(!target || state==null) throw new ArgumentNullException(!target ? "target" : "state");
            if(player) player.InteractRequested-=Interact;
            player=target; rules=state; if(player) { player.Rules=state; player.InteractRequested+=Interact; }
        }
        void Update()
        {
            if(!player || rules==null) return;
            var feet=player.BodyPosition;
            player.ShellOnPlate=PressurePlate && PressurePlate.isActiveAndEnabled &&
                PressurePlate.bounds.SqrDistance(new Vector3(feet.x,feet.y,PressurePlate.bounds.center.z))<0.04f;
            if(Gate) Gate.enabled=!rules.GateUnlocked;
        }
        public bool TryActivateSigil()
        {
            if(!isActiveAndEnabled || !player || rules==null || !player.CanAct || !rules.Alive || !Sigil ||
                Vector2.Distance(player.ActivePosition,Sigil.position)>InteractionRadius) return false;
            bool unlocked=rules.GateUnlocked;
            bool result=rules.ActivateSigil(player.ShellOnPlate);
            if(result && !unlocked) { if(Gate) Gate.enabled=false; GateOpened?.Invoke(); }
            return result;
        }
        void Interact() { TryActivateSigil(); }
        public bool TryCollectDew(GameObject dew)
        {
            if(!isActiveAndEnabled || !player || rules==null || !player.CanAct || !rules.Alive || rules.Butterfly || !dew || !dew.activeInHierarchy ||
                Vector2.Distance(player.BodyPosition,dew.transform.position)>InteractionRadius || !collected.Add(dew)) return false;
            rules.CollectDew(); dew.SetActive(false); return true;
        }
        // Parent calls after Rules.Reset(); checkpoint death must NOT reset this registry.
        public void ResetWorld()
        {
            foreach(var dew in collected) if(dew) dew.SetActive(true);
            collected.Clear(); if(Gate && rules!=null) Gate.enabled=!rules.GateUnlocked;
        }
        void OnDestroy() { if(player) player.InteractRequested-=Interact; }
    }
}
