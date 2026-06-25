using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets._ReusableScripts.CuchiCuchi.AI;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores.AI.Clases;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores.Clases;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores.Clases.Abstracts;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores.Mapas.Genetica.Handlers;
using Assets._ReusableScripts.Genetica.NPCs;
using Assets.Base.Genetica.Runtime.NPCs.Abstracts.Interfaces;
using BetterExperience.Utils;
using UnityEngine;

namespace BetterExperience.Wrappers.Pools;

public class GuestPool
{
	private List<GuestInstance> guests = new List<GuestInstance>();

	public PiscinaDeNpcsManager Instance { get; private set; }

	public GeneFactoryInfo PersonalityGeneInfo { get; private set; }

	public GeneFactoryInfo AppearanceGeneInfo { get; private set; }

	public GeneFactoryInfo GeneFactory { get; private set; }

	public int Count => Instance.count;

	public int NonClassifiedCount => Instance.ObtenerSujetosAgrupadoNoCalificadoCount();

	public List<GuestInstance> Guests => guests;

	public Dictionary<GeneId, GeneInfoEx> Eve
	{
		get
		{
			Dictionary<GeneId, GeneInfoEx> dictionary = new Dictionary<GeneId, GeneInfoEx>();
			ProductorDeSujetosDeAparienciaFisicaFemenina productorDeSujetosDeAparienciaFisicaFemenina = Instance.aparienciaFisica.productor as ProductorDeSujetosDeAparienciaFisicaFemenina;
			if (productorDeSujetosDeAparienciaFisicaFemenina != null)
			{
				productorDeSujetosDeAparienciaFisicaFemenina.@default.PrepareAlteradoresDicc();
				ReadGenesInto(productorDeSujetosDeAparienciaFisicaFemenina.@default.preparedAlteradoresDicc, dictionary);
			}
			ProductorDeSujetosDePersonalidadFemenina productorDeSujetosDePersonalidadFemenina = Instance.personalidad.productor as ProductorDeSujetosDePersonalidadFemenina;
			if (productorDeSujetosDePersonalidadFemenina != null)
			{
				productorDeSujetosDePersonalidadFemenina.@default.PrepareAlteradoresDicc();
				ReadGenesInto(productorDeSujetosDePersonalidadFemenina.@default.preparedAlteradoresDicc, dictionary);
				foreach (GeneInfoEx value in dictionary.Values)
				{
					if (!value.Id.Item1.Contains("_Grupo_") && value.Id.Item1.Contains("_TraitHumano_"))
					{
						int key = Mathf.RoundToInt(value.Value * 100f);
						HumanTraitScore humanTraitScore = (HumanTraitScore)AlteradorDeEnumValueBasePara100Max<HumanTraitScore>.alteradorIndexToEnumValue[key];
						value.Value = AlteradorDeEnumValueConZero<HumanTraitScore>.EnumToMod(humanTraitScore, AlteradorDeEnumValueConZero<HumanTraitScore>.count);
						value.Value = 0.5f;
					}
				}
			}
			return dictionary;
		}
	}

	static GuestPool()
	{
		RuntimeHelpers.RunClassConstructor(typeof(AlteradorDeGrupoQueCompartenValores).TypeHandle);
		RuntimeHelpers.RunClassConstructor(typeof(AlteradorDeTraitDePersonalidad).TypeHandle);
	}

	public GuestPool(PiscinaDeNpcsManager instance)
	{
		Instance = instance;
		InitializeProxies();
	}

