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
		readonly string DDOPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dungeons and Dragons Online\\UI\\Skins\\DDO UI Manager");
		readonly string SkinsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins");
		readonly string TempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

		List<DDOUISkin> Skins = new List<DDOUISkin>();

		public MainWindow()
		{
			InitializeComponent();
#if DEBUG
			miDeveloper.IsEnabled = true;
			miDeveloper.SetValue(VisibilityProperty, Visibility.Visible);
#else
			miDeveloper.IsEnabled = false;
			miDeveloper.SetValue(VisibilityProperty, Visibility.Hidden);
#endif

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

			miBackupSkins.IsEnabled = !string.IsNullOrWhiteSpace(SettingsManager.data.BackupPath);

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
			view.Refresh();
			view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
		}

		private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow sw = new SettingsWindow();
			sw.ShowDialog();
			miBackupSkins.IsEnabled = !string.IsNullOrWhiteSpace(SettingsManager.data.BackupPath);
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
				bw.ProgressChanged += Process_ProgressChanged;
				bw.RunWorkerCompleted += Process_Completed;
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

		private void Process_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Dispatcher.InvokeAsync(new Action(() => UpdateStatusBar((StatusBarUpdate)e.UserState)));
		}

		private void Process_Completed(object sender, RunWorkerCompletedEventArgs e)
		{
			Dispatcher.InvokeAsync(ProcessDone);
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

		void ProcessDone()
		{
			Cursor = Cursors.Arrow;
			tbStatusBarText.Text = "Done";
			tbProgressText.Text = null;
			pbProgressBar.Value = 0;

			DisplayProcessErrors();
		}

		void DisplayProcessErrors()
		{
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
			Cursor = Cursors.Wait;
			foreach (var skin in Skins)
			{
				tbStatusBarText.Text = "Backing up " + skin.Name;
				CompressFileWindow cfw = new CompressFileWindow();
				cfw.Owner = this;
				cfw.Initialize(SettingsManager.data.BackupPath, skin.RootPath);
				cfw.ShowDialog();
				if (!string.IsNullOrWhiteSpace(cfw.ErrorMessage))
				{
					MessageBox.Show(cfw.ErrorMessage, "Error backing up " + skin.Name, MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			Cursor = Cursors.Arrow;
			tbStatusBarText.Text = "Done.";
		}

		DDOUISkin SkinToApply;
		private void ApplyMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SkinToApply = lvSkins.SelectedItem as DDOUISkin;
			if (SkinToApply == null) return;

			try
			{
				if (Directory.Exists(DDOPath)) DeleteFolder(DDOPath);
				Directory.CreateDirectory(DDOPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Stop);
				return;
			}

			Cursor = Cursors.Wait;

			BackgroundWorker bw = new BackgroundWorker();
			bw.WorkerReportsProgress = true;
			bw.DoWork += ApplySkin_DoWork;
			bw.ProgressChanged += Process_ProgressChanged;
			bw.RunWorkerCompleted += Process_Completed;
			bw.RunWorkerAsync();
		}

		private void ApplySkin_DoWork(object sender, DoWorkEventArgs e)
		{
			ProcessErrors = new StringBuilder();
			BackgroundWorker bw = sender as BackgroundWorker;
			StatusBarUpdate sbu;

			sbu.MainString = "Applying " + SkinToApply.Name + " ...";
			sbu.ProgressBarMin = 0;

			ProcessErrors.Clear();
			var files = Directory.GetFiles(SkinToApply.RootPath);
			sbu.ProgressBarMax = files.Length;
			for (int i = 0; i < files.Length; i++)
			{
				try
				{
					sbu.SecondaryString = (i + 1).ToString();
					sbu.ProgressBarValue = i + 1;
					bw.ReportProgress(i, sbu);
					File.Copy(files[i], Path.Combine(DDOPath, Path.GetFileName(files[i])));
				}
				catch (Exception ex)
				{
					ProcessErrors.AppendLine(ex.Message);
				}
			}

			XmlDocument doc = new XmlDocument();
			try
			{
				string docpath = Path.Combine(DDOPath, "SkinDefinition.xml");
				doc.Load(docpath);
				XmlElement xe = doc.GetElementsByTagName("SkinName")[0] as XmlElement;
				xe.SetAttribute("Name", "DDO UI Manager");
				doc.Save(docpath);
			}
			catch (Exception ex)
			{
				ProcessErrors.AppendLine(ex.Message);
			}
		}

		private void RenameSkin_Click(object sender, RoutedEventArgs e)
		{
			DDOUISkin skin = lvSkins.SelectedItem as DDOUISkin;
			if (skin == null) return;
			RenameSkinWindow rsw = new RenameSkinWindow(skin.Name, Skins.Select(s => s.Name).ToList());
			rsw.Owner = this;
			if (rsw.ShowDialog() == true)
			{
				string errors = skin.Rename(rsw.NewSkinName);
				if (!string.IsNullOrWhiteSpace(errors))
				{
					MessageBox.Show(errors, "Rename Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			RefreshSkinList();
		}

		private void DeleteSkin_Click(object sender, RoutedEventArgs e)
		{
			DDOUISkin skin = lvSkins.SelectedItem as DDOUISkin;
			if (skin == null) return;

			if (MessageBox.Show("Are you sure you want to delete " + skin.Name + "?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

			Skins.Remove(skin);
			DeleteFolder(skin.RootPath);

			RefreshSkinList();
		}

		private void Skin_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lvSkins.SelectedItem == null) lvSkins.ContextMenu = null;
			else lvSkins.ContextMenu = lvSkins.Resources["ItemCM"] as ContextMenu;
		}

		private void Skin_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			Point pt = e.GetPosition(lvSkins);
			var hit = System.Windows.Media.VisualTreeHelper.HitTest(lvSkins, pt);
			if (hit.VisualHit is TextBlock) { }
			else lvSkins.SelectedItem = null;
		}

		private void GenerateCategoryTreeMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SkinAssetTreeWindow satw = new SkinAssetTreeWindow();
			satw.ShowDialog();
		}
	}
}
