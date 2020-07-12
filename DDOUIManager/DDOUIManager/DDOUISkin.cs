using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace DDOUIManager
{
	public class DDOUISkinAsset
	{
		public string AssetName;
		public string AssetPath;
		public string AssetSource;
	}

	public class DDOUISkin
	{
		public string Name;
		public string RootPath;
		public List<DDOUISkinAsset> Assets = new List<DDOUISkinAsset>();
		Dictionary<string, DDOUISkinAsset> AssetCache = new Dictionary<string, DDOUISkinAsset>();

		public DDOUISkinAsset this[string a]
		{
			get
			{
				if (AssetCache.TryGetValue(a, out DDOUISkinAsset sa)) return sa;
				else return null;
			}
		}

		public static DDOUISkin Load(string rp)
		{
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load(Path.Combine(rp, "SkinDefinition.xml"));
				XmlElement xe = doc.GetElementsByTagName("SkinName")[0] as XmlElement;
				DDOUISkin skin = new DDOUISkin()
				{
					Name = xe.GetAttribute("Name"),
					RootPath = rp
				};
				if (string.IsNullOrWhiteSpace(skin.Name)) return null;
				var mas = doc.GetElementsByTagName("Mapping");
				foreach (XmlElement ma in mas)
				{
					DDOUISkinAsset sa = new DDOUISkinAsset
					{
						AssetName = ma.GetAttribute("ArtAssetID"),
						AssetPath = Path.Combine(rp, ma.GetAttribute("FileName")),
						AssetSource = skin.Name
					};
					if (skin.AssetCache.ContainsKey(sa.AssetName)) continue;
					skin.Assets.Add(sa);
					skin.AssetCache[sa.AssetName] = sa;
				}

				return skin;
			}
			catch
			{
				return null;
			}
		}
	}
}
