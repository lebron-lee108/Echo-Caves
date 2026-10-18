# Echo Caves — C-role uGUI handoff

This is AI-assisted C-role UI presentation/authoring. It implements editable uGUI and local screen navigation, not gameplay, AI, audio playback, economy, progression or saving. Sample values and screenshots of these views must be described as UI previews.

## Latest HUD default cleanup (source only; main rebuilds its new root/prefab later)

Build now leaves only Vitals and Objective visible within HUD. `HUD/BossStagger`, `HUD/RecordingSnapshot`, `HUD/FeedbackSnapshot` and `HUD/PreviewNavigation` are inactive by default, with all their existing children, serialized references and button events retained. Boss still has exactly three authored white square markers; no active Boss display before B requests that state. Main's existing review component owns F1 toggling of the retained PreviewNavigation object. This worker added no key polling or gameplay state.

`AssertCleanHUD(root)` checks all four inactive groups and the two active groups, and is called by AssertRoot. The preview-label contract now checks the retained authored labels even when their example groups are hidden. `ValidateTextLayout` explicitly activates the four HUD example groups on the disposable root for its HUD measurement, then hides them before measuring other screens and again in finally. It restores HUD and asserts the clean default. `CleanHUDRegression()` checks default cleanliness, that measured label count equals all authored TMP labels including inactive children, and cleanliness after the audit. Native RED was observed (`BossStagger` initially active); updated native Validate passed with all **161 labels** measured, no overflow and clean HUD restored.

The main agent's current PrefabPath (`EchoCavesUI-v02.prefab`) was preserved, not reverted. No scene UI rebuild, prefab export or existing prefab mutation was performed; main will rebuild its own root and choose its later v03 prefab destination. Earlier all-state HUD screenshots are superseded as default-state evidence.

## Latest correction: settings return and board-18 menu overlay

Runtime-only settings fix: `OpenSettings()` now returns immediately if Settings is already current, preserving the first caller. No serialized fields, event signatures or prefab references changed. The native regression was first observed failing (`MainMenu` expected, `Settings` actual), then passed after the method fix: open Settings three times + Back returns to MainMenu, then repeats successfully from Pause. This regression is also part of `Validate()`. Existing prefab component methods pick up the script fix without a prefab rebuild.

After re-reading approved `images/18.png`, MainMenu was corrected from an opaque dashboard to a chapter-camera overlay: no fullscreen background Image, translucent navy backing over only the left 720/1600 pixels (45%), retained full thin brass frame, ivory 80px title at (140,190), four 450px-wide buttons at x160 and y420/494/568/642. The right waveform, pitch and generic header were removed; the small bottom preview navigation/disclaimer remain. No baked image, terrain or camera is supplied. Main will rebuild ONLY its own new UI root/prefab and select the chapter camera for the final composition.

Native Validate passed after both corrections: 13 views, **161 labels / zero overflow**, repeated settings return, persistent input references, map topology and all other contracts. Added assertions enforce menu transparency, left 45% backing, title/button anchors and removal of the old waveform/tagline.

Latest native UI-only images: `E:/VP515/Evidence/EchoCaves/UI/native-13screens-20261019-v02/`. Visually rechecked MainMenu, Map and Radio. MainMenu's blank navy right side is the disposable capture camera's clear color, not an opaque UI covering the chapter; the scene integration capture belongs to main. Strict reference-quality limitations remain: live cave scenery, pixel-art logo treatment, character/radio-machine art and focus/hover-in-game composition are not proven by these isolated images. Map remains a readable corrected schematic; Radio remains a navy UI layout with a decorative waveform and explicit sample-state labels, not the illustrated radio scene in board 23. Do not call those images full visual fidelity or environment-art completion. The v01 menu image and earlier waveform-menu description are superseded.

Only the three owned UI scripts were explicitly imported for the native method regression; no global Refresh, scene save, main-scene mutation or prefab export was invoked. Both disposable validation/capture guards preserved the shared scene identity/root IDs/dirty state.

