# BetterExperience 2.0 — Adaptation Plan (v6 → SMA 23.1)

*Generated: 2026-06-24*
*Source: v6 decompiled source + strings analysis of SMA 23.1 managed DLLs (191 assemblies)*

---

## Executive Summary

Most of v6 works against SMA 23.1 as-is. The three hard crashes that blocked 10.4e are either already fixed in 23.1's API or need minor surgery. The only substantial work is the Python scripting bridge, which needs to be rebuilt so existing `pycs` scene scripts run without modification.

**Total estimated effort:** 1–2 days.

---

## 1. API Compatibility Results

### 1.1 `DiccionarioDeNombresDeAlteradoresFemeninos` — ✅ NO CHANGE NEEDED

This was the primary crash on 10.4e (TypeLoadException at plugin load). It is **present in SMA 23.1** across multiple assemblies:

- `Assembly-CSharp.dll`
- `Assembly-D_C_Dependientes_ReSc.dll`
- `TValle.BeachGirl.Characters.Female.Alteradores.dll`
- `TValle.BeachGirl.MapasDeAlteradores.dll`
- (and others)

The specific constants used by v6 (`Scaler_Seno_R`, `Scaler_Seno_L`) are present. `SafetyNetFeature.cs` and `ModifierManager.cs` require no changes.

### 1.2 `IIKUpdater.onAllIKsUpdated` — ✅ NO CHANGE NEEDED

The event `onAllIKsUpdated` (past tense, as v6 uses) is present in `Base.RootMotion.BeachGirl.dll`:

```
add_onAllIKsUpdated      ✓
remove_onAllIKsUpdated   ✓
M_IIKUpdater_onAllIKsUpdated  ✓
```

The old 10.4e name `onAllIKsUpdating` is also still present in some assemblies, but that's the name v6 already moved away from. `IKFeature.cs` requires no changes.

### 1.3 `PelvisMovementController.currentLocalTarget` — ✅ NO CHANGE NEEDED

Property getter is confirmed in `Assembly-D_C_Dependientes_ReSc.dll`:

```
m_currentLocalTarget
get_currentLocalTarget    ✓
```

The read side of `PlayerCharacter.cs`'s `PelvisY` and `PelvisZ` properties works as written.

### 1.4 `PelvisMovementController.Control(Vector3)` — ⚠️ ONE-LINE FIX

`Control()` is NOT found in 23.1's `Assembly-D_C_Dependientes_ReSc.dll`. The method that sets a pelvis target position in 23.1 is:

```
MovePelvisTarget    ✓  (confirmed in assembly strings)
```

**Fix — `PlayerCharacter.cs` lines 96 and 108:**

```csharp
// BEFORE (v6 — won't compile against 23.1)
set => pelvisCtl.Control(new Vector3(0f, value, PelvisZ));
set => pelvisCtl.Control(new Vector3(0f, PelvisY, value));

// AFTER
set => pelvisCtl.MovePelvisTarget(new Vector3(0f, value, PelvisZ));
set => pelvisCtl.MovePelvisTarget(new Vector3(0f, PelvisY, value));
```

**Caveat:** Verify that `MovePelvisTarget(Vector3)` has the same semantics as `Control(Vector3)` — both should set the pelvis's local position target. If `MovePelvisTarget` has a different signature (e.g. takes a transform), adjust accordingly by checking the compiled DLL with ILSpy.

---

## 2. Python Scripting Bridge

This is the main work item.

### 2.1 Problem

v6 changed the Python-to-C# bridge from v5.2a:

| Version | Builtin exposed to Python | Access pattern |
|---|---|---|
| v5.2a | `__pycsrt` module | `__pycsrt.api.find_go_by_name(...)` |
| v6 | `pyrt` (Pyrt class) | `pyrt.get_main_strand()` etc. |

The existing `pycs/core.py` opens with:

```python
import __pycsrt as _pyrt
pyrt = _pyrt.api       # aliases the full game API
```

This fails immediately in v6 because `__pycsrt` is not exposed. All downstream calls (`pyrt.dialogue(...)`, `pyrt.play_clip(...)`, etc.) break.

### 2.2 What `__pycsrt.api` Exposed (v5.2a `PyStoryRuntime`)

The full public API of `PyStoryRuntime.cs` — all methods `pycs/core.py` calls:

