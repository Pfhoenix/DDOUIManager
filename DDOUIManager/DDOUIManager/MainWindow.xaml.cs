using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;


namespace DDOUIManager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		readonly string DDOPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dungeons and Dragons Online\\UI\\Skins");
		readonly string SkinsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins");
		readonly string TempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

		List<DDOUISkin> Skins = new List<DDOUISkin>();

		public MainWindow()
		{
			InitializeComponent();

			if (!Directory.Exists(SkinsPath))
			{
				try
				{
					Directory.CreateDirectory(SkinsPath);
				}
				catch
				{
					MessageBox.Show("Unable to create UI skins directory in application folder!", "Application Error", MessageBoxButton.OK, MessageBoxImage.Stop);
					Close();
				}
			}

			if (!Directory.Exists(DDOPath))
			{
				try
				{
					Directory.CreateDirectory(DDOPath);
				}
				catch
				{
					MessageBox.Show("Unable to create the appropriate folder in your documents folder!", "Application Error", MessageBoxButton.OK, MessageBoxImage.Stop);
					Close();
				}
			}

			// scan local Skins directory for "installed" skins
			var skindefs = Directory.GetFiles(SkinsPath, "SkinDefinition.xml", SearchOption.AllDirectories);
			foreach (var sd in skindefs)
			{
				DDOUISkin skin = DDOUISkin.Load(sd);
				if (skin != null) Skins.Add(skin);
			}
			lvSkins.ItemsSource = Skins;
		}

		void RefreshSkinList()
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvSkins.ItemsSource);
			view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
			view.Refresh();
		}

		private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow sw = new SettingsWindow();
			sw.ShowDialog();
		}

		bool DeleteFolder(string path)
		{
			try
			{
				ProcessStartInfo Info = new ProcessStartInfo();
				Info.Arguments = "/C rd /s /q \"" + path + "\"";
				Info.WindowStyle = ProcessWindowStyle.Hidden;
				Info.CreateNoWindow = true;
				Info.FileName = "cmd.exe";
				Process p = Process.Start(Info);
				while (!p.HasExited) { }

				return true;
			}
			catch
			{
				MessageBox.Show("Error deleting the temp folder! Try deleting the temporary folder by hand (" + path + ") and then trying again.", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
				return false;
			}
		}

		List<string> XDocPaths = new List<string>();
		List<XmlDocument> XDocsToProcess = new List<XmlDocument>();

		void ProcessFolderToAddSkins(string path)
		{
			// enumerate all SkinDefinition.xml files in TempPath
			string[] files = Directory.GetFiles(path, "SkinDefinition.xml", SearchOption.AllDirectories);
			XDocPaths.Clear();
			XDocsToProcess.Clear();
			// validate uniqueness of skin names
			foreach (string f in files)
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(f);
				XmlElement xe = doc.SelectSingleNode("opt/SkinName") as XmlElement;
				if (xe == null) continue;
				string n = xe.GetAttribute("Name");
				if (string.IsNullOrWhiteSpace(n)) continue;
				if (Skins.Exists(s => s.Name == n))
				{
					SkinNameConflictWindow sncw = new SkinNameConflictWindow(n, Skins.Select(sn => sn.Name).ToList());
					sncw.Owner = this;
					if (sncw.ShowDialog() == true)
					{
						if (sncw.SelectedOption == SkinNameConflictWindow.SkipImporting) continue;
						else if (sncw.SelectedOption == SkinNameConflictWindow.RenameExisting)
						{
							var skin = Skins.Find(s => s.Name == n);
							skin.Rename(sncw.NewSkinName);
						}
						else if (sncw.SelectedOption == SkinNameConflictWindow.RenameImporting)
						{
							xe.SetAttribute("Name", sncw.NewSkinName);
						}
					}
					else continue;
				}
				XDocPaths.Add(Path.GetDirectoryName(f));
				XDocsToProcess.Add(doc);
			}

			if (XDocsToProcess.Count == 0)
			{
				MessageBox.Show("No skins were processed and no changes made.", "Null operation", MessageBoxButton.OK, MessageBoxImage.Information);
				Cursor = Cursors.Arrow;
			}
			else
			{
				BackgroundWorker bw = new BackgroundWorker();
				bw.WorkerReportsProgress = true;
				bw.DoWork += ProcessXDocs_DoWork;
				bw.ProgressChanged += ProcessXDocs_ProgressChanged;
				bw.RunWorkerCompleted += ProcessXDocs_Completed;
				bw.RunWorkerAsync();
			}
		}

		struct StatusBarUpdate
		{
			public string MainString;
			public string SecondaryString;
			public int ProgressBarMin;
			public int ProgressBarValue;
			public int ProgressBarMax;
		}

		StringBuilder ProcessErrors;

		private void ProcessXDocs_DoWork(object sender, DoWorkEventArgs e)
		{
			ProcessErrors = new StringBuilder();
			BackgroundWorker bw = sender as BackgroundWorker;
			StatusBarUpdate sbu;
			DDOUISkin skin;
			for (int i = 0; i < XDocsToProcess.Count; i++)
			{
				skin = new DDOUISkin();
				XmlElement xe = XDocsToProcess[i].GetElementsByTagName("SkinName")[0] as XmlElement;
				skin.Name = xe.GetAttribute("Name");
				skin.RootPath = Path.Combine(SkinsPath, skin.Name);
				try
				{
					if (Directory.Exists(skin.RootPath))
					{
						if (!DeleteFolder(skin.RootPath))
						{
							ProcessErrors.AppendLine("Unable to delete existing skin directory " + skin.RootPath);
							continue;
						}
					}
					Directory.CreateDirectory(skin.RootPath);
				}
				catch (Exception ex)
				{
					ProcessErrors.AppendLine(ex.Message);
					continue;
				}
				sbu.MainString = "Processing " + skin.Name;
				var maps = XDocsToProcess[i].GetElementsByTagName("Mapping");
				sbu.ProgressBarMax = maps.Count;
				sbu.ProgressBarMin = 0;

				for (int m = 0; m < maps.Count; m++)
				{
					sbu.SecondaryString = (m + 1).ToString();
					sbu.ProgressBarValue = m + 1;
					bw.ReportProgress(m, sbu);

					xe = maps[m] as XmlElement;
					string fn = xe.GetAttribute("FileName").Replace('/', '\\');
					DDOUISkinAsset sa = skin.AddAsset(xe.GetAttribute("ArtAssetID"), Path.Combine(skin.RootPath, Path.GetFileName(fn)), skin.Name);
					if (sa == null) continue;
					try
					{
						File.Copy(Path.Combine(XDocPaths[i], fn), sa.AssetPath, true);
					}
					catch (Exception ex)
					{
						ProcessErrors.AppendLine(ex.Message);
						continue;
					}
					xe.SetAttribute("FileName", Path.GetFileName(sa.AssetPath));
				}

				try
				{
					XDocsToProcess[i].Save(Path.Combine(skin.RootPath, "SkinDefinition.xml"));
				}
				catch (Exception ex)
				{
					ProcessErrors.AppendLine(ex.Message);
					continue;
				}

				Dispatcher.Invoke(new Action(() => ProcessNewSkin(skin)));
			}

			XDocPaths.Clear();
			XDocsToProcess.Clear();
		}

		private void ProcessXDocs_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Dispatcher.InvokeAsync(new Action(() => UpdateStatusBar((StatusBarUpdate)e.UserState)));
		}

		private void ProcessXDocs_Completed(object sender, RunWorkerCompletedEventArgs e)
		{
			Dispatcher.InvokeAsync(ProcessXDocsDone);
		}

		void UpdateStatusBar(StatusBarUpdate update)
		{
			tbStatusBarText.Text = update.MainString;
			tbProgressText.Text = update.SecondaryString;
			pbProgressBar.Minimum = update.ProgressBarMin;
			pbProgressBar.Maximum = update.ProgressBarMax;
			pbProgressBar.Value = update.ProgressBarValue;
		}

		void ProcessNewSkin(DDOUISkin skin)
		{
			DDOUISkin old = Skins.Find(s => string.Compare(s.Name, skin.Name, true) == 0);
			if (old != null) Skins.Remove(old);
			Skins.Add(skin);
			RefreshSkinList();
		}

		void ProcessXDocsDone()
		{
			Cursor = Cursors.Arrow;
			tbStatusBarText.Text = "Done";
			tbProgressText.Text = null;
			pbProgressBar.Value = 0;

			if (ProcessErrors.Length > 0)
			{
				string ef = Path.Combine(TempPath, "errors.txt");
				File.WriteAllText(ef, ProcessErrors.ToString());
				Process.Start(ef);
			}
		}

		private void AddSkinArchiveMenuItem_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (string.IsNullOrWhiteSpace(SettingsManager.data.WinRARPath)) ofd.Filter = "Archive file (*.zip)|*.zip";
			else ofd.Filter = "Archive file (*.zip, *.rar)|*.zip;*.rar";
			if (ofd.ShowDialog() != true) return;

			if (Directory.Exists(TempPath) && !DeleteFolder(TempPath)) return;

			Directory.CreateDirectory(TempPath);

			DecompressFileWindow dfw = new DecompressFileWindow();
			dfw.Owner = this;
			dfw.Initialize(TempPath, ofd.FileName);
			Cursor = Cursors.Wait;
			if (dfw.ShowDialog() == true)
			{
				ProcessFolderToAddSkins(TempPath);
			}
			else
			{
				Cursor = Cursors.Arrow;
				MessageBox.Show(dfw.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
			}
		}

		private void AddSkinFolderMenuItem_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
			Cursor = Cursors.Wait;
			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				ProcessFolderToAddSkins(fbd.SelectedPath);
			}
			else Cursor = Cursors.Arrow;
		}

		private void BackupSkinsMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void ApplyMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void RenameSkin_Click(object sender, RoutedEventArgs e)
		{

		}

		private void DeleteSkin_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