## Native capture delivery and separate read-only API audit

Latest explicit authorization added `public static string[] CaptureScreens(string folder)`: it BUILDS UI under a disposable preview host, assigns a preview camera/1600×900 RenderTexture, uses ScreenSpaceCamera/worldCamera with planeDistance 1, switches all thirteen views and forces Canvas/TMP updates before each render/readback/PNG. The no-argument and GameObject overloads still clone an existing root. `CaptureScreensToFolder(string folder)` is the equivalent non-overloaded CLI adapter, because Pipeline run_script v1 rejects overloaded entry names. Existing PNG evidence is never overwritten. Finally restores RenderTexture.active, destroys temporary objects/textures and closes the preview scene; active scene identity, root IDs and dirty-state guards passed.

Worker actually invoked only the disposable folder capture, producing thirteen distinct native PNGs at `E:/VP515/Evidence/EchoCaves/UI/native-13screens-20261019-v01/`. All PNG headers were checked: 13/13 are 1600×900. Visually inspected `01-MainMenu.png`, `05-Map.png` and `08-Radio.png`: readable CJK/English, thin brass frames, no obvious text clipping; map arrows show surface walk-in before the shaft, A→D right, D down to E, E→F→G left and the same shaft up. Radio shows a single left soundscape/player, right listeners/copies, 00:20/01:00, the 15–25 second highlight and retained masters. Main menu and radio are UI-only navy presentations with editable waveform motifs; they are not evidence of reference-board cave art integration. Main owns remaining art and scene-composited visual acceptance.

The latest disposable native Validate also passed all contracts and 165 labels across thirteen views. No prefab was exported and no main-scene UI/camera/save operation was invoked.

Separate read-only API audit (manual second pass, not an external reviewer) found no blocker for this capture/authoring scope, with these integration caveats:

- `Validate()` audits freshly built disposable UI, not arbitrary manually edited scene instances. Main should run `AssertRoot(newRoot)` after its rebuild, and check its actual EventSystem/input module separately. Build reuses an existing scene EventSystem without validating that external module's configuration.
- Historical audit finding, now FIXED: repeated `OpenSettings()` previously overwrote the return destination. The runtime guard and native repeat-open regression above supersede that finding.
- The isolated prefab includes an EventSystem; placing it into an existing scene can create duplicate active systems. Main should ensure exactly one when it later exports/integrates. BuildPrefabs intentionally refuses overwriting an existing prefab.
- `CaptureScreens(null)` is ambiguous between string/GameObject overloads; use a typed folder string or root. CLI callers should use CaptureScreensToFolder. Partial failed runs preserve their PNGs and need a new destination on retry.
- Folder-capture text/structure are validated separately by Validate; successful PNG output alone is not gameplay, button-input, save/economy, art-fidelity or full scene acceptance.

## Native text-layout validation update

With the user's new permission to invoke disposable validation only, the worker reproduced `Vitals/Abilities` overflow, then measured all thirteen screens while each screen was active. The complete native failure set was eleven labels: HUD Abilities (required height 49.2 vs 45), Objective Heading (30.4 vs 30), MainMenu Wordmark (101.4 vs 100), Pause Title (60.8 vs 60), Dialogue Speaker (39.1 vs 39), four SoundId labels (43.4 vs 40), Radio Recording (46.3 vs 46), Radio Clock (36.2 vs 36).

Source layout fixes increase those heights to 56, 36, 110, 68, 44, 48, 52 and 42 respectively, and extend the enclosing Vitals card from 146 to 160. Font sizes, content and overflow assertions are retained. No auto-shrink, clipping, truncation or test bypass was added. `ValidateTextLayout` now activates and audits all views, checks both TMP overflow and preferred width/height, aggregates every error with measured sizes, and restores HUD. It rejects a shared-scene root. Validation objects are explicitly named Disposable_NotShared and marked DontSave. A finally guard verifies the active main scene identity, root IDs and dirty state are preserved.

