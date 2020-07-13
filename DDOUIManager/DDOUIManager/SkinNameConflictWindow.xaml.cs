using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DDOUIManager
{
	/// <summary>
	/// Interaction logic for SkinNameConflictWindow.xaml
	/// </summary>
	public partial class SkinNameConflictWindow : Window
	{
		public const string SkipImporting = "Skip Importing Skin";
		public const string OverwriteExisting = "Overwrite Existing Skin";
		public const string RenameExisting = "Rename Existing Skin";
		public const string RenameImporting = "Rename Importing Skin";

		List<string> ExistingSkinNames;
		string[] Options =
		{
			SkipImporting,
			OverwriteExisting,
			RenameExisting,
			RenameImporting
		};

		public string SelectedOption { get; set; }
		public string NewSkinName { get; set; }

		public SkinNameConflictWindow(string sn, List<string> esn)
		{
			InitializeComponent();
			DataContext = this;
			lbl.Content = lbl.Content.ToString().Replace("%N", sn);
			ExistingSkinNames = esn;
			cbOptions.ItemsSource = Options;
		}

		void EvaluateOkayState()
		{
			btnOkay.IsEnabled = !tbRename.IsEnabled || (!string.IsNullOrWhiteSpace(tbRename.Text) && !ExistingSkinNames.Any(s => string.Compare(s, tbRename.Text, true) == 0));
		}

		private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			tbRename.IsEnabled = cbOptions.SelectedIndex > 1;
			EvaluateOkayState();
		}

		private void Rename_TextChanged(object sender, TextChangedEventArgs e)
		{
			EvaluateOkayState();
		}

		private void Rename_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter && btnOkay.IsEnabled) Okay_Clicked(null, null);
		}

		private void Okay_Clicked(object sender, RoutedEventArgs e)
		{
			EvaluateOkayState();
			if (!btnOkay.IsEnabled) return;

			DialogResult = true;
			Close();
		}
	}
}