| Method | Purpose |
|---|---|
| `find_go_by_name(root, *names)` | Deep-search for GameObject by name chain |
| `stop_dialogue()` | IEnumerator — ends active dialogue window |
| `send_notification(text, duration, fadeOut)` | Overlay info message |
| `is_guest_busy()` | True if an interaction is active |
| `get_guest_interaction_context()` | Returns `InteractionManager.CreateQueryContext()` |
| `guest_goto_poi(poiId, orientation=2)` | Walk guest to POI — returns WaitUntil |
| `guest_teleport_poi(poiId, orientation=2)` | Teleport guest to POI — returns WaitUntil |
| `dialogue(who, text)` | Show subtitle line — returns WaitUntil |
| `apply_posture(postureId)` | Apply posture to guest — returns WaitUntil |
| `play_clip(name, ...)` | Play named animation clip — returns WaitUntil |
| `install_character_interaction()` | Attach InteractiveObject to guest's head |
| `enumerate_clothes()` | List `SimpleCloth` objects currently equipped |
| `take_off_cloth(id)` | Remove clothing piece by ID |
| `set_eye_expression(exp, duration)` | Set eye expression via `OjosExpresionController` |
| `enumerate_transitions()` | List available posture-change interactions |
| `enumerate_gotos()` | List available goto interactions |
| `execute_interaction(interaction)` | Execute an Interaction object — returns WaitUntil |
| `dialogue_response(options)` | Show menu choices — returns WaitUntil |
| `get_last_response()` | Index of last chosen menu option |
| `set_look_at_target(transform, duration)` | Script the guest's look-at target |
| `make_interactive(go)` | Attach InteractiveObject to any GameObject |
| `gio_apply_genes_from_package(filename)` | Load and apply gene file from VFS |
| `Session` (property) | The current `GameSession` |
| `get_main_strand()` ✓ | Already in v6 Pyrt |
| `can_invoke_immediate(strand)` ✓ | Already in v6 Pyrt |
| `invoke_next(gen, strand)` ✓ | Already in v6 Pyrt |
| `invoke_last(gen, strand)` ✓ | Already in v6 Pyrt |
| `new_strand()` ✓ | Already in v6 Pyrt |
| `call_with_exception_trap(func)` ✓ | Already in v6 Pyrt |

### 2.3 What `__pycsrt.ai` Exposed (v5.2a `SimpleAi`)

```python
_pyrt.ai.Anger      # SimpleEmotion — .value, .add(v), .reset(), .on_max_value
_pyrt.ai.Pleasure   # SimpleEmotion
_pyrt.ai.Pain       # SimpleEmotion
_pyrt.ai.Consent    # SimpleEmotion
_pyrt.ai.SetBehaviorRoot(node)   # set the root BehaviorNode
```

### 2.4 Fix Strategy

**Do not change `pycs/core.py`.** Instead, expose a backward-compatible `__pycsrt` builtin alongside the existing `pyrt`.

In `SessionScriptFeature.cs`, extend `ExposeRuntime()`:

```csharp
private void ExposeRuntime()
{
    ScriptScope builtin = Python.GetBuiltinModule(python);

    // v6 API (for new scripts)
    Pyrt pyrt = new Pyrt(logger, scope, this, session);
    builtin.SetVariable("pyrt", (object)pyrt);

    // v5.2a compatibility shim (for existing pycs scripts)
    var compat = new PycsrtCompat(logger, scope, this, session);
    builtin.SetVariable("__pycsrt", (object)compat);
}
```

**`PycsrtCompat`** is a new inner class in `SessionScriptFeature.cs`:

```csharp
public class PycsrtCompat
{
    public PyStoryRuntimeCompat api { get; }
    public SimpleAiCompat ai { get; }

    public PycsrtCompat(Logger logger, ScopeSupport scope, ScriptHost host, GameSession session)
    {
        api = new PyStoryRuntimeCompat(logger, scope, host, session);
        ai  = new SimpleAiCompat(session, scope);
    }
}
```

### 2.5 `PyStoryRuntimeCompat` — Implementation Notes

Port `PyStoryRuntime.cs` from v5.2a into the v6 `SessionScriptFeature.cs` as a nested class. The services it needs come from the same places v6 already uses:

| Service | How to get it in v6 |
|---|---|
| `StoryManager` | `session.Scope.Lookup<StoryManager>()` (via `customScene.Scope`) — or delay-resolve when `Story.SceneScope` is ready |
| `POIManager` | `storyManager.Current.SceneScope.Lookup<POIManager>()` |
| `PoseManager` | `storyManager.Current.SceneScope.Lookup<PoseManager>()` |
| `InteractionManager` | `storyManager.Current.SceneInterviewScope.Lookup<InteractionManager>()` |
| `DialogueManager` | `storyManager.Current.SceneScope.Lookup<DialogueManager>()` |
| `OverlayService` | `storyManager.Current.SceneScope.Lookup<OverlayService>()` |
| `VirtIO` (for gene files) | `plugin.virtIO` (already on `ScriptHost`) |

