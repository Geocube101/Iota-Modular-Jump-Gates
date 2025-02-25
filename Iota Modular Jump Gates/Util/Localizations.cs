using Sandbox.ModAPI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage;

namespace IOTA.ModularJumpGates.Util
{
	/// <summary>
	/// Class handling text localizations
	/// See SISK's mod localization script: https://github.com/SiskSjet/SE_Mod_Utils/blob/master/Localization/LocalizationComponent.cs
	/// </summary>
	[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
	public class Localizations : MySessionComponentBase
	{
		#region Public Variables
		/// <summary>
		/// The game's current language
		/// </summary>
		public static MyLanguagesEnum? ModLocalizedLanguage { get; private set; }
		#endregion

		#region "MySessionComponentBase" Methods
		public override void LoadData()
		{
			this.LoadLocalization();
			MyAPIGateway.Gui.GuiControlRemoved += this.OnGuiControlRemoved;
		}

		protected override void UnloadData()
		{
			MyAPIGateway.Gui.GuiControlRemoved -= this.OnGuiControlRemoved;
			Localizations.ModLocalizedLanguage = null;
		}
		#endregion

		#region Private Methods
		private void LoadLocalization()
		{
			string path = Path.Combine(this.ModContext.ModPathData, "Localization");
			HashSet<MyLanguagesEnum> supported_langs = new HashSet<MyLanguagesEnum>();
			MyTexts.LoadSupportedLanguages(path, supported_langs);
			MyLanguagesEnum current_lang = supported_langs.Contains(MyAPIGateway.Session.Config.Language) ? MyAPIGateway.Session.Config.Language : MyLanguagesEnum.English;
			if (Localizations.ModLocalizedLanguage != null && Localizations.ModLocalizedLanguage == current_lang) return;
			Localizations.ModLocalizedLanguage = current_lang;
			MyTexts.MyLanguageDescription description = MyTexts.Languages.Where(x => x.Key == current_lang).Select(x => x.Value).FirstOrDefault(); ;

			if (MyTexts.Languages.TryGetValue(current_lang, out description))
			{
				string cultureName = string.IsNullOrWhiteSpace(description.CultureName) ? null : description.CultureName;
				string subcultureName = string.IsNullOrWhiteSpace(description.SubcultureName) ? null : description.SubcultureName;
				MyTexts.LoadTexts(path, cultureName, subcultureName);
			}
		}

		private void OnGuiControlRemoved(object obj)
		{
			if (obj.ToString().EndsWith("ScreenOptionsSpace")) this.LoadLocalization();
		}
		#endregion
	}
}
