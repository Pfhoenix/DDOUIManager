using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace DDOUIManager
{
	/// <summary>
	/// Interaction logic for CompressFileWindow.xaml
	/// </summary>
	public partial class CompressFileWindow : Window
	{
		public string ErrorMessage { get; private set; }

		string BackupPath;
		string RootPath;

		public CompressFileWindow()
		{
			InitializeComponent();
		}

		public void Initialize(string bp, string rp)
		{
			BackupPath = bp;
			RootPath = rp;
		}

		bool _shown;
		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);

			if (_shown)
				return;

			_shown = true;

			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += ZipFileCompress_DoWork;
			bw.RunWorkerCompleted += ZipFileCompress_Completed;
			bw.RunWorkerAsync();
		}

		private void ZipFileCompress_Completed(object sender, RunWorkerCompletedEventArgs e)
		{
			Dispatcher.InvokeAsync(new Action(CompressionCompleted));
		}

		private void ZipFileCompress_DoWork(object sender, DoWorkEventArgs e)
		{
			string filename = Path.Combine(BackupPath, Path.GetDirectoryName(RootPath) + ".zip");
			try
			{
				if (File.Exists(filename)) File.Delete(filename);
				ZipFile.CreateFromDirectory(RootPath, filename);
			}
			catch (Exception ex)
			{
				ErrorMessage = ex.Message;
			}
		}

		private void CompressionCompleted()
		{
			DialogResult = true;
			Close();
		}
	}
}