Native source execution via Pipeline `run_script`, entry `EchoCaves.Editor.EchoUIBuilder.Validate`, passed: **165 labels / 13 views, zero overflow**, plus existing navigation, default HUD, map, samples and persistent input contracts. The worker did not refresh, save or rebuild the shared scene, simulate device input, generate a prefab or invoke capture. Native validation is now tested; previous below-the-line notes describing no native calls are historical handoff status.

Already-authored scene objects retain their old serialized dimensions until main applies the source updates. Main can invoke `EchoUIBuilder.ApplyTextLayoutFixes(existingEchoUIRoot)` to update exactly those twelve RectTransforms (11 labels + Vitals), with Undo and prefab-instance override recording. This deferred migration neither rebuilds UI, changes view selection nor saves the scene; the worker has not invoked it on the main scene.

## Input-reference repair after main's native validation

Main reported scene checks 46/46 passing but UI validation failing with `Prefab input references must survive serialization`. Read-only inspection found all six reference subassets already serialized in `EchoUIPreviewInput.asset`. The installed Input System module's `actionsAsset` setter calls `UpdateReferenceForNewAsset`, which creates transient references; `SwapAction` then skips replacement if the new reference resolves to the same action. Editor Reset can populate module slots even when its GameObject is inactive. Consequently, adding subassets alone did not ensure the module stored those subassets.

The fix clears all ten old module action-reference slots before setting `actionsAsset`, then assigns the six persistent UI subassets. Existing asset/reference identities are reused; only missing reference subassets are added/saved. No existing asset is deleted, no global save/refresh is called. The strengthened contract checks all six module slots, their actual SerializedObject fields, subasset paths, action resolution and persistent GUID/local IDs. `Validate()` additionally injects a transient same-action Point reference into its disposable instance, repairs it, and verifies reference identity after two repairs.

Main should refresh/compile, then call `EchoUIBuilder.Validate()` again. No input-asset deletion or scene rebuild is required. If an already-created scene instance contains the old module references, main can explicitly call `EchoUIBuilder.RepairPreviewInput(existingEchoUIRoot)`; this only repairs input modules inside that owned UI root and does not save the scene. It does not touch a reused external EventSystem. If a prefab was already saved with bad references, main must load its contents, repair that isolated root, explicitly save the owned prefab, unload, then run `ValidatePrefab()`; normal `BuildPrefabs()` still refuses to overwrite authored prefabs.

`public static string[] EchoUIBuilder.CaptureScreens()` is an optional main-owned deferred capture entry point. It locates the single existing UI root in the active scene; `CaptureScreens(GameObject sourceRoot)` selects it explicitly. It CLONES that existing root into a disposable preview scene rather than rebuilding any UI, temporarily uses ScreenSpaceCamera on the clone, renders all thirteen views at 1600×900 into a native RenderTexture, and returns thirteen PNG paths under `Evidence/EchoCaves/UI/<unique-run>/`. SRP uses supported StandardRequest rendering; the built-in pipeline uses Camera.Render. Uniform images throw rather than reporting a successful capture. Main's existing Canvas/cameras/views are never modified. RenderTexture.active is restored and temporary resources/scene are disposed. HUD/dialogue captures have a solid navy backdrop and are explicitly UI-only, not terrain/gameplay captures; actual scene-composited captures remain main's responsibility. Capture has not been executed by this worker. Apply the deferred layout migration first if capturing an older serialized UI instance.

After this repair, separate runtime/Editor offline compilation passed with exit 0. Native repair, prefab round trip and capture remain unexecuted here because main owns Unity. Default Build visibility and every non-input UI design remain unchanged.

## Entry points (main agent owns Unity)

