using System;
using System.Collections.Generic;
using System.IO;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

namespace BetterExperience.PyStory.Scripting;

[PythonType]
public class RepositoryMetaImporter
{
	internal sealed class MemoryStreamContentProvider : TextContentProvider
	{
		private readonly PythonContext _context;

		private readonly byte[] _data;

		private readonly int _index;

		private readonly int _count;

		private readonly string _path;

		internal MemoryStreamContentProvider(PythonContext context, byte[] data, string path)
			: this(context, data, 0, (data != null) ? data.Length : 0, path)
		{
		}

		internal MemoryStreamContentProvider(PythonContext context, byte[] data, int index, int count, string path)
		{
			_context = context;
			_data = data;
			_index = index;
			_count = count;
			_path = path;
		}

		public override SourceCodeReader GetReader()
		{
			return ((LanguageContext)_context).GetSourceReader((Stream)new MemoryStream(_data, _index, _count, writable: false), ((LanguageContext)_context).DefaultEncoding, _path);
		}
	}

	private Logger logger = Logger.Create<RepositoryMetaImporter>();

	private PythonScriptRepository repository;

	private Dictionary<object, object> moduleCache = new Dictionary<object, object>();

	public RepositoryMetaImporter(PythonScriptRepository repository)
	{
		this.repository = repository;
		logger.EnableInfo = false;
	}

	public object find_module(CodeContext context, string fullname, params object[] args)
	{
		logger.Info("Find module {0}", fullname);
		if (GetModuleCode(context, fullname, out var _, out var _) != null)
		{
			return this;
		}
		return null;
	}

	public object load_module(CodeContext context, string fullname)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		if (moduleCache.TryGetValue(fullname, out var result))
		{
			return result;
		}
		bool isPackage;
		string text;
		byte[] moduleCode = GetModuleCode(context, fullname, out isPackage, out text);
		if (moduleCode == null)
		{
			return null;
		}
		PythonContext languageContext = context.LanguageContext;
		ScriptCode scriptCode = default(ScriptCode);
		PythonModule pythonModule = languageContext.CompileModule(text, fullname, new SourceUnit((LanguageContext)(object)languageContext, (TextContentProvider)(object)new MemoryStreamContentProvider(languageContext, moduleCode, text), text, (SourceCodeKind)4), (ModuleOptions)0, ref scriptCode);
		PythonDictionary _dict__ = pythonModule.Get__dict__();
		_dict__.Add((object)"__name__", (object)fullname);
		_dict__.Add((object)"__loader__", (object)this);
		_dict__.Add((object)"__package__", (object)null);
		_dict__.Add((object)"__file__", (object)"<resource>");
		if (isPackage)
		{
			PythonList value = new PythonList();
			_dict__.Add((object)"__path__", (object)value);
		}
		moduleCache.Add(fullname, pythonModule);
		try
		{
			scriptCode.Run(pythonModule.Scope);
		}
		catch (Exception)
		{
			moduleCache.Remove(fullname);
			throw;
		}
		return pythonModule;
	}

	private byte[] GetModuleCode(CodeContext context, string fullname, out bool isPackage, out string text)
	{
		string filepath = fullname.Replace('.', Path.DirectorySeparatorChar);
		string packagepath = Path.Combine(filepath, "__init__.py");
		string modulepath = filepath + ".py";
		logger.Info("Search path for {0}: {1} {2}", fullname, packagepath, modulepath);
		string src = repository.GetScript(packagepath);
		if (src != null)
		{
			isPackage = true;
			text = packagepath;
		}
		else
		{
			isPackage = false;
			text = modulepath;
			src = repository.GetScript(modulepath);
		}
		if (src != null)
		{
			return ((LanguageContext)context.LanguageContext).DefaultEncoding.GetBytes(src);
		}
		return null;
	}
}
