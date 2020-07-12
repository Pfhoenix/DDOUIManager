using System;
using System.IO;
using System.Xml.Serialization;

namespace DDOUIManager
{
	[Serializable]
	public class SettingsManager
	{
		static SettingsManager _Data;
		public static SettingsManager data
		{
			get
			{
				try
				{
					if (_Data == null)
					{
						XmlSerializer reader = new XmlSerializer(typeof(SettingsManager));
						using (FileStream fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml"), FileMode.Open))
						{
							_Data = reader.Deserialize(fs) as SettingsManager;
						}
					}
				}
				catch
				{
					_Data = new SettingsManager();
				}

				return _Data;
			}
		}

		public string BackupPath;
		public string WinRARPath;

		SettingsManager() { }

		public static bool Save()
		{
			try
			{
				XmlSerializer writer = new XmlSerializer(typeof(SettingsManager));
				using (FileStream fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml"), FileMode.Create))
				{
					writer.Serialize(fs, _Data);
				}

				return true;
			}
			catch { return false; }
		}
	}
}