The v5.2a `PyStoryRuntime` subscribed to `Story.SceneScopeCreated` and `Story.InterviewScopeCreated` to initialize these lazily. Use the same pattern — subscribe in the constructor, resolve services in the callbacks.

**Stub methods** (implement as no-ops or throw on first use, then fill in):
- All 20+ methods listed in §2.2

**Key implementation detail:** v5.2a's `ScriptingContext.ScriptingScope` maps to v6's `StrandService.scriptingScope`. The `ScriptHost` in v6 already is a `StrandService`, so `host.scriptingScope` is the equivalent scope.

### 2.6 `SimpleAiCompat` — Implementation Notes

Port `SimpleAi.cs` from v5.2a. The session emotion components are accessed identically:

```csharp
EmocionesFemeninas emotions = session.Guest.Impl.GetComponentInChildren<EmocionesFemeninas>();
Pleasure = new SimpleEmotion(emotions.placer);
Anger    = new SimpleEmotion(emotions.rage);
Pain     = new SimpleEmotion(emotions.dolor);
Consent  = new SimpleEmotion(emotions.consentToHero);
```

However, `SimpleAi` was a full `SessionService` in v5.2a which also drove stimulus reaction processing (`StimulusTrackingService`, `BehaviorNode`). For the 2.0 port:
- If `Behavior` / `BehaviorNode` / stimulus routing is needed: port `SimpleAi` as a full `SessionService` registered via `SessionTracker.SessionServices`, and expose it to Python via `__pycsrt.ai`.
- If only emotion stats are needed (simpler case): create a lightweight `SimpleAiCompat` that just wraps the `EmocionesFemeninas` component without the stimulus pipeline.

The doc_office demo scripts only read `_pyrt.ai.Anger`, `Pleasure`, `Pain`, `Consent` and call `_pyrt.ai.SetBehaviorRoot(...)`. Full stimulus routing is needed for reaction-driven scenes.

---

## 3. Implementation Order

Work items in recommended order:

1. **`PlayerCharacter.cs` — `Control()` → `MovePelvisTarget()`**
   - File: `BetterExperience/2.0/BetterExperience/BetterExperience.Wrappers.Characters/PlayerCharacter.cs`
   - Lines 96, 108
   - 5 minutes

2. **Stub build** — Confirm the four DLLs compile against 23.1 before touching the Python bridge
   - Run `build.bat` — expect zero errors except possibly the `Control()` method above
   - Fix any other compile errors discovered (may be none)

3. **Port `PyStoryRuntimeCompat`** into `SessionScriptFeature.cs`
   - Copy `PyStoryRuntime.cs` method bodies (v5.2a source is in `BEU52a_CS/BepInEx/plugins/decompile/Better_Story/BetterExperience.PyStory.Scripting/PyStoryRuntime.cs`)
   - Update service resolution to use v6's session/scope pattern
   - Add scope event subscriptions for lazy service init

4. **Port `SimpleAiCompat`** — decide full vs. lightweight based on whether BehaviorNode support is needed

5. **Wire up `__pycsrt` builtin** in `SessionScriptFeature.ExposeRuntime()`

6. **Integration test** — load the `doc_office` package and run through its scenes

---

## 4. Files to Create / Modify

| File | Action | Effort |
|---|---|---|
| `BetterExperience/2.0/BetterExperience/BetterExperience.Wrappers.Characters/PlayerCharacter.cs` | Copy from v6, fix `Control()` | trivial |
| `BetterExperience/2.0/Better_Story/BetterExperience.PyStory/SessionScriptFeature.cs` | Copy from v6, add `PycsrtCompat` + `PyStoryRuntimeCompat` + `SimpleAiCompat` inner classes | main work |
| All other v6 .cs files | Copy from `update60e/decompiled/` into `2.0/` folders as-is | copy only |

Everything except the Python bridge and the single-line Pelvis fix is a straight copy of v6 source into the 2.0 project.

---

## 5. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `MovePelvisTarget` has wrong signature | Low | Low | One-line re-fix; also can guard with `#if` |
| 23.1 has renamed other types v6 uses | Low | Medium | Run stub build first — compiler will report all missing references |
| `StoryManager` scope access differs in 23.1 | Medium | Medium | Use `Lookup<StoryManager>()` after verifying it's registered in `customScene.Scope` |
| IronPython 3.4.1 behavior differs from v5.2a's IronPython version | Low | Medium | Test with `doc_office` load — most differences are numeric/typing edge cases |
| `StimulusTrackingService` namespace moved in 23.1 | Low | Low | Verify via `strings` on `Assembly-D_E_Chuchi_ReSc.dll` if needed |
