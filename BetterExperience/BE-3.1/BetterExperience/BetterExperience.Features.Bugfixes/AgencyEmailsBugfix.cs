using System.Collections.Generic;
using Assets._ReusableScripts.Memorias;
using Assets._ReusableScripts.Memorias.JsonMemorias;
using Assets.TValle.Pro.Entrevista.Runtime.Economia.Agencias.Eventos;
using BetterExperience.GameScopes;
using HarmonyLib;

namespace BetterExperience.Features.Bugfixes;

internal class AgencyEmailsBugfix : PluginService
{
	public static class HarmonyPatches
	{
		private static Logger logger = Logger.Create<AgencyEmailsBugfix>();

		[HarmonyPatch(typeof(EmailsFromAgencies), "OnMemoryLoaded")]
		[HarmonyPostfix]
		public static void OnEmailsLoaded(EmailsFromAgencies __instance)
		{
			IJsonMemoryNode value = Traverse.Create((object)__instance).Field("m_memoria").GetValue<IJsonMemoryNode>();
			logger.Error("onemailsloaded");
			List<IJsonMemoryNode> list = new List<IJsonMemoryNode>();
			foreach (IMemoryNode<string, string> child in value.children)
			{
				IJsonMemoryNode jsonMemoryNode = value.FindChild<IJsonMemoryNode>(child.nodeID);
				if (jsonMemoryNode != null && jsonMemoryNode.FindDataInt("tipoDeEmail", -1) == -1)
				{
					logger.Error("Broken email found {0}", jsonMemoryNode.Save());
					list.Add(jsonMemoryNode);
				}
			}
			foreach (IJsonMemoryNode item in list)
			{
				value.RemoverChild(item.nodeID);
			}
		}
	}

	public override void OnStart()
	{
		base.OnStart();
		Harmony.CreateAndPatchAll(typeof(HarmonyPatches), (string)null);
		logger.Error("Patched");
	}
}
