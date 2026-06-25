using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BetterExperience.GameScopes;
using BetterExperience.PyStory.Scripting;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

namespace BetterExperience.PyStory;

public class SessionScriptFeature : SessionService
{
	private class ScriptHost : StrandService, IDisposable
	{
		private GameSession session;

		private ScriptEngine python;

		private PythonScriptRepository scripts;

		public ScriptPluginFeature.Plugin plugin { get; }

		private ScopeSupport scope => session.Scope;

		public ScriptHost(ScriptPluginFeature.Plugin plugin, DispatcherService dispatcher, GameSession session)
			: base(dispatcher)
		{
			logger.Prefix = "[pymod-" + plugin.package.Id + "]";
			this.session = session;
			this.plugin = plugin;
			scripts = new PythonScriptRepository();
			scripts.Init(plugin.virtIO);
			Start();
		}

		private void Start()
		{
			base.scriptingScope.Start();
			python = Python.CreateEngine();
			PythonList metapath = Python.GetSysModule(python).GetVariable<PythonList>("meta_path");
			metapath.append((object)new RepositoryMetaImporter(scripts));
			ExposeRuntime();
			ImportAllModules();
		}

		public void Dispose()
		{
			base.scriptingScope.Dispose();
			if (python != null)
			{
				python.Runtime.Shutdown();
				python = null;
			}
		}

		private void ImportAllModules()
		{
			logger.Info("Importing all python modules...");
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			int imported = 0;
			foreach (string scriptfile in scripts.AutoimportScripts)
			{
				string name = scriptfile;
				if (name.ToLowerInvariant().EndsWith(".py"))
				{
					name = name.Substring(0, name.Length - 3);
				}
				if (name.ToLowerInvariant().EndsWith("__init__"))
				{
					name = name.Substring(0, name.Length - 8);
				}
				if (name.EndsWith("\\"))
				{
					name = name.Substring(0, name.Length - 1);
				}
				string module = name.Replace("\\", ".");
				logger.Debug("Module {0} as {1}", name, module);
				try
				{
					python.Execute<object>("import " + module);
					imported++;
				}
				catch (Exception ex)
				{
					logger.Error("Module import failed {0}: {1}", module, ex.Message);
				}
			}
			stopwatch.Stop();
			logger.Info("Loaded {0} modules in {1}ms.", imported, stopwatch.ElapsedMilliseconds);
		}

		private void ExposeRuntime()
		{
			ScriptScope builtin = Python.GetBuiltinModule(python);
			Pyrt pyrt = new Pyrt(logger, scope, this, session);
			builtin.SetVariable("pyrt", (object)pyrt);
		}
	}

	public class Pyrt
	{
		private StrandService scriptingContext;

		public Logger logger { get; }

		public ScopeSupport scope { get; }

		public ScopeSupport scriptScope => scriptingContext.scriptingScope;

		public GameSession session { get; private set; }

		public GameSession Session => session;

		public ScopeSupport Scope => scope;

		public ScopeSupport ScriptScope => scriptScope;

		public Pyrt(Logger logger, ScopeSupport scope, StrandService strandService, GameSession session)
		{
			this.logger = logger;
			this.scope = scope;
			scriptingContext = strandService;
			this.session = session;
		}

		public PyStrand get_main_strand()
		{
			return scriptingContext.MainStrand;
		}

		public bool can_invoke_immediate(PyStrand strand)
		{
			return strand.Frames.Count == 0;
		}

		public bool invoke_next(IEnumerable e, PyStrand strand)
		{
			scriptingContext.SpawnNext(e.GetEnumerator(), strand);
			return true;
		}

		public bool invoke_last(IEnumerable e, PyStrand strand)
		{
			scriptingContext.SpawnLast(e.GetEnumerator(), strand);
			return true;
		}

		public PyStrand new_strand()
		{
			return new PyStrand(scriptingContext.scriptingScope);
		}

		public void call_with_exception_trap(Func<object> x)
		{
			try
			{
				x();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Crash");
			}
		}
	}

	private ScriptPluginFeature feature;

	private List<ScriptHost> hosts = new List<ScriptHost>();

	public SessionScriptFeature(ScriptPluginFeature feature)
	{
		this.feature = feature;
	}

	public override void OnStart()
	{
		base.OnStart();
		DispatcherService dispatcher = Lookup<DispatcherService>();
		foreach (ScriptPluginFeature.Plugin p in feature.plugins)
		{
			StartPlugin(dispatcher, p);
		}
	}

	private void StartPlugin(DispatcherService dispatcher, ScriptPluginFeature.Plugin p)
	{
		logger.Info("Starting {0}", p.package.Id);
		hosts.Add(new ScriptHost(p, dispatcher, base.Session));
		ScriptPluginFeature.Plugin plugin = p;
		plugin.restart.Add(delegate
		{
			List<ScriptHost> list = hosts.Where((ScriptHost x) => x.plugin == plugin).ToList();
			if (list.Count != 0)
			{
				int index = hosts.IndexOf(list[0]);
				hosts[index].Dispose();
				hosts[index] = new ScriptHost(plugin, dispatcher, base.Session);
			}
		}, base.Scope);
	}
}
