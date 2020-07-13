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

		public DDOUISkinAsset AddAsset(string n, string p, string s)
		{
			if (AssetCache.ContainsKey(n)) return null;
			DDOUISkinAsset sa = new DDOUISkinAsset
			{
				AssetName = n,
				AssetPath = p,
				AssetSource = s
			};
			Assets.Add(sa);
			AssetCache[sa.AssetName] = sa;

			return sa;
		}

		public static DDOUISkin Load(XmlDocument doc, string rp)
		{
			try
			{
				XmlElement xe = doc.GetElementsByTagName("SkinName")[0] as XmlElement;
				DDOUISkin skin = new DDOUISkin()
				{
					Name = xe.GetAttribute("Name"),
					RootPath = rp
				};
				if (string.IsNullOrWhiteSpace(skin.Name)) return null;
				var mas = doc.GetElementsByTagName("Mapping");
				foreach (XmlElement ma in mas)
					skin.AddAsset(ma.GetAttribute("ArtAssetID"), Path.Combine(rp, ma.GetAttribute("FileName")), skin.Name);

				return skin;
			}
			catch
			{
				return null;
			}
		}

		public static DDOUISkin Load(string sdfile)
		{
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load(sdfile);
				string rp = Path.GetDirectoryName(sdfile);

				return Load(doc, rp);
			}
			catch
			{
				return null;
			}
		}

		public string Rename(string newname)
		{
			int i = RootPath.LastIndexOf(Name, StringComparison.OrdinalIgnoreCase);
			string newrp = Path.Combine(RootPath.Substring(0, i), newname);
			try
			{
				Directory.Move(RootPath, newrp);
				RootPath = newrp;
			}
			catch
			{
				return "Error moving the skin folder to the new location!";
			}

			XmlDocument doc = new XmlDocument();
			try
			{
				string docpath = Path.Combine(RootPath, "SkinDefinition.xml");
				doc.Load(docpath);
				XmlElement xe = doc.GetElementsByTagName("SkinName")[0] as XmlElement;
				xe.SetAttribute("Name", newname);
				doc.Save(docpath);
			}
			catch
			{
				return "Error modifying the SkinDefinition.xml file for the skin!";
			}

			Name = newname;
			foreach (var a in Assets)
				a.AssetSource = newname;

			return null;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
