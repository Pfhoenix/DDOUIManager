using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DDOUIManager
{
	/// <summary>
	/// Interaction logic for RenameSkinWindow.xaml
	/// </summary>
	public partial class RenameSkinWindow : Window
	{
		List<string> ExistingSkinNames;

		public string NewSkinName { get; set; }

		public RenameSkinWindow(string sn, List<string> esn)
		{
			InitializeComponent();
			DataContext = this;
			ExistingSkinNames = esn;
			NewSkinName = sn;
		}

		void EvaluateOkayState()
		{
			btnOkay.IsEnabled = !string.IsNullOrWhiteSpace(tbRename.Text) && !ExistingSkinNames.Any(s => string.Compare(s, tbRename.Text, true) == 0);
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
