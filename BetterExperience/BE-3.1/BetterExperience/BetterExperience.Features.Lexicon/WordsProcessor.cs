using System;
using System.Collections.Generic;
using Assets;
using Assets._ReusableScripts.CuchiCuchi.AI;
using Assets._ReusableScripts.CuchiCuchi.Dialogos;
using Assets._ReusableScripts.CuchiCuchi.Dialogos.Genericos.Globales;
using Assets._ReusableScripts.CuchiCuchi.Dialogos.Objetos;
using Assets.SingletonesAndSystemasGlobales.AbstractLayer.Globales;
using BetterExperience.GameScopes;
using HarmonyLib;

namespace BetterExperience.Features.Lexicon;

internal class WordsProcessor : BaseDialogProcessor<DeepDict<string, string, string, ADialogVariant>>
{
	public WordsProcessor(PersistenceService persistence)
		: base("words", persistence)
	{
	}

	protected override void TraverseTree()
	{
		PalabrasDeDialogosGenericos.DialogosDeLocal uS = Singleton<PalabrasDeDialogosGenericos>.instance.US;
		Walk(uS.acciones3PersonaDeTipoDeRespuesta, SpecificHandler("acciones3PersonaDeTipoDeRespuesta"));
		Walk(uS.acciones3PersonaPluralDeTipoDeRespuesta, SpecificHandler("acciones3PersonaPluralDeTipoDeRespuesta"));
		Walk(uS.accionesConjugadasDeTipoDeRespuesta, SpecificHandler("accionesConjugadasDeTipoDeRespuesta"));
		Walk(uS.accionesDeTipoDeRespuesta, SpecificHandler("accionesDeTipoDeRespuesta"));
		Walk(uS.accionesPasadoDeTipoDeRespuesta, SpecificHandler("accionesPasadoDeTipoDeRespuesta"));
		Walk(uS.accionesPluralesDeTipoDeRespuesta, SpecificHandler("accionesPluralesDeTipoDeRespuesta"));
		Walk(uS.accionesPresentesDeTipoDeRespuesta, SpecificHandler("accionesPresentesDeTipoDeRespuesta"));
		Walk(uS.emocionesPersonalesDeTipoDeRespuesta, SpecificHandler("emocionesPersonalesDeTipoDeRespuesta"));
		Walk(uS.emocionesPersonalesPluralesDeTipoDeRespuesta, SpecificHandler("emocionesPersonalesPluralesDeTipoDeRespuesta"));
		Walk(uS.emocionesPresentesDeTipoDeRespuesta, SpecificHandler("emocionesPresentesDeTipoDeRespuesta"));
		Walk(uS.emocionesTerceraPersonaDeTipoDeRespuesta, SpecificHandler("emocionesTerceraPersonaDeTipoDeRespuesta"));
		Walk(uS.emocionesTerceraPersonaPluralesDeTipoDeRespuesta, SpecificHandler("emocionesTerceraPersonaPluralesDeTipoDeRespuesta"));
		Walk(uS.intensidadAdjetivo, SpecificHandler("intensidadAdjetivo"));
		Walk(uS.intensidadAdverbio, SpecificHandler("intensidadAdverbio"));
		Walk(uS.peticionesPersonalesNegativasDeTipoDeRespuesta, SpecificHandler("peticionesPersonalesNegativasDeTipoDeRespuesta"));
		Walk(uS.peticionesPersonalesPositivasDeTipoDeRespuesta, SpecificHandler("peticionesPersonalesPositivasDeTipoDeRespuesta"));
		Walk(uS.peticionesPresentesNegativasDeTipoDeRespuesta, SpecificHandler("peticionesPresentesNegativasDeTipoDeRespuesta"));
		Walk(uS.peticionesPresentesPositivasDeTipoDeRespuesta, SpecificHandler("peticionesPresentesPositivasDeTipoDeRespuesta"));
		Walk(uS.peticionesSerConjugadoDeTipoDeRespuesta, SpecificHandler("peticionesSerConjugadoDeTipoDeRespuesta"));
		Walk(uS.sentimientoPerfectoDeTipoDeRespuesta, SpecificHandler("sentimientoPerfectoDeTipoDeRespuesta"));
		Walk(uS.tipoDeRespuestaDeTipoDePalabras, SpecificHandler("tipoDeRespuestaDeTipoDePalabras"));
	}

	private Action<string, string, ListaDeDialogosBase> SpecificHandler(string v)
	{
		return delegate(string a, string b, ListaDeDialogosBase c)
		{
			a = Translator.Instance.Encode(a);
			b = Translator.Instance.Encode(b);
			Traverse.Create((object)c).Field("text").GetValue<string>();
			ADialogVariant orAdd = base.Data.GetOrAdd("@" + v, "@" + a, "@" + b, () => new ADialogVariant());
			Visit(orAdd, c);
		};
	}

	private void Walk(DiccionaryEnum<Personalidad.TipoDeRespuestaDeDialogoDeHeroina, ListaDeDialogos> x, Action<string, string, ListaDeDialogosBase> handler)
	{
		foreach (KeyValuePair<int, ListaDeDialogos> item in x)
		{
			string arg = ((Personalidad.TipoDeRespuestaDeDialogoDeHeroina)item.Key/*cast due to constrained. prefix*/).ToString();
			handler("_", arg, item.Value);
		}
	}

	private void Walk<K>(IDictionary<K, DiccionaryEnum<Personalidad.TipoDeRespuestaDeDialogoDeHeroina, ListaDeDialogos>> input, Action<string, string, ListaDeDialogosBase> mapper)
	{
		foreach (KeyValuePair<K, DiccionaryEnum<Personalidad.TipoDeRespuestaDeDialogoDeHeroina, ListaDeDialogos>> item in input)
		{
			string k = Encode(item.Key);
			Walk(item.Value, delegate(string a, string b, ListaDeDialogosBase c)
			{
				mapper(k, b, c);
			});
		}
	}

	private string Encode<K>(K key)
	{
		if (key is (int, int, int, int) tuple)
		{
			return tuple.Item1 + "_" + tuple.Item2 + "_" + tuple.Item3 + "_" + tuple.Item4;
		}
		if (key is ReaccionHumana reaccionHumana)
		{
			return reaccionHumana.ToString();
		}
		if (key is int num)
		{
			return num.ToString();
		}
		throw new ArgumentException("Unsupported type " + key.GetType());
	}
}
