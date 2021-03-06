﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebsiteStudio.Core.Compiling.Steps;
using WebsiteStudio.Core.Localization;
using WebsiteStudio.Core.Pages;
using WebsiteStudio.Interface.Compiling;
using WebsiteStudio.Interface.Compiling.Security;

namespace WebsiteStudio.Core.Compiling {
	public class Compiler {
		
		internal const String FileExtensionHtml = "html";

		internal const String MetaDirectoryName = "meta";

		internal const String MediaDirectoryName = "media";

		internal const String DirectoryUp = "..";

		private readonly Project _Project;
		
		public bool Error { get; private set; }

		public IEnumerable<CompilerMessage> Messages
			=> _Exceptions.Select(x => new CompilerMessage(x))
			.Concat(_Messages)
			.OrderBy(x => x.MessageType)
			.Distinct();

		private readonly List<CompilerMessage> _Messages;
		
		private readonly List<String> _StyleSheetFiles;

		private readonly List<CompilerStep> _Steps;

		private readonly Dictionary<Type, int> _ModuleCompilerFlags;

		internal IDictionary<Type, int> ModuleCompilerFlags => _ModuleCompilerFlags;

		private readonly List<Exception> _Exceptions;
		
		private readonly Stopwatch _Stopwatch;

		public Compiler(Project project)
			: this(project, new CompilerSettings()) {
		}

		public Compiler(Project project, CompilerSettings settings) {
			Error = false;
			_Project = project;
			_StyleSheetFiles = new List<String>();
			_ModuleCompilerFlags = new Dictionary<Type, int>();
			_Exceptions = new List<Exception>();
			_Stopwatch = new Stopwatch();
			_Messages = new List<CompilerMessage>();
			
			ValidateProject(project);
			
			if (Error) {
				return;
			}

			DirectoryInfo outputDirectory = new DirectoryInfo(_Project.OutputPath);
			DirectoryInfo metaDirectory = new DirectoryInfo(Path.Combine(_Project.OutputPath, MetaDirectoryName));
			DirectoryInfo mediaDirectory = new DirectoryInfo(Path.Combine(_Project.OutputPath, MediaDirectoryName));

			_Project.ReloadTheme();
			
			_Steps = new List<CompilerStep>();

			_Steps.Add(new PrepareDirectoryStep(outputDirectory));
			_Steps.Add(new PrepareDirectoryStep(metaDirectory));
			_Steps.Add(new PrepareDirectoryStep(mediaDirectory));
			_Steps.Add(new BuildStyleSheetsStep(_Project.Theme, metaDirectory, _StyleSheetFiles));
			_Steps.Add(new BuildFontsStep(_Project.Theme, metaDirectory));
			_Steps.Add(new BuildImagesStep(_Project.Theme, metaDirectory, _StyleSheetFiles, _Project.Favicon));
			_Steps.Add(new CopyMediaStep(_Project.Media, mediaDirectory));

			if (_Project.GenerateSitemap) {
				_Steps.Add(new BuildSitemapStep(_Project, outputDirectory));
			}

			ReadOnlyCollection<String> styleSheetFiles = _StyleSheetFiles.AsReadOnly();
			List<PageSecurityInfo> pageSecurityInfos = new List<PageSecurityInfo>();

			foreach (Language language in _Project.Languages) {
				if (settings.PreviewLanguage != null && language.Id != settings.PreviewLanguage.Id) {
					continue;
				}

				foreach (Page page in _Project.AllPages) {
					if (settings.PreviewPage != null && page.Id != settings.PreviewPage.Id || page.Disable) {
						continue;
					}

					BuildPageStep step = new BuildPageStep(language, page, _Project.Theme, outputDirectory, styleSheetFiles);
					_Steps.Add(step);

					if (page.AllowedGroups.Any()) {
						PageSecurityInfo securityInfo = new PageSecurityInfo();
						securityInfo.Directory = step.File.Directory;
						securityInfo.Groups = page.AllowedGroups;
						pageSecurityInfos.Add(securityInfo);
					}
				}
			}

			if (_Project.Webserver != null) {
				_Steps.Add(new WebserverStep(_Project, pageSecurityInfos));
			}
		}

		public static void ClearOutputDirectory(Project project) {
			if (project == null || project.OutputPath == null) {
				return;
			}

			DirectoryInfo directory = new DirectoryInfo(project.OutputPath);
			if (!directory.Exists) {
				return;
			}

			PrepareDirectoryStep step = new PrepareDirectoryStep(directory);
			step.Run();
		}

