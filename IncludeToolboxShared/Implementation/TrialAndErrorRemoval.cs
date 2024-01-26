using System;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using System.IO;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
	internal sealed class AsyncDispatcher : IDisposable
	{
		TaskCompletionSource<bool> tcs;
		private bool disposedValue;

		private void Dispatch(bool a)
		{
			tcs.SetResult(a);
		}
		public AsyncDispatcher()
		{
			VS.Events.BuildEvents.SolutionBuildDone += Dispatch;
		}
		~AsyncDispatcher()
		{
			if (!disposedValue)
				VS.Events.BuildEvents.SolutionBuildDone -= Dispatch;
		}

		public async Task<bool> CompileAsync(VCFileConfiguration config)
		{
			tcs = new();
			for (int i = 0; i < 3; i++)
			{
				try
				{
					config.Compile(true, false);
					return await tcs.Task;
				}
				catch (Exception)
				{
					await Task.Delay(100);
				}
			}
			return false;
		}

		void IDisposable.Dispose()
		{
			if (!disposedValue)
			{
				VS.Events.BuildEvents.SolutionBuildDone -= Dispatch;
				disposedValue = true;
			}
			GC.SuppressFinalize(this);
		}
	}
	internal sealed class DialogGuard : IDisposable
	{
		private bool disposedValue;
		public IVsThreadedWaitDialog4 Dialog { get; private set; }

		public DialogGuard(IVsThreadedWaitDialog4 dialog)
		{
			this.Dialog = dialog;
		}

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}
				Dialog.EndWaitDialog();
				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		~DialogGuard()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
	internal sealed class TrialAndErrorRemoval
	{
		public static bool WorkInProgress { get; private set; }

		public int Removed { get; private set; } = 0;


		//Makes less variable noise
		struct Descriptor
		{
			public IncludeLine[] lines;
			public ITextBuffer buffer;
			public string text;
			public string filename;
			public VCFileConfiguration config;
			public int offset;
			public TrialAndErrorRemovalOptions settings;
		}


		private async Task<bool> TestCompileAsync(VCFileConfiguration config)
		{
			using AsyncDispatcher dispatcher = new();
			return await dispatcher.CompileAsync(config);
		}



        public async Task<string> StartGenericAsync(VCFileConfiguration config, DocumentView document, TrialAndErrorRemovalOptions settings)
		{
            var buffer = document.TextBuffer;
            var snap = buffer.CurrentSnapshot;

            var text = snap.GetText();
            var span = Utils.GetIncludeSpan(text);
            text = text.Substring(span.Start, span.Length);

            var lines = Parser.ParseInclues(text.AsSpan(), settings.IgnoreIfdefs);
            var iterator = lines.Where(s => !s.Keep);

            // Filter regecies
            string documentName = Path.GetFileNameWithoutExtension(document.FilePath);
            string[] ignoreRegexList = RegexUtils.FixupRegexes(settings.IgnoreList, documentName);
            iterator = iterator.Where(line => !ignoreRegexList.Any(regexPattern =>
                                                                 new System.Text.RegularExpressions.Regex(regexPattern).Match(line.Content).Success));

            iterator = settings.RemovalOrder == IncludeRemovalOrder.TopToBottom ? iterator : iterator.Reverse();

            var array = iterator.ToArray();
            try
            {
                Descriptor desc = new()
                {
                    lines = array,
                    settings = settings,
                    config = config,
                    text = text,
                    filename = document.FilePath,
                    buffer = buffer,
                    offset = span.Start
                };
                await RemoveAsync(desc);
                _ = Output.WriteLineAsync($"Successfully removed {Removed} headers from {desc.filename}");
            }
            catch (Exception e)
            {
                return $"Failed to create a dialog: {e.Message}";
            }
            return "";
        }

        private async Task RemoveAsync(Descriptor desc)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			using DialogGuard dialog = new(await StartProgressDialogAsync(desc.filename, desc.lines.Length + 1));
			int delta = 0;
			using AsyncDispatcher dispatcher = new();
			int step = 0;

			foreach (var line in desc.lines)
			{
				string waitMessage = $"Removing #includes from '{desc.filename}'";
				string progressText = $"Trying to remove '{line.Content}' ...";
				dialog.Dialog.UpdateProgress(
					szUpdatedWaitMessage: waitMessage,
					szProgressText: progressText,
					szStatusBarText: "Running Trial & Error Removal - " + waitMessage + " - " + progressText,
					iCurrentStep: ++step,
					iTotalSteps: desc.lines.Length + 1,
					fDisableCancel: false,
					pfCanceled: out var canceled);

				if (canceled)
				{
					_ = Output.WriteLineAsync("Operation was cancelled.");
					return;
				}

				var rs = desc.settings.KeepLineBreaks ?
					line.ReplaceSpanWithoutNewline(desc.offset) :
					line.ReplaceSpan(desc.offset);

				desc.buffer.Delete(rs);

				bool b = await dispatcher.CompileAsync(desc.config);

				if (b)
				{
					if (desc.settings.RemovalOrder == IncludeRemovalOrder.TopToBottom)
					{
						desc.offset -= rs.Length;
						delta += rs.Length;
					}
					await Output.WriteLineAsync($"{line.FullFile} was successfully removed");
					Removed++;
					continue;
				}
				desc.buffer.Insert(rs.Start, desc.text.Substring(rs.Start - desc.offset + delta, rs.Length));
				await Output.WriteLineAsync($"Unable to remove {line.FullFile}");
			}
		}
		public async Task<string> StartHeaderAsync(VCFile file, VCFile support,  TrialAndErrorRemovalOptions settings)
		{
            var document = await VS.Documents.GetDocumentViewAsync(file.FullPath);
            VCFileConfiguration config = VCUtil.GetVCFileConfig(support);
            if (config == null) return $"{support.Name} has failed to yield a config.";

            return !await TestCompileAsync(config)
				? $"{file.FullPath} failed to compile. Include removal stopped."
				: await StartGenericAsync(config, document, settings);
		}
		//Expected: compilable file .cpp or other
		public async Task<string> StartAsync(VCFile file, TrialAndErrorRemovalOptions settings)
		{
			var document = await VS.Documents.GetDocumentViewAsync(file.FullPath);
            VCFileConfiguration config = VCUtil.GetVCFileConfig(file);
            if (config == null) return $"{file.Name} has failed to yield a config.";

			return !await TestCompileAsync(config)
				? $"{document.FilePath} failed to compile. Include removal stopped."
				: await StartGenericAsync(config, document, settings);
		}

		private async Task<IVsThreadedWaitDialog4> StartProgressDialogAsync(string documentName, int steps)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var dialog_factory = (IVsThreadedWaitDialogFactory)await VS.Services.GetThreadedWaitDialogAsync();
			var dialog = dialog_factory.CreateInstance();


			string waitMessage = $"Parsing '{documentName}' ... ";
			dialog.StartWaitDialogWithPercentageProgress(
				szWaitCaption: "Include Toolbox - Running Trial & Error Include Removal",
								szWaitMessage: waitMessage,
								szProgressText: null,
								varStatusBmpAnim: null,
								szStatusBarText: "Running Trial & Error Removal - " + waitMessage,
								fIsCancelable: true,
								iDelayToShowDialog: 0,
								iTotalSteps: steps,
								iCurrentStep: 0);

			return dialog;
		}
	}
}