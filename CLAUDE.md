# BetterExperience → SMA 23.1 Adaptation — Reference Guide

## Project Layout
```
betterexperience_2.0_conversion/
  BetterExperience/2.0/BetterExperience/   ← C# source (4 .csproj files)
  build.bat                                 ← runs MSBuild for all 4 DLLs
  commit-and-push.bat                       ← git commit + push
  GameRefs.props                            ← shared HintPath references
```

## The 4 DLLs (build in this order)
| Project | Status |
|---------|--------|
| `BetterExperience/BetterExperience.csproj` | All 63 errors fixed (see below) |
| `Better_Cloth/Better_Cloth.csproj` | Not yet started |
| `Better_Scene/Better_Scene.csproj` | Not yet started |
| `Better_Story/Better_Story.csproj` | Not yet started — contains pycs bridge |

## Target Framework
- **net472** (Unity Mono). No MathF, no System.Runtime.CompilerServices.Unsafe (production use).
- `<LangVersion>latest</LangVersion>` in all 4 csproj files.

## Key SMA 23.1 Breaking Changes

### Hard obsolete errors (CS0619 — `#pragma warning disable` DOES NOT help)
These types were marked `[Obsolete("msg", true)]` (error:true):
- `EntrevistaConFemaleDePoolDelDia` → use `[HarmonyTargetMethod]` or `Traverse`
- `BaseFemalePoseLoader` → use `AccessTools.TypeByName()` + `EventInfo`/`PropertyInfo` reflection
- `MemoriaDeCharacterTemporal` → stub; feature is no-op
- `PiscinasDeEventosDeEntrevista` → stub; game now has single pool natively
- `HorariosNormalesDeEntrevistas` → stub; used in AutoTraining scheduling
- `AutoRatings` → stub
- `ISujetoCalificable.calificado` → stub; return false
- `ObtenerSujetosAgrupadoNoCalificadoCount()` → stub; return 0

### Renamed APIs
- `IPene.tip` → `IPene.partePunta`   (tip Transform is now partePunta)
- `IPene.root` → `IPene.parteBase`   (root Transform is now parteBase)
- `ControlladorDeGestosFaciales` → `ControlladorDeGestosFacialesEmocionales`
- `TipoDeExpresion` → could not resolve namespace; FaceOverride block commented out

### Removed APIs
- `BoneStretchedChain.penetracionLocalActual` → return 0f
- `BoneStretchedChain.maximaProfundidadVirtualAlcanzada` → return false
- `HandPickController.w` → removed; condition stubbed to just check `.enabled`

### Type changes
- `PortraitsModelBase` is now `PortraitsModelBase<T>` (generic) → gallery picker methods stubbed

### Decompiler artifacts to watch for
- `Unsafe.As<T,T>(ref x)` where T==T → no-op, replace with `x`
- `Unsafe.AsPointer(ref struct.field)` → pointer struct mutation, comment out
- `((T)(ref v)).X` (ref cast) → decompiler artifact, replace with `v.X`

### Assembly locations (SMA 23.1)
```
G:\Games\AAA\Some_Modeling_Agency_0.23.1_f1\Some Modeling Agency_Data\Managed\
```
- `Assembly-D_C_Dependientes_ReSc.dll` — PelvisMovementController, HandPickController, TipoDeExpresion
- `Assembly-D_D_Characters_ReSc.dll` — Characters
- `TValle.BeachGirl.dll` — IPene, IPeneSimple, Penetrador
- `TValle.IU.dll` — ModalWindow, PortraitsModelBase<T>, PosePortraitsModel, OutfitPortraitsModel

## Reflection Patterns Used

### HarmonyTargetMethod for obsolete patch target
```csharp
[HarmonyPatch]
private static class MyPatch {
    [HarmonyTargetMethod]
    static MethodBase TargetMethod() {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .FirstOrDefault(t => t.Name == "ObsoleteTypeName");
        return type?.GetMethod("MethodName",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }
    [HarmonyPrefix]
    static void Prefix(object __instance) {
        Traverse t = Traverse.Create(__instance);
        var field = t.Field<FieldType>("fieldName").Value;
    }
}
```

### AccessTools.TypeByName for obsolete type events/properties
```csharp
private static readonly Type _type = AccessTools.TypeByName("Full.Namespace.ClassName");
private static readonly EventInfo _event = _type?.GetEvent("eventName");
private static readonly PropertyInfo _prop = _type?.GetProperty("propName");
// Usage:
_event?.AddEventHandler(instance, handler);
var val = (_prop?.GetValue(instance))?.ToString() ?? "";
```

## SSH / Git
- Deploy key: `F:\Games\AAA\better_experience_working\.ssh\deploy_key`
- Remote: git@github.com:...
- **Never paste the private key in chat**
- Permission granted to commit and push when needed

## Build Workflow
1. Run `build.bat` — reports errors per project
2. Fix errors (see patterns above)
3. Run `commit-and-push.bat` after clean build