```csharp
// Read-only prerequisite check before Build: no hierarchy or asset writes.
Debug.Log(EchoCaves.Editor.EchoUIContractTests.Preflight());

// Creates a temporary preview hierarchy, runs contracts/navigation/text-bound checks, then disposes it.
// First invocation also creates the new owned TMP font and UI-only input assets.
Debug.Log(EchoCaves.Editor.EchoUIBuilder.Validate());

// Author into the main agent's chosen scene/parent. No scene save is performed here.
GameObject canvas = EchoCaves.Editor.EchoUIBuilder.Build(parent: null);

// Generate a complete, editable prefab and verify its serialized round trip.
EchoCaves.Editor.EchoUIBuilder.BuildPrefabs();
Debug.Log(EchoCaves.Editor.EchoUIBuilder.ValidatePrefab());

// On a freshly authored scene instance:
Debug.Log(EchoCaves.Editor.EchoUIContractTests.AssertRoot(canvas));
Debug.Log(EchoCaves.Editor.EchoUIContractTests.AssertNavigation(canvas));
```

Exact required API:

- `public static GameObject EchoCaves.Editor.EchoUIBuilder.Build(Transform parent = null)`
- `public static void EchoCaves.Editor.EchoUIBuilder.BuildPrefabs()`
- `public static string EchoCaves.Editor.EchoUIBuilder.Validate()` (summary or exception)

`BuildPrefabs()` deliberately refuses to overwrite an existing prefab, preserving manual UI edits. For regeneration, main must choose how to preserve those edits first. `ValidatePrefab()` loads an isolated copy and does not save changes. `Build()` is explicit, not an InitializeOnLoad hook; it neither refreshes the database nor saves a scene or unrelated dirty assets.

## Owned paths

Source delivered here:

- `Assets/_EchoCaves/UI/EchoUIBindings.cs`
- `Assets/_EchoCaves/UI/Editor/EchoUIContractTests.cs` (first source file written, before implementation)
- `Assets/Editor/EchoCaves/EchoUIBuilder.cs`
- `Assets/_EchoCaves/UI/README-C-UI.md`

Generated only when main invokes the API:

- `Assets/_EchoCaves/UI/Fonts/EchoCavesUI-SDF.asset` with material/atlas subassets
- `Assets/_EchoCaves/UI/Input/EchoUIPreviewInput.asset` with persistent UI action references (Input System builds)
- `Assets/_EchoCaves/UI/Prefabs/EchoCavesUI.prefab`
- Unity-generated `.meta` files for new assets/folders; main should retain them normally.

The source font is expected at `Assets/_EchoCaves/Fonts/NotoSansCJKsc-Regular.otf`. The builder reads it and never replaces it. TMP shaders/resources already exist in this project; missing prerequisites produce actionable errors, never an interactive importer.

## Hierarchy and presentation

1600×900 Screen Space Overlay, Scale With Screen Size, match 0.5, sorting order 100. Root has Canvas, CanvasScaler, GraphicRaycaster and serialized `EchoUIBindings`. HUD alone is active by default. Thirteen direct view children: HUD, MainMenu, Pause, Settings, Dialogue, Map, SoundLibrary, Playback, Radio, Shop, Settlement, Fail, End.

All text, frames, waveforms, markers, tracks, cards and buttons are editable native objects. No flattened reference boards. All buttons share one authoring style: navy `#081728` fill, one-pixel brass `#c39a57` border, cyan `#57dbe5` hover/keyboard focus, ivory text, visible pressed/disabled states. Bottom HUD navigation is explicitly a preview toolbar. Dialogue and HUD leave the scene visible; the other views use opaque navy panels. Main menu includes an editable soundscape motif rather than pretending a review-board image is final scene art. Portrait is explicitly a D-owned placeholder.

The isolated prefab supplies an EventSystem with persistent UI-only pointer, click, scroll, arrow navigation and submit/cancel actions. `Build(parent)` reuses an active EventSystem in the same scene if present and does not change it. If main instantiates the prefab into a scene already containing one, disable/remove the prefab's `PresentationEventSystem` in the scene instance. Keep exactly one active EventSystem. No project input setting is changed. Escape is an input cancel action, not a global pause/gameplay shortcut.

## Programmer integration

