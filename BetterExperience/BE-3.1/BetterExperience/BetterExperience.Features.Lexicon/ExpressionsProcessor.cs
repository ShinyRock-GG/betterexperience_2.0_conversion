using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.Dialogos;
using Assets._ReusableScripts.CuchiCuchi.Dialogos.Genericos.Objetos;
using Assets._ReusableScripts.CuchiCuchi.Dialogos.Globales.Abstracts;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using HarmonyLib;

namespace BetterExperience.Features.Lexicon;

internal class ExpressionsProcessor : BaseDialogProcessor<Dictionary<string, ADialogVariant>>
{
	public ExpressionsProcessor(PersistenceService persistence)
		: base("expressions", persistence)
	{
	}

	protected override void TraverseTree()
	{
		IDialogosDePersonalidades[] array = UnityUtils.FindAndInitializeSingletonsOfType<IDialogosDePersonalidades>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] is DialogosDePersonalidadesLogic.IDialogos dialogos))
			{
				continue;
			}
			foreach (KeyValuePair<IHolderDeCollecionDeDialogoInfo, IEnvolturaCondicionalDeHolders> item in dialogos.diccDialogoDePersonalidadAEnvolturaCondicional)
			{
				foreach (IHolderDeCollecionDeDialogoInfo grupo in item.Value.grupos)
				{
					foreach (DialogosLocalizadosGenericos item2 in Traverse.Create((object)grupo).Field("m_items").GetValue<List<DialogosLocalizadosGenericos>>())
					{
						ADialogVariant valueOrAdd = base.Data.GetValueOrAdd("@" + item2.name, () => new ADialogVariant());
						Visit(valueOrAdd, item2);
					}
					if (IsDirty())
					{
						BaseDialogProcessor<Dictionary<string, ADialogVariant>>.Reset(grupo);
					}
				}
			}
		}
	}
}
