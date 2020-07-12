using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


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
			var subdirs = Directory.GetDirectories(SkinsPath);
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

		public void EmptyDirectory(string directory)
		{
			// First delete all the files, making sure they are not readonly
			var stackA = new Stack<DirectoryInfo>();
			stackA.Push(new DirectoryInfo(directory));

			var stackB = new Stack<DirectoryInfo>();
			while (stackA.Any())
			{
				var dir = stackA.Pop();
				foreach (var file in dir.GetFiles())
				{
					file.IsReadOnly = false;
					file.Delete();
				}
				foreach (var subDir in dir.GetDirectories())
				{
					stackA.Push(subDir);
					stackB.Push(subDir);
				}
			}

			Thread.Sleep(50);

			// Then delete the sub directories depth first
			while (stackB.Any())
			{
				stackB.Pop().Delete();
			}
		}

		private void AddSkinArchiveMenuItem_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (string.IsNullOrWhiteSpace(SettingsManager.data.WinRARPath)) ofd.Filter = "Archive file (*.zip)|*.zip";
			else ofd.Filter = "Archive file (*.zip, *.rar)|*.zip;*.rar";
			if (ofd.ShowDialog() != true) return;

			if (Directory.Exists(TempPath))
			{
				try
				{
					//EmptyDirectory(TempPath);
					ProcessStartInfo Info = new ProcessStartInfo();
					Info.Arguments = "/C rd /s /q \"" + TempPath + "\"";  
					Info.WindowStyle = ProcessWindowStyle.Hidden;
					Info.CreateNoWindow = true;
					Info.FileName = "cmd.exe";
					Process p = Process.Start(Info);
					while (!p.HasExited) { }
				}
				catch
				{
					MessageBox.Show("Error emptying the temp folder! Try deleting the temporary folder by hand (" + TempPath + ") and then trying again.", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
					return;
				}
			}

			Directory.CreateDirectory(TempPath);

			DecompressFileWindow dfw = new DecompressFileWindow();
			dfw.Owner = this;
			dfw.Initialize(TempPath, ofd.FileName);
			if (dfw.ShowDialog() == true)
			{

			}
			else
			{
				MessageBox.Show(dfw.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
			}
		}


		private void AddSkinFolderMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void BackupSkinsMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
