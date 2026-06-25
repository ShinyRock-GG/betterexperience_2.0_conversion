using System;
using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores.Mapas.Genetica;
using Assets._ReusableScripts.Genetica;
using Assets._ReusableScripts.Genetica.NPCs;

namespace BetterExperience.Wrappers.Pools;

public class GuestInstance
{
	public ISujetoIdentificableNpc Instance { get; private set; }

	public GuestPool Pool { get; private set; }

	public string Id => Instance.NpcID.ToString();

	public bool Classified => ((ISujetoCalificable)Instance).calificado;

	public int Level => ((ISujetoNivel)Instance).nivel;

	public Dictionary<string, float> Rating
	{
		get
		{
			Dictionary<string, float> dictionary = new Dictionary<string, float>();
			foreach (KeyValuePair<string, IConjuntoDeGenes> item in Instance.aparienciaFisica.conjuntoPorNombre)
			{
				dictionary[item.Key] = item.Value.fitnes;
			}
			foreach (KeyValuePair<string, IConjuntoDeGenes> item2 in Instance.personalidad.conjuntoPorNombre)
			{
				dictionary[item2.Key] = item2.Value.fitnes;
			}
			return dictionary;
		}
	}

	public GuestInstance(ISujetoIdentificableNpc instance, GuestPool pool)
	{
		Instance = instance;
		Pool = pool;
	}

	public Dictionary<GeneId, GeneInfoEx> ExtractAppearance()
	{
		return ExtractGenes(Instance.aparienciaFisica, Pool.AppearanceGeneInfo);
	}

	public Dictionary<GeneId, GeneInfoEx> ExtractAll()
	{
		Dictionary<GeneId, GeneInfoEx> dictionary = new Dictionary<GeneId, GeneInfoEx>();
		foreach (KeyValuePair<GeneId, GeneInfoEx> item in ExtractAppearance())
		{
			dictionary.Add(item.Key, item.Value);
		}
		foreach (KeyValuePair<GeneId, GeneInfoEx> item2 in ExtractPersonality())
		{
			dictionary.Add(item2.Key, item2.Value);
		}
		return dictionary;
	}

	public Dictionary<GeneId, GeneInfoEx> ExtractPersonality()
	{
		return ExtractGenes(Instance.personalidad, Pool.PersonalityGeneInfo);
	}

	private Dictionary<GeneId, GeneInfoEx> ExtractGenes(ISujetoIdentificable geneset, GeneFactoryInfo factory)
	{
		Dictionary<GeneId, GeneInfoEx> dictionary = new Dictionary<GeneId, GeneInfoEx>();
		Dictionary<object, GeneItem> dictionary2 = new Dictionary<object, GeneItem>();
		geneset.Decodificar(dictionary2);
		foreach (KeyValuePair<object, GeneItem> item in dictionary2)
		{
			Tuple<string, int> value = (Tuple<string, int>)item.Key;
			_ = item.Key;
			GeneId geneId = new GeneId(value);
			GeneItem value2 = item.Value;
			if (!factory.GeneToGroup.ContainsKey(geneId.Item1))
			{
				new Logger().Info("No group found for gene {0}", geneId.Item1);
				continue;
			}
			GeneInfoEx geneInfoEx = new GeneInfoEx
			{
				Id = geneId,
				Value = value2.valor,
				Group = factory.GeneToGroup[geneId.Item1]
			};
			if (geneset.conjuntoPorNombre.TryGetValue(geneInfoEx.Group, out var value3))
			{
				geneInfoEx.Rating = value3.fitnes;
			}
			else
			{
				new Logger().Info("No rating for {0}", geneInfoEx.Group);
			}
			dictionary[geneId] = geneInfoEx;
		}
		return dictionary;
	}

	internal void Randmize(int appearance = 1, int personality = 1)
	{
		using (IGeneticsEditor geneticsEditor = OpenModifiersForUpdate(Instance.aparienciaFisica))
		{
			geneticsEditor.Randomize(appearance);
		}
		using IGeneticsEditor geneticsEditor2 = OpenModifiersForUpdate(Instance.personalidad);
		geneticsEditor2.Randomize(personality);
	}

	internal void Reset()
	{
		using (IGeneticsEditor geneticsEditor = OpenModifiersForUpdate(Instance.aparienciaFisica))
		{
			geneticsEditor.Reset();
		}
		using IGeneticsEditor geneticsEditor2 = OpenModifiersForUpdate(Instance.personalidad);
		geneticsEditor2.Reset();
	}

	private IGeneticsEditor OpenModifiersForUpdate(ISujetoIdentificable obj)
	{
		if (obj is SujetoIdentificableAlteradoresAparienciaFemeninos)
		{
			return new AppearanceEditor((SujetoIdentificableAlteradoresAparienciaFemeninos)obj, Pool);
		}
		if (obj is SujetoAlteradoresPersonalidadFemeninos)
		{
			return new PersonalityEditor((SujetoIdentificableAlteradoresPersonalidadFemeninos)obj, Pool);
		}
		throw new Exception("Unexected type " + obj.GetType());
	}

	internal void UpdateAppearance(List<GeneInfo> update)
	{
		WriteGenetics(Instance.aparienciaFisica, update);
	}

	internal void UpdateAll(List<GeneInfo> update)
	{
		List<GeneInfo> list = new List<GeneInfo>();
		List<GeneInfo> list2 = new List<GeneInfo>();
		foreach (GeneInfo item in update)
		{
			if (Pool.AppearanceGeneInfo.GeneToGroup.ContainsKey(item.Id.Item1))
			{
				list.Add(item);
				continue;
			}
			if (Pool.PersonalityGeneInfo.GeneToGroup.ContainsKey(item.Id.Item1))
			{
				list2.Add(item);
				continue;
			}
			throw new Exception("Gene " + item.Id.Item1 + " has no group mapping");
		}
		WriteGenetics(Instance.aparienciaFisica, list);
		WriteGenetics(Instance.personalidad, list2);
	}

	private void WriteGenetics(ISujetoIdentificable geneset, List<GeneInfo> update)
	{
		Dictionary<object, GeneItem> dictionary = new Dictionary<object, GeneItem>();
		foreach (GeneInfo item in update)
		{
			dictionary.Add(item.Id, new GeneItem
			{
				valor = item.Value
			});
		}
		geneset.Codificar(dictionary);
	}
}
