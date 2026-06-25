using System;
using System.Collections;
using System.Collections.Generic;
using BetterExperience.GameScopes;
using IronPython.Hosting;
using IronPython.Modules;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using UnityEngine;

namespace BetterExperience.PyStory.Scripting;

internal class ScriptingContext : IDisposable
{
	private delegate object ImportDelegate(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple, int level);

	private Logger logger = Logger.Create<ScriptingContext>();

	private DispatcherService dispatcher;

	private ScriptEngine python;

	private PythonScriptRepository scripts;

	private Dictionary<string, Scope> modules = new Dictionary<string, Scope>();

	private ScopeSupport scriptingScope = new ScopeSupport();

	public List<Func<PyStrand, IEnumerator>> StrandEpilogueGens = new List<Func<PyStrand, IEnumerator>>();

	public ScriptingStage Stage { get; }

	public Observable<string> OnErrorReport { get; } = new Observable<string>();

	public ScopeSupport ScriptingScope => scriptingScope;

	public ScriptEngine Engine => python;

	public PyStrand MainStrand { get; private set; }

	public ScriptingContext(DispatcherService dispatcher, PythonScriptRepository scripts, ScriptingStage stage)
	{
		Stage = stage;
		this.dispatcher = dispatcher;
		this.scripts = scripts;
		python = Python.CreateEngine();
		PythonList metapath = Python.GetSysModule(python).GetVariable<PythonList>("meta_path");
		metapath.append((object)new RepositoryMetaImporter(scripts));
		scriptingScope.OnException += ScriptingScope_OnException;
		scriptingScope.Start();
		MainStrand = new PyStrand(scriptingScope);
	}

	private void ScriptingScope_OnException(ScopeSupport.ScopeExceptionEvent obj)
	{
		obj.Handled = true;
		PyStrandFrame ct = PyStrandFrame.CurrentFrame;
		string pythonContext = "None";
		if (ct != null)
		{
			pythonContext = ct.DescribeContext();
		}
		logger.Error(obj.Exception, "PyScript exception. Python context: {0}", pythonContext);
		string report = "PyStrand context: " + pythonContext + "\n\n";
		string pyex = python.GetService<ExceptionOperations>(Array.Empty<object>()).FormatException(obj.Exception);
		logger.Error("Python exception:\n{0}", pyex);
		report += "Python stacktrace:\n\n";
		report += pyex;
		report += "\n\n";
		Exception exception = obj.Exception;
		SyntaxErrorException see = (SyntaxErrorException)(object)((exception is SyntaxErrorException) ? exception : null);
		if (see != null)
		{
			report += $"Syntax error: {((Exception)(object)see).Message} at {see.SourcePath} {see.Line}:{see.Column}";
		}
		else
		{
			report += "Managed stacktrace:\n\n";
			report += obj.Exception.ToString().Replace("--->", "\n--->");
		}
		OnErrorReport.Invoke(report);
	}

	public void Dispose()
	{
		scriptingScope.Dispose();
		python.Runtime.Shutdown();
	}

	public Coroutine StartPyEngineCoroutine(IEnumerator coro, PyStrand strand)
	{
		if (strand.Frames.Count > 0)
		{
			throw new ArgumentException("Cannot wrap active strand in coroutine");
		}
		if (logger.EnableDebug)
		{
			logger.Debug("Starting coroutine for {0}", coro.GetType());
		}
		strand.Frames.Push(new PyStrandFrame(coro, strand));
		return dispatcher.StartCoroutine(PyScriptCoroInternal(strand), scriptingScope);
	}

	private IEnumerator PyScriptCoroInternal(PyStrand strand)
	{
		if (logger.EnableDebug)
		{
			logger.Debug("Starting coroutine {0}", strand.Frames.Peek().DescribeContext());
		}
		while (strand.Scope.Started)
		{
			if (strand.Frames.Count > 0)
			{
				PyStrandFrame frame = strand.Frames.Peek();
				if (!strand.IsFailed && frame.StepForward())
				{
					if (logger.EnableDebug)
					{
						logger.Debug("Step {0} {1}", frame.DescribeContext(), frame.Flow.Current);
					}
					object current = frame.Flow.Current;
					if (current is IEnumerator ie2)
					{
						strand.Frames.Push(new PyStrandFrame(ie2, strand));
						continue;
					}
					current = frame.Flow.Current;
					if (current is IEnumerable ie3)
					{
						strand.Frames.Push(new PyStrandFrame(ie3.GetEnumerator(), strand));
					}
					else
					{
						yield return frame.Flow.Current;
					}
					continue;
				}
				strand.Frames.Pop();
				if (strand.Frames.Count != 0)
				{
					continue;
				}
				if (logger.EnableDebug)
				{
					logger.Debug("Coroutine {0} complete", frame.DescribeContext());
				}
				if (!strand.Scope.Started)
				{
					logger.Info("Frame {0} interrupted due to disposed scope", frame.DescribeContext());
				}
				else if (strand.IsFailed)
				{
					logger.Info("Frame {0} interrupted due to strand failure", frame.DescribeContext());
				}
				foreach (Func<PyStrand, IEnumerator> e in StrandEpilogueGens)
				{
					IEnumerator it = e(strand);
					while (it.MoveNext())
					{
						yield return it.Current;
					}
				}
			}
			else
			{
				if (strand.Queue.Count <= 0)
				{
					break;
				}
				PyStrandFrame next = strand.Queue[0];
				strand.Queue.RemoveAt(0);
				strand.Frames.Push(next);
				if (logger.EnableDebug)
				{
					logger.Debug("Starting next queued frame");
				}
			}
		}
	}

	internal void SpawnNext(IEnumerator e, PyStrand targetStrand)
	{
		targetStrand = CheckedStrand(targetStrand);
		if (targetStrand.Frames.Count > 0)
		{
			targetStrand.SubmitFirst(e);
		}
		else
		{
			StartPyEngineCoroutine(e, targetStrand);
		}
	}

	internal void SpawnLast(IEnumerator e, PyStrand targetStrand)
	{
		targetStrand = CheckedStrand(targetStrand);
		if (targetStrand.Frames.Count > 0)
		{
			targetStrand.SubmitLast(e);
		}
		else
		{
			StartPyEngineCoroutine(e, targetStrand);
		}
	}

	private PyStrand CheckedStrand(PyStrand target)
	{
		if (target == null)
		{
			target = MainStrand;
		}
		return target;
	}

	protected object DoDatabaseImport(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple, int level)
	{
		logger.Error("Import! {0}", moduleName);
		string filename = moduleName + ".py";
		string rawScript = scripts.GetScript(filename);
		if (rawScript != null)
		{
			if (modules.TryGetValue(moduleName, out var cached))
			{
				return cached;
			}
			ScriptSource source = python.CreateScriptSourceFromString(rawScript, filename);
			ScriptScope scope = python.CreateScope();
			source.Execute(scope);
			Scope modscope = HostingHelpers.GetScope(scope);
			scope.SetVariable(moduleName, (object)modscope);
			modules[moduleName] = modscope;
			return modscope;
		}
		return Builtin.__import__(context, moduleName, (object)null, (object)null, (object)null, 0);
	}
}
