using System;
using System.Linq;
using Assets._ReusableScripts.CuchiCuchi.Controllers;
using Assets._ReusableScripts.CuchiCuchi.PhysicsAndBonesScripts;
using Assets._ReusableScripts.PhysicsScripts;
using BetterExperience.Features.Console;
using BetterExperience.GameScopes;
using BetterExperience.Utils;

namespace BetterExperience.Features;

internal class TestFeature : SessionService
{
	[ConsoleCommand("", new string[] { "test" })]
	public class GuestScoreQuery
	{
		[ConsoleCommandArg(Key = "i", Mode = ConsoleArgMode.KeyValue, Name = "123")]
		public float value { get; set; }
	}

	[ConsoleCommand("", new string[] { "test2" })]
	public class GuestScoreQuery2
	{
		[ConsoleCommandArg(Key = "i", Mode = ConsoleArgMode.Tail, Name = "123")]
		public string value { get; set; }
	}

	[ConsoleCommand("", new string[] { "test3" })]
	public class GuestScoreQuery3
	{
		[ConsoleCommandArg(Key = "i", Mode = ConsoleArgMode.KeyValue, Name = "123")]
		public float value { get; set; }
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<ConsoleService>().RegisterCommand<GuestScoreQuery>(test, base.Scope);
		Lookup<ConsoleService>().RegisterCommand<GuestScoreQuery2>(test2, base.Scope);
		Lookup<ConsoleService>().RegisterCommand<GuestScoreQuery3>(test3, base.Scope);
	}

	private string test(GuestScoreQuery query)
	{
		if (base.Session.Guest == null)
		{
			return "start interview first";
		}
		VagController componentInChildren = base.Session.Guest.Impl.GetComponentInChildren<VagController>();
		UpdateChain(componentInChildren.labiaController.l.chain, query.value);
		UpdateChain(componentInChildren.labiaController.r.chain, query.value);
		return "ok";
	}

	private string test2(GuestScoreQuery2 query)
	{
		if (base.Session.Guest == null)
		{
			return "start interview first";
		}
		float[] values = query.value.Split(new char[1] { ' ' }).Select(float.Parse).ToArray();
		VagController componentInChildren = base.Session.Guest.Impl.GetComponentInChildren<VagController>();
		UpdateChain(componentInChildren.labiaController.l.chain, values);
		UpdateChain(componentInChildren.labiaController.r.chain, values);
		return "ok";
	}

	private void UpdateChain(Linear7BoneChainBase lchain, float value)
	{
		float[] values = new float[lchain.puntosBase.Count].Fill(value);
		UpdateChain(lchain, values);
	}

	private void UpdateChain(Linear7BoneChainBase lchain, float[] values)
	{
		for (int i = 0; i < Math.Min(lchain.puntosBase.Count, values.Length); i++)
		{
			float finalTagetPoistionMod = values[i];
			JointDistancesAdmin jointDistancesAdmin = lchain.puntosBase[i].jointDistancesAdmin;
			jointDistancesAdmin.configuracion.finalTagetPoistionMod = finalTagetPoistionMod;
			jointDistancesAdmin.UpdateDistaceAndTargetMods();
		}
		lchain.estadoDePuntos.posicionesLocalesIniciales.Actializar();
	}

	private string test3(GuestScoreQuery3 query)
	{
		if (base.Session.Guest == null)
		{
			return "start interview first";
		}
		VagController componentInChildren = base.Session.Guest.Impl.GetComponentInChildren<VagController>();
		UpdateChain2(componentInChildren.labiaController.l.chain, query.value);
		UpdateChain2(componentInChildren.labiaController.r.chain, query.value);
		return "ok";
	}

	private void UpdateChain2(Linear7BoneChainBase lchain, float value)
	{
		float[] values = new float[lchain.puntosBase.Count].Fill(value);
		UpdateChain2(lchain, values);
	}

	private void UpdateChain2(Linear7BoneChainBase lchain, float[] values)
	{
		for (int i = 0; i < Math.Min(lchain.puntosBase.Count, values.Length); i++)
		{
			float value = values[i];
			JointDistancesAdmin jointDistancesAdmin = lchain.puntosBase[i].jointDistancesAdmin;
			jointDistancesAdmin.estirable.ObtenerModificadorNotNull("mymod").SetLinearLimitTo(value);
			jointDistancesAdmin.UpdateDistaceAndTargetMods();
		}
		lchain.estadoDePuntos.posicionesLocalesIniciales.Actializar();
	}
}
