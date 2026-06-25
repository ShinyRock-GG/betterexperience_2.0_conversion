using Assets;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.DialogueSystem.GoTo.UI;
using BetterExperience.GameScopes;
using HarmonyLib;

namespace BetterExperience.CustomScene;

public static class BetterSceneHarmonyPatches
{
	public static Observable<HashSetList<string>> OnAfterGoToListPopulated { get; } = new Observable<HashSetList<string>>();

	[HarmonyPatch(typeof(OpcionesDeTHSDonaDeGoToDisponibles), "LoadKeys")]
	[HarmonyPostfix]
	public static void After_OpcionesDeTHSDonaDeGoToDisponibles_LoadKeys(HashSetList<string> resultado)
	{
		OnAfterGoToListPopulated.Invoke(resultado);
	}
}