`EchoUIBindings.Views` contains serialized panel and first-selection references. `ShowView((int)EchoUIView.Map)`, `OpenSettings()` and `BackFromSettings()` only switch UI. Settings remembers the view that opened it. No runtime UI rebuilding. No `Update`, timers, audio sources, PlayerPrefs, save operations, gameplay dependencies or scene loads.

Buttons store persistent Inspector-visible calls. Business buttons stay on their current page and invoke these empty UnityEvents for B:

| Intent | Event |
| --- | --- |
| Start / continue / quit | `onNewGameRequested`, `onContinueGameRequested`, `onQuitRequested` |
| Resume / main menu | `onResumeRequested`, `onReturnToMenuRequested` |
| Dialogue next / skip | `onDialogueContinueRequested`, `onDialogueSkipRequested` |
| Audition / world playback | `onAuditionRequested(string soundId)`, `onPlaybackRequested(string soundId)` |
| Broadcast / one highlight | `onBroadcastRequested`, `onHighlightRequested` |
| Battery purchase / settlement acknowledgement | `onPurchaseBatteryRequested`, `onSettlementAcknowledgeRequested` |
| Recovery / save-and-return | `onRetryFromRestRequested`, `onSaveAndReturnRequested` |
| Master / music / effects / ambience sliders | `onMasterVolumeRequested(float)`, `onMusicVolumeRequested(float)`, `onEffectsVolumeRequested(float)`, `onAmbienceVolumeRequested(float)` |
| Shake / text speed | `onScreenShakeRequested(bool)`, `onTextSpeedRequested` |

B should feed real text through the serialized TMP label references, and real panels through `Views`; the exposed example numbers are documentation fixtures, not an economy model. Sliders/toggle show their own control state and emit requests; audio, settings persistence and text speed are not implemented. Percent labels explicitly say sample.

Radio: one 60-second sample player, static `00:20 / 01:00`; 15–25 second cyan band and a single highlight request. The 3-listener / 12-yuan display is a mid-program snapshot, distinct from the final settlement sample. Final sample is 6 copies × 4 = 24, electricity 2, net 22. Optional battery is 18, capacity 100→120, projected balance 4. Insufficient-funds card is a separate disabled sample. Masters remain retained. No second player, shopping carts, oxygen or transformation mechanics.

Map is hand-authored nodes/lines, not board 21's generated connectors. Surface: Outside→WalkIn→MapBoard→ShaftTop. One vertical shaft sits left of underground rooms. ShaftMiddle→A→B→C→D; D down to E through a descending passage; E→F→G travels left to ShaftBottom, then returns up the same shaft. Three landings have identical x coordinates. Arrows denote travel direction. Map is not to scale and does not teleport. Boss snapshot has exactly three white square markers and no HP slider.

## Original handoff verification status (superseded by native text-layout update above)

No Unity command, native write, refresh, scene/prefab generation or capture was invoked by this worker. The contract assertion file was written first; the user explicitly permitted handing native RED/GREEN execution to main, so no native failing-test or passing-test claim is made. Final-source offline Roslyn compilation against this project's installed Unity/TMP/UI/InputSystem assemblies passed with exit 0, with runtime and Editor compiled as separate assemblies. Compiler errors encountered during authoring were corrected. Temporary compiler outputs are outside the project. This is compiler evidence only, not Unity integration or visual acceptance.

Main should import these new source files, run `Preflight()` then `Validate()` before scene Build, then `BuildPrefabs()` for serialization checks. Capture all thirteen views at 1600×900 and at another 16:9 size; verify CJK glyphs, text bounds, map alignment/arrows, button hover/pressed/disabled state, actual mouse/keyboard clicks, and one active EventSystem. Assertions exercise default visibility, every view switch, settings return, purchase intent with no balance mutation, persistent fonts/button callbacks/input references, map topology and numeric snapshots. Tests restore HUD after navigation. Text-bound checks do not replace visual review; no gameplay/teammate integration or course acceptance is asserted.
