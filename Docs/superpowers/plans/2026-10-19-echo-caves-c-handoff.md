# Echo Caves C Handoff Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans. User explicitly chose continuous execution, with independent UI/docs sidecars; no per-part review pauses.

**Goal:** Deliver an editable first chapter environment and UI handoff in existing Unity project, rename and push only authorized changes.
**Architecture:** Isolated new asset namespace; deterministic Editor builder consumes layout model and modular environment art, produces serialized continuous scene, prefabs and validation evidence. UI has presentation events but no business/gameplay implementation.
**Tech Stack:** Unity6000.3.23f1, URP17.3, uGUI/TMP, live Unity CLI, Git/LFS.
**Spec:** Docs/superpowers/specs/2026-10-19-echo-caves-c-handoff.md

## Global Constraints
Preserve dirty original files, no main merge/forcepush, no purchased/private raw-reference publication, no gameplay/character animation replacement. Work in original E:/VP515 as explicitly requested; no new worktree/editor project.

## Review Focus
1. Same shaft must connect entrance/A/G; D→E is cave descent, not another elevator.
2. No disconnected mandatory path after adding rugged terrain or rope bridges; A first gap retains catch ledge.
3. Parallax renders independent layers and does not open gaps during camera travel.
4. UI readable at common aspect ratios, no stale generated-route map or fake functional business buttons.
5. Fresh clone resolves all new assets/metas/font license; no dependency on untracked old city materials or local absolute art paths.

## Tasks
- [ ] 1 Baseline and contracts: capture status, scene dirty/pipeline, original settings hash; author scene validation before scene exists and watch fail.
- [ ] 2 Resource/scale/layout: modular cave source assets, model for terrain/anchors/routes; resource ownership records. Test required region/shaft/bridge invariants.
- [ ] 3 Scene authoring: Editor-only builder creates serialized terrain/prefabs/backgrounds/device-state placeholders/layering/collision and review camera. Test real native hierarchy and physical route probes; capture every region.
- [ ] 4 UI: independent authoring unit builds editable uGUI and named UnityEvents. Test all panel existence, inputs, missing refs, topology, layout; capture native UI.
- [ ] 5 Polish and handoff: inspect real captures, correct geometry/overdraw/seams, docs + exports; verify original content untouched.
- [ ] 6 Rename/publish: Editor ProductName only, rename existing remote via authenticated existing session; scoped commit on existing feature branch, LFS push, remote hash/readback + fresh-clone dependency validation. Report permission block if any, never invent success.

## Execution rulings
- User explicitly overrides repeated design/plan review stops; this file records already approved scope and continuous method.
- Keep original checkout because user specifically requests existing Unity project and feature branch. Directory isolation is new _EchoCaves namespace; do not stash/reset dirty work.
- Scene assets/config are Editor-produced; state is validated natively. Tests before each authored behavior; visual quality additionally needs actual rendered review.
