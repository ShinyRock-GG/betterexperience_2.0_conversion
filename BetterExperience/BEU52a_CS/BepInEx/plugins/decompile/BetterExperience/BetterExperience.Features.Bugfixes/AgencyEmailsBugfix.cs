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
			IJsonMemoryNode __m_memoria = Traverse.Create((object)__instance).Field("m_memoria").GetValue<IJsonMemoryNode>();
			logger.Error("onemailsloaded");
			List<IJsonMemoryNode> toremove = new List<IJsonMemoryNode>();
			foreach (IMemoryNode<string, string> c in __m_memoria.children)
			{
				IJsonMemoryNode node = __m_memoria.FindChild<IJsonMemoryNode>(c.nodeID);
				if (node != null)
				{
					int type = node.FindDataInt("tipoDeEmail", -1);
					if (type == -1)
					{
						logger.Error("Broken email found {0}", node.Save());
						toremove.Add(node);
					}
				}
			}
			foreach (IJsonMemoryNode c2 in toremove)
			{
				__m_memoria.RemoverChild(c2.nodeID);
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