	private void InitializeProxies()
	{
		Dictionary<ISujetoIdentificableNpc, GuestInstance> dictionary = new Dictionary<ISujetoIdentificableNpc, GuestInstance>();
		foreach (GuestInstance guest in guests)
		{
			dictionary.Add(guest.Instance, guest);
		}
		guests.Clear();
		foreach (ISujetoIdentificableNpc nPC in Instance.NPCs)
		{
			if (!dictionary.TryGetValue(nPC, out var value))
			{
				value = new GuestInstance(nPC, this);
			}
			guests.Add(value);
		}
		IProductorDeConjuntos component = Instance.GetComponent<IProductorDeConjuntos>();
		PersonalityGeneInfo = CreateGeneFactoryInfo(component.conjuntosParaPersonalidad);
		AppearanceGeneInfo = CreateGeneFactoryInfo(component.conjuntosParaApariencia);
		InitializeSensitivityMap();
		GeneFactory = new GeneFactoryInfo();
		PersonalityGeneInfo.Groups.ForEach(GeneFactory.Groups.Add);
		AppearanceGeneInfo.Groups.ForEach(GeneFactory.Groups.Add);
		PersonalityGeneInfo.GeneToGroup.ForEach(GeneFactory.GeneToGroup.Add);
		AppearanceGeneInfo.GeneToGroup.ForEach(GeneFactory.GeneToGroup.Add);
		PersonalityGeneInfo.GroupToGenes.ForEach(GeneFactory.GroupToGenes.Add);
		AppearanceGeneInfo.GroupToGenes.ForEach(GeneFactory.GroupToGenes.Add);
		PersonalityGeneInfo.SensitivityMap.ForEach(GeneFactory.SensitivityMap.Add);
		AppearanceGeneInfo.SensitivityMap.ForEach(GeneFactory.SensitivityMap.Add);
		GeneFactory.Eve = Eve;
	}

	private void InitializeSensitivityMap()
	{
		IReadOnlyList<ModificadoresDeAlterador> list = ((ProductorDeSujetosDeAparienciaFisicaFemenina)Instance.aparienciaFisica.productor).@default.ObtenerAlteradorModificadores();
		SaveSensitivity(AppearanceGeneInfo, list);
		IReadOnlyList<ModificadoresDeAlterador> list2 = ((ProductorDeSujetosDePersonalidadFemenina)Instance.personalidad.productor).@default.ObtenerAlteradorModificadores();
		SaveSensitivity(PersonalityGeneInfo, list2);
	}

	private void SaveSensitivity(GeneFactoryInfo factory, IReadOnlyList<ModificadoresDeAlterador> list)
	{
		foreach (ModificadoresDeAlterador item in list)
		{
			GeneSensitivity value = GeneSensitivity.Normal;
			if (item.sensible)
			{
				value = GeneSensitivity.Soft;
			}
			else if (item.volatil)
			{
				value = GeneSensitivity.Hard;
			}
			for (int i = 0; i < item.modificadores.Length; i++)
			{
				factory.SensitivityMap[new GeneId(item.alteradorName, i)] = value;
			}
		}
	}

	private GeneFactoryInfo CreateGeneFactoryInfo(IReadOnlyDictionary<string, IReadOnlyCollection<object>> groups)
	{
		GeneFactoryInfo geneFactoryInfo = new GeneFactoryInfo();
		foreach (string key in groups.Keys)
		{
			geneFactoryInfo.Groups.Add(key);
			IReadOnlyCollection<object> readOnlyCollection = groups[key];
			List<string> list = new List<string>();
			geneFactoryInfo.GroupToGenes[key] = list;
			foreach (string item in readOnlyCollection)
			{
				geneFactoryInfo.GeneToGroup[item] = key;
				list.Add(item);
			}
		}
		return geneFactoryInfo;
	}

	internal void Invalidate()
	{
		InitializeProxies();
	}

	private void ReadGenesInto(IReadOnlyDictionary<string, ModificadoresDeAlterador> preparedAlteradoresDicc, Dictionary<GeneId, GeneInfoEx> result)
	{
		foreach (KeyValuePair<string, ModificadoresDeAlterador> item in preparedAlteradoresDicc)
		{
			string key = item.Key;
			ModificadoresDeAlterador value = item.Value;
			for (int i = 0; i < value.modificadores.Length; i++)
			{
				GeneId geneId = new GeneId(key, i);
				result[geneId] = new GeneInfoEx
				{
					Id = geneId,
					Value = value.modificadores[i],
					Group = ((IDictionary<string, string>)GeneFactory.GeneToGroup).GetValueOrDefault(key, (string)null)
				};
			}
		}
	}
}
