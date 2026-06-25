using System.Collections.Generic;
using System.Linq;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores.Clases;
using UnityEngine;

namespace BetterExperience.Wrappers.Characters;

public class ModifierManager
{
	private AlteradoresDeAparienciaFemenina meshModifiers;

	private AlteradoresDePersonalidadFemenina scriptModifiers;

	private Dictionary<string, AlteratorModifier> alteradors = new Dictionary<string, AlteratorModifier>();

	public IReadOnlyDictionary<string, AlteratorModifier> Modifiers => alteradors;

	public ModifierManager(GameObject owner)
	{
		meshModifiers = owner.GetComponentInChildren<AlteradoresDeAparienciaFemenina>();
		scriptModifiers = owner.GetComponentInChildren<AlteradoresDePersonalidadFemenina>();
		(from x in meshModifiers.mapaDeValores.ObtenerAlteradorModificadores()
			select new AlteratorModifier(x, _InvalidateMesh, meshModifiers.Obtener(x.alteradorName))).ForEach(delegate(AlteratorModifier x)
		{
			alteradors.Add(x.Name, x);
		});
		(from x in scriptModifiers.mapaDeValores.ObtenerAlteradorModificadores()
			select new AlteratorModifier(x, _InvalidateScript, scriptModifiers.Obtener(x.alteradorName))).ForEach(delegate(AlteratorModifier x)
		{
			alteradors.Add(x.Name, x);
		});
	}

	private void _InvalidateMesh()
	{
		meshModifiers.flagToForceUpdateValores = true;
	}

	private void _InvalidateScript()
	{
		scriptModifiers.flagToForceUpdateValores = true;
	}
}
