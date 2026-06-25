using System.Collections.Generic;
using Assets;
using Assets._ReusableScripts.CuchiCuchi.Dialogos;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using HarmonyLib;

namespace BetterExperience.Features.Lexicon;

internal class BodyPartsProcessor : BaseDialogProcessor<Dictionary<string, ADialogVariant>>
{
	public BodyPartsProcessor(PersistenceService persistence)
		: base("body_parts", persistence)
	{
	}

	protected override void TraverseTree()
	{
		foreach (KeyValuePair<int, ListaDeNombresDeParteDelCuerpo> item in Traverse.Create((object)Singleton<NombresLocalizadosDePartes>.instance).Field("m_US_Cuerpo").GetValue<DiccionaryEnum<ParteDelCuerpoHumano, ListaDeNombresDeParteDelCuerpo>>())
		{
			string key = "@" + (ParteDelCuerpoHumano)item.Key/*cast due to constrained. prefix*/;
			ADialogVariant valueOrAdd = base.Data.GetValueOrAdd(key, () => new ADialogVariant());
			Visit(valueOrAdd, item.Value);
		}
	}
}
