﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using WebsiteStudio.Core.Plugins;
using WebsiteStudio.Core.Properties;
using WebsiteStudio.Interface.Plugins;

namespace WebsiteStudio.Core.Compiling.Steps {
	class BuildIndexFileStep : ICompilerStep {

		private readonly Project _Project;

		private readonly String _Path;

		private readonly String[] _Languages;

		public String Output { get; private set; }
		
		public BuildIndexFileStep(Project project) {
			_Project = project;
			_Languages = _Project.Languages.Select(l => l.Id).ToArray();

			_Path = Path.Combine(_Project.OutputPath, Project.FileIndex);
			Output = String.Format("Building index file: {0}", _Path);
		}

		public void Run() {
			BuildIndexHtmlFile();
			BuildJavaScriptLanguageRedirect();
		}

		private void BuildIndexHtmlFile() {
			HtmlDocument file = new HtmlDocument();

			StringBuilder script = new StringBuilder();
			script.Append("WebsiteStudio.LanguageRedirect(");
			script.Append(JsonConvert.SerializeObject(_Languages));

			if (_Project.StartPage != null) {
				script.Append(", \"");
				script.Append(Compiler.CreateUrl(_Project.StartPage));
				script.Append("\"");
			}

			script.Append(");");

			file.AddScript(Resources.IndexPageScript);
			file.AddScript(script.ToString());

			file.Compile(_Path);
		}

		private void BuildJavaScriptLanguageRedirect() {
			String startPage = _Project.StartPage != null ? Compiler.CreateUrl(_Project.StartPage) : null;
		}
	}
}