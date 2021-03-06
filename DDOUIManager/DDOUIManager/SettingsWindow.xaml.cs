﻿using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DDOUIManager
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();

			txtBackupPath.Text = SettingsManager.data.BackupPath;
			txtWinRARPath.Text = SettingsManager.data.WinRARPath;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			bool save = false;
			if (Directory.Exists(txtBackupPath.Text))
			{
				SettingsManager.data.BackupPath = txtBackupPath.Text;
				save = true;
			}
			if (File.Exists(Path.Combine(txtWinRARPath.Text, "rar.exe")))
			{
				SettingsManager.data.WinRARPath = txtWinRARPath.Text;
				save = true;
			}

			if (save) SettingsManager.Save();

			base.OnClosing(e);
		}

		void SetTextFromFolderSearch(TextBox tb)
		{
			System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) tb.Text = fbd.SelectedPath;
		}

		private void FindBackupPath_Click(object sender, RoutedEventArgs e)
		{
			SetTextFromFolderSearch(txtBackupPath);
		}

		private void FindWinRARPath_Click(object sender, RoutedEventArgs e)
		{
			SetTextFromFolderSearch(txtWinRARPath);
		}
	}
}
