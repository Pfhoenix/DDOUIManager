using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	}
}
