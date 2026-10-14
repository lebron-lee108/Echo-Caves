namespace VP515.Slice
{
    // Pure rules. Tick and Damage use game time supplied by the adapter (never wall time).
    public sealed class SliceRules
    {
        public bool Butterfly, GateUnlocked, EnemyDefeated;
        public float Health = 100, Energy = 100;
        public int DewCount;
        public int EnemyHealth { get; private set; } = 3;
        public bool Alive { get { return Health > 0; } }
        float nextDamageTime = float.NegativeInfinity;
        int sequence, activeAttack;
        bool attackHit;
        static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
        public void Reset()
        {
            GateUnlocked = EnemyDefeated = false; DewCount = 0; EnemyHealth = 3; Respawn();
        }
        // Checkpoint recovery preserves gate, inventory and enemy progress. Reset is a new run.
        public void Respawn()
        {
            Butterfly = false; Health = Energy = 100; activeAttack = 0;
            nextDamageTime = float.NegativeInfinity;
        }
        public bool Damage(float amount, float now)
        {
            if (!Alive || !Finite(amount) || amount <= 0 || !Finite(now) || now < nextDamageTime) return false;
            Health = System.Math.Max(0, Health - amount); nextDamageTime = now + 0.75f;
            if (!Alive) { Butterfly = false; activeAttack = 0; }
            return true;
        }
        public void Tick(float dt, bool advancing)
        {
            if (!advancing || !Alive || !Finite(dt) || dt <= 0) return;
            Energy = System.Math.Max(0, System.Math.Min(100, Energy + dt * (Butterfly ? -5 : 10)));
            if (Butterfly && Energy == 0) { Butterfly = false; activeAttack = 0; }
        }
        public bool ToggleForm()
        {
            if (!Alive || (!Butterfly && Energy <= 0)) return false;
            Butterfly = !Butterfly; activeAttack = 0; return true;
        }
        public bool ActivateSigil(bool bodyOnPlate)
        {
            if (!Alive || !Butterfly || !bodyOnPlate) return false;
            GateUnlocked = true; return true;
        }
        public bool UseDew()
        {
            if (!Alive || Health >= 100 || DewCount <= 0) return false;
            DewCount--; Health = System.Math.Min(100, Health + 35); return true;
        }
        // The world owns object identity: call once for each collected object.
        public void CollectDew() { if (Alive) DewCount++; }
        public int BeginAttack()
        {
            if (!Alive || Butterfly) return 0;
            sequence = sequence == int.MaxValue ? 1 : sequence + 1;
            activeAttack = sequence; attackHit = false; return activeAttack;
        }
        public bool HitEnemy(int attackToken)
        {
            if (!Alive || Butterfly || EnemyDefeated || attackToken <= 0 || attackToken != activeAttack || attackHit) return false;
            attackHit = true; EnemyHealth--; EnemyDefeated = EnemyHealth == 0; return true;
        }
    }
}