		private void ValidateProject(Project project) {
			if (String.IsNullOrWhiteSpace(project.OutputPath) || !Directory.Exists(project.OutputPath)) {
				HandleException(new DirectoryNotFoundException("The output directory could not be found."));
			}

			if (project.Theme == null) {
				HandleException(new FileNotFoundException("The theme file could not be found.", project.ThemePath));
			}

			if (project.Languages.Length == 0) {
				HandleException(new Exception("No languages configured."));
			}

			if (project.StartPage == null) {
				HandleException(new Exception("No start page selected."));
			}

			if (project.Webserver == null) {
				_Messages.Add(new CompilerMessage("No web server configured.", CompilerMessageType.Information));
			}

			if (project.UglyURLs) {
				const String uglyURLsNoAuthSupport = "Using ugly URLs does not support the usage of page security.";

				if (project.AllPages.Any(x => x.AllowedGroups.Any())) {
					HandleException(new Exception(uglyURLsNoAuthSupport));
				}
				else {
					_Messages.Add(new CompilerMessage(uglyURLsNoAuthSupport, CompilerMessageType.Warning));
				}
			}
		}

		public async Task CompileAsync() {
			await CompileAsync(null);
		}

		public async Task CompileAsync(IProgress<CompilerProgressReport> progress) {
			await Task.Run(() => {
				Compile(progress);
			});
		}
		
		private CompilerProgressReport CreateProgressReport(int step, int steps, String message, params String[] args) {
			int percentage = 0;

			if (steps > 0) {
				percentage = (int)(((decimal)step / (decimal)steps) * 100.0M);
			}
			
			return new CompilerProgressReport(percentage, message, args);
		}

		public void Compile() {
			Compile(null);
		}

		public void Compile(IProgress<CompilerProgressReport> progress) {
			if (Error) {
				return;
			}

			progress?.Report(CreateProgressReport(0, 1, "------ Build started: {0} ------", _Project.ProjectFileName));

			_Stopwatch.Start();
			int steps = _Steps.Count;

			for(int i = 0; i < steps; i++) {
				var step = _Steps[i];
				progress?.Report(CreateProgressReport(i, steps, step.Output));

				try {
					step.Run();
					_Messages.AddRange(step.Messages);
				}
				catch (Exception ex) {
					HandleException(ex);
				}
			}

			_Stopwatch.Stop();
			progress?.Report(CreateProgressReport(1, 1, "Time elapsed: {0}", _Stopwatch.Elapsed.ToString()));
			
			if (!Error) {
				progress?.Report(CreateProgressReport(1, 1, "========== Build succeeded =========="));
			}
			else {
				progress?.Report(CreateProgressReport(1, 1, "========== Build failed =========="));
			}
		}

		internal static String CreateUrl(Page page, Language language) {
			List<String> path = new List<String>();
			Page parent = page.Parent as Page;

			while (parent != null) {
				path.Insert(0, parent.PathName);
				parent = parent.Parent as Page;
			}

			path.Insert(0, language.Id);

			int count = path.Count;
			for(int i = 0; i < count + 1; i++) {
				path.Insert(0, DirectoryUp);
			}

			if (page.Project.UglyURLs) {
				path.Add(String.Concat(page.PathName, ".", FileExtensionHtml));
			}
			else {
				path.Add(String.Concat(page.PathName, "/"));
			}

			return String.Join("/", path);
		}

		internal static String CreateUrl(Page targetPage) {
			return CreateUrl(targetPage, (Page)null);
		}

		internal static String CreateUrl(Page targetPage, Page currentPage) {
			if (targetPage == null) {
				return String.Empty;
			}

			if (targetPage == currentPage) {
				return ".";
			}

			List<String> path = new List<String>();
			Page parent = targetPage.Parent as Page;
			bool targetPageIsSubpageOfCurrentPage = false;

			while (parent != null) {
				if (parent == currentPage) {
					targetPageIsSubpageOfCurrentPage = true;
					break;
				}

				path.Insert(0, parent.PathName);
				parent = parent.Parent as Page;
			}

			if (targetPage.Project.UglyURLs) {
				path.Add(String.Concat(targetPage.PathName, ".", FileExtensionHtml));
			}
			else {
				path.Add(String.Concat(targetPage.PathName, "/"));
			}

			if (!targetPageIsSubpageOfCurrentPage) {
				for (int i = 0; i < (currentPage?.Level ?? 0); i++) {
					path.Insert(0, DirectoryUp);
				}
			}

			return String.Join("/", path);
		}
		
		private DirectoryInfo GetChildDirectory(DirectoryInfo currentDirectory, Page page) {
			String path = Path.Combine(currentDirectory.FullName, page.PathName);
			DirectoryInfo info = new DirectoryInfo(path);
			info.Create();
			return info;
		}

		private void HandleException(Exception ex) {
			Error = true;
			_Exceptions.Add(ex);
		}

		public static IHtmlElement CreateHtmlElement(String name) {
			return new HtmlElement(name);
		}
	}
}
