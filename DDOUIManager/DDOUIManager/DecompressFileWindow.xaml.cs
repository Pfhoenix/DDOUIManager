using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace DDOUIManager
{
	/// <summary>
	/// Interaction logic for DecompressFileWindow.xaml
	/// </summary>
	public partial class DecompressFileWindow : Window
	{
		public string ErrorMessage { get; private set; }

		string TempPath;
		string FileName;

		public DecompressFileWindow()
		{
			InitializeComponent();
		}

		public void Initialize(string tp, string fn)
		{
			TempPath = tp;
			FileName = fn;
		}

		bool _shown;
		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);

			if (_shown)
				return;

			_shown = true;

			if (string.Compare(Path.GetExtension(FileName), ".rar", true) == 0)
			{
				ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(SettingsManager.data.WinRARPath, "rar.exe"), "x \"" + FileName + "\" \"" + TempPath + "\"");
				psi.WindowStyle = ProcessWindowStyle.Minimized;
				Process p = Process.Start(psi);
				p.EnableRaisingEvents = true;
				p.Exited += RarDecompress_Completed;
			}
			else if (string.Compare(Path.GetExtension(FileName), ".zip", true) == 0)
			{
				BackgroundWorker bw = new BackgroundWorker();
				bw.DoWork += ZipFileDecompress_DoWork;
				bw.RunWorkerCompleted += ZipFileDecompress_Completed;
				bw.RunWorkerAsync();
			}
		}

		private void RarDecompress_Completed(object sender, EventArgs e)
		{
			Dispatcher.InvokeAsync(new Action(DecompressionCompleted));
		}

		private void ZipFileDecompress_Completed(object sender, RunWorkerCompletedEventArgs e)
		{
			Dispatcher.InvokeAsync(new Action(DecompressionCompleted));
		}

		private void ZipFileDecompress_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				ZipFile.ExtractToDirectory(FileName, TempPath);
			}
			catch
			{
				ErrorMessage = "Error extracting from the zip file! Ensure the file is an actual zip archive file.";
			}
		}

		private void DecompressionCompleted()
		{
			DialogResult = true;
			Close();
		}
	}
}

