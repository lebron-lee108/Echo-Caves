# Chapter One Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans task-by-task. Steps use checkbox syntax. User explicitly requests making the playable demo first without further text-only style confirmation; proceed within this spec, record deviations, then submit the playable result for review.

**Goal:** Deliver a small, polished, extendable VP515 first-section playable.

**Architecture:** Pure C# rule state drives MonoBehaviour player/enemy/world adapters. A standalone uGUI presentation receives a HUD snapshot and invokes actions. A Unity Editor builder creates scene/assets using APIs; AI bitmap assets are generated independently and imported with bilinear filtering.

**Tech Stack:** Unity 6000.3.23f1, URP 2D, Input System, uGUI, TextMeshPro, existing Git/LFS.

**Spec:** Docs/superpowers/specs/2026-10-15-chapter-one-slice.md

## Global Constraints

- Windows PC, Unity 6000.3.23f1; preserve Foundation/SampleScene and package versions.
- Non-pixel hand-painted, no reference footage/screenshots used as runtime assets.
- Local feature branch; no main merge or deployment without review.
- Scene/prefab/serialized assets authored by Unity APIs only.
- English runtime UI; Chinese handoff; AI work attribution accurate.

## Review Focus

- UI clicks must not leak into melee after closing a panel.
- Paused/open-panel state must stop health/energy/enemy progression.
- Body or butterfly out of bounds must recover rather than softlock.
- Death and restart reset transient objects and preserve only intended checkpoint state.
- Different aspect ratios must keep menus, targets and readable HUD in bounds.

### Task 1: Rule state and gameplay adapters

**Files:** Assets/_VP515/Scripts/Slice/Core/SliceRules.cs; SlicePlayer.cs; SliceEnemy.cs; SliceWorld.cs; Assets/Editor/VP515Slice/SliceRuleTests.cs.

**Interfaces:** Namespace VP515.Slice. SliceRules owns Health (100), Energy (100), DewCount, GateUnlocked, EnemyDefeated; Reset(), Damage(float,float), Tick(float,bool), ToggleForm(), ActivateSigil(bool), UseDew(), and BeginAttack/HitEnemy attack-token behavior. Runtime adapters expose player, progress and session events to bootstrap. Keep all API names documented in source summary.

- [ ] Write rule tests with independently derived health/invulnerability/pause/energy/restart expectations; run absent rules to observe RED.
- [ ] Implement rules and run the same tests GREEN.
- [ ] Add Input System player, shell, enemy and world adapters; test collision/form/attack/restoration integration in Play mode.
- [ ] Commit only reviewed task files, tests and Unity-generated meta.

### Task 2: Functional uGUI presentation

**Files:** Assets/_VP515/Scripts/Slice/UI/SliceUI.cs; SliceUIState.cs; Assets/Editor/VP515Slice/SliceUITests.cs.

**Interfaces:** UI owns SliceView enum {Menu,Playing,Paused,Inventory,Settings,Controls,Dead,Complete}; SliceHudData struct Health, Energy, DewCount, Objective, Prompt, Butterfly, GateUnlocked, EnemyDefeated, Elapsed. SliceUI.Build(TMP_FontAsset font), Show(SliceView), UpdateHUD(SliceHudData), ShowToast(string). Public Action callbacks StartRequested, ResumeRequested, RestartRequested, MenuRequested, QuitRequested, DewRequested; Action<float> VolumeChanged; Action<bool> ShakeChanged. SetSummary(float seconds,bool enemyDefeated,int collected); SetSettings(float volume,bool shake).

- [ ] Define scene-level UI tests: menu click callback, paused HUD visibility, unavailable dew disabled, controls/ settings back navigation; observe RED.
- [ ] Build real Canvas/EventSystem with TMP text and panels, wire callbacks, make GREEN.
- [ ] Verify layout at1280x720/1920x1080 and that closing clicks do not reach gameplay.
- [ ] Commit reviewed UI source and meta.

### Task 3: Art, world layout and scene builder

**Files:** Assets/_VP515/Art/Slice/; Assets/Editor/VP515Slice/SliceBuilder.cs; Assets/_VP515/Scripts/Slice/Presentation/SliceAtmosphere.cs; SliceAudio.cs; SliceBootstrap.cs.

**Interfaces:** SliceBuilder.Create(), Validate(), BuildWindows(). Bootstrap builds bindings between gameplay and SliceUI; receives generated sprites, no dependence on external default image paths. Camera/particles/audio animate only allowed states; audio volume uses saved preferences.

- [ ] Generate original background, transparent scenery and character assets; inspect alpha, style and bounds; save in project and record prompts.
- [ ] Create immutable-scoped ChapterOneSlice scene with APIs, serialized sprites/ prefabs and grounded movement paths.
- [ ] Implement layered view, responsive camera, readable feedback and original synthesized audio.
- [ ] Run Validate for colliders, root state, UI bindings, asset references and active scene; report failures honestly.

### Task 4: Integration and delivered build

**Files:** Assets/Editor/VP515Slice/SlicePlayTests.cs; Docs/Slice/Play-Guide.md; Docs/Slice/Verification.md; Docs/Asset-Sources.md; Docs/Contributions.md.

- [ ] Run rule/UI suite plus real input journey; fail on menu/pause/restart regressions, fix root causes.
- [ ] Capture actual menu, gameplay and HUD screenshots; inspect rather than infer polish from compilation.
- [ ] Build Windows to Builds/Windows/ChapterOneSlice and verify launch.
- [ ] Fresh review of changed branch; fix important findings with regression tests.
- [ ] Hand off executable and source with precise tested/unverified limits; do not auto-merge main.
