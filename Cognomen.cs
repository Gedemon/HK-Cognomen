// the section below is a bit similar to "include" in Lua
// this is a list of namespace from the DLL listed in "references" in the solution explorer from which we're going to use methods.
// I've copied the DLL in the "lib" folder, but we could have referenced some of them (BepInEx, Harmony, Unity) directly in the game's folder
// the system's DLLs are part of windows / visual studio installation and even if they are also in the game's folder, you should keep using the default path
// All the Amplitude's DLL have been "publicized" with a tool named AssemblyPublicizer (https://github.com/CabbageCrow/AssemblyPublicizer) to avoid using reflection, which makes the code more complex

using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Options;
using Amplitude.Mercury.Avatar;
using Amplitude.Mercury.Data.GameOptions;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;
using Amplitude.Mercury.UI;
using BepInEx;
using HarmonyLib;
using HumankindModTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// For now this plugin is organized with multiple classes in one file, but we could put each class in a different file (add -> new item -> new class by right-clicking the project in solution explorer)
// Having the same name space means all your classes can access the other classes without needing to add "using..." at the top

namespace SeelingCat.Cognomen
{
	// The list of possible values for the Empires naming options
	public static class EmpireNamingMode
	{
		public const string
			Empire = "Empire",
			EmpireAvatar = "EmpireAvatar",
			EmpireFullAvatar = "EmpireFullAvatar",
			FullEmpire = "FullEmpire",
			FullEmpireAvatar = "FullEmpireAvatar",
			FullAvatar = "FullAvatar",
			FullBoth = "FullBoth";
	}

	// The possible qualifiers for an empire size
	public enum EmpireSize
	{
		Small,
		Medium,
		Large
	}

	// this is where BepInEx and Harmony are initialized
	// BaseUnityPlugin is a class from BepInEx that also uses classes from UnityEngine.dll and UnityEngine.CoreModule.dll which is why those are in the project's references
	// "awake" is from one of the Unity classes and called when the game is loaded (before the main screen), that's when we ask Harmony to patch all the methods we marked for patching 

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Cognomen : BaseUnityPlugin
    {
		// Setting regions allows to quickly fold/unfold big portions of code to navigate the file
		//
		#region BepInEx/Harmony initialization

		public const string pluginGuid = "seelingcat.humankind.cognomen";
		public const string pluginName = "Cognomen";
		public const string pluginVersion = "1.0.0.0";

		void Awake()
		{
			Harmony harmony = new Harmony(pluginGuid);
			harmony.PatchAll();
		}

		#endregion

		#region Options

		// This section defines the options added in the setup screen
		// it uses (AOM)'s framework (in HumankindModTool folder) to add the options into the DB

		public static readonly GameOptionInfo EmpireNamingOption = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_EmpireNamingMode",
			GroupKey = "GameOptionGroup_LobbyPaceOptions",
			DefaultValue = EmpireNamingMode.FullEmpireAvatar,
			Title = "[Cognomen] Empire naming in generic texts",
			Description = "Set how Empires will be named in various texts (outside the diplomatic screen).",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "Culture + (Avatar)",
					Description = "Add the Avatar name after the Culture name: You're at peace with the Celts (Boudicca)",
					Value = EmpireNamingMode.EmpireAvatar
				},
				new GameOptionStateInfo
				{
					Title = "Culture",
					Description = "Use the game's original naming: You're at peace with the Celts",
					Value = EmpireNamingMode.Empire
				},
				new GameOptionStateInfo
				{
					Title = "Culture Dynamic",
					Description = "Use the same Empire name as in the Diplomatic Screen (dynamic naming based on Civics, Ideologies and Size): You're at peace with the Celtic Kingdom",
					Value = EmpireNamingMode.FullEmpire
				},
				new GameOptionStateInfo
				{
					Title = "Avatar Dynamic",
					Description = "Use the same Avatar name as in the Diplomatic Screen instead of the Culture name (dynamic naming based on Civics, Ideologies and Size): You're at peace with the Queen Boudicca",
					Value = EmpireNamingMode.FullAvatar
				},
				new GameOptionStateInfo
				{
					Title = "Full Dynamic",
					Description = "Show the full dynamic names: You're at peace with the Celtic Kingdom (Queen Boudicca)",
					Value = EmpireNamingMode.FullBoth
				},
				new GameOptionStateInfo
				{
					Title = "Culture + Avatar Dynamic",
					Description = "Use the same Avatar name as in the Diplomatic Screen with the culture adjective (dynamic naming based on Civics, Ideologies and Size): You're at peace with the Celtic Queen Boudicca",
					Value = EmpireNamingMode.EmpireFullAvatar
				},
				new GameOptionStateInfo
				{
					Title = "Culture Dynamic + (Avatar)",
					Description = "Use the same Empire name as in the Diplomatic Screen (dynamic naming based on Civics, Ideologies and Size) with the Avatar short name: You're at peace with the Celtic Kingdom (Boudicca)",
					Value = EmpireNamingMode.FullEmpireAvatar
				},
			}
		};

		static string empireNamingMode => GameOptionHelper.GetGameOption(EmpireNamingOption);

		#endregion

		#region Patched Methods

		// here we ask Harmony to patch the method "GetEmpireName" from the class "GameUtils" in the "Amplitude.Mercury.UI.Helpers" namespace
		// I've not added "Amplitude.Mercury.UI.Helpers" in the using section because i think we'll just use it twice here and adding too many name spaces may slow down intellisense (basically the autocompletion and suggestions for code)
		// about defining the class for patching, see harmony documentation for details : https://harmony.pardeike.net/
		// in short "HarmonyPrefix" means we want our code to run before the original method
		// in that case our method is always "public static bool" and will return "true" to say we want to run the original method after our code or "false" to not run the original
		// "HarmonyPostfix" would mean we want the code to run after the original method
		// if the original method is returning a value (in the case below, a string), we can change that value in prefix or postfix by referencing "__result"
		// we also pass the original instance (of "GameUtils" in that case) to our modified method using "__instance"
		// I'd suggest to decompile all the Amplitude DLLs in a folder (using either HMT by AOM or ILSpy) for referencing and searching methods 
		// Here I'm replacing the original method, so I set the result then return false to not run the original code

		[HarmonyPatch(typeof(Amplitude.Mercury.UI.Helpers.GameUtils))]
		public class GameUtils_Patch
		{
			[HarmonyPatch("GetEmpireName")]
			[HarmonyPrefix]
			public static bool GetEmpireName(Amplitude.Mercury.UI.Helpers.GameUtils __instance, ref string __result, int empireIndex, EmpireColor useColor = EmpireColor.Tertiary, bool isEmpireNameOnTransparentBackground = false, bool isAnonymous = false)
			{
				// As I understand it, snapshot is a cache to get things faster
				// line below is equivalent of "if isAnonymous then empireNameInfo = AnonymousInfo else empireNameInfo = EmpireNamePerIndex"

				ref EmpireNameInfo empireNameInfo = ref isAnonymous ? ref Snapshots.EmpireNameSnapshot.PresentationData.AnonymousInfo : ref Snapshots.EmpireNameSnapshot.PresentationData.EmpireNamePerIndex[empireIndex];

				// We call our new function to name Empire (GetCustomMajorEmpireName, see later in the code) only for Major Empire

				bool isMajorEmpire = __instance.IsMajorEmpire(empireIndex);
				string empireName = isMajorEmpire ? GetLongMajorEmpireName(empireNameInfo, empireIndex) : empireNameInfo.EmpireRoughName;

				// Now that we have the Empire name, code below is an adaptation of the original method to format it depending on the context
				// in which the original method was called

				bool isLocalEmpire = empireIndex == Snapshots.GameSnapshot.PresentationData.LocalEmpireInfo.EmpireIndex;
				bool flag = !__instance.IsMajorEmpire(empireIndex) || __instance.IsMajorEmpireKnownByLocalEmpire(empireIndex);
				string color = empireNameInfo.ColorStringPerEmpireColorIndex[(int)useColor];

				if (isEmpireNameOnTransparentBackground)
				{
					string symbolIcon = empireNameInfo.SymbolIcon;
					string text = ((isLocalEmpire || flag) ? empireName : __instance.DataUtils.GetLocalizedTitle(__instance.FactionUnknown));
					__result = "<c=" + color + ">[SquircleBackground]</c>" + symbolIcon + " " + text;
					return false;
				}

				if (!isAnonymous)
				{
					__result = Amplitude.Mercury.Sandbox.Sandbox.EmpireNamesRepository.GetColorizedName(empireName, color);
					return false;
				}

				__result = Snapshots.EmpireNameSnapshot.PresentationData.GetAnonymousName(useColor);
				return false;

				// below is the original function for reference
				// it's in the GameUtils class in \Amplitude.Mercury.Firstpass\Amplitude\Mercury\UI\Helpers\GameUtils.cs
				// there it can call directly other methods from that class in the code like "IsMajorEmpire"
				// same for properties/fields of the class (see C# documentation)
				/*
				public string GetEmpireName(int empireIndex, EmpireColor useColor = EmpireColor.Tertiary, bool isEmpireNameOnTransparentBackground = false, bool isAnonymous = false)
				{
					if (isEmpireNameOnTransparentBackground)
					{
						ref EmpireNameInfo reference = ref isAnonymous ? ref Snapshots.EmpireNameSnapshot.PresentationData.AnonymousInfo : ref Snapshots.EmpireNameSnapshot.PresentationData.EmpireNamePerIndex[empireIndex];
						bool num = empireIndex == Snapshots.GameSnapshot.PresentationData.LocalEmpireInfo.EmpireIndex;
						bool flag = !IsMajorEmpire(empireIndex) || IsMajorEmpireKnownByLocalEmpire(empireIndex);
						string text = reference.ColorStringPerEmpireColorIndex[(int)useColor];
						string symbolIcon = reference.SymbolIcon;
						string text2 = ((num || flag) ? reference.EmpireRoughName : DataUtils.GetLocalizedTitle(FactionUnknown));
						return "<c=" + text + ">[SquircleBackground]</c>" + symbolIcon + " " + text2;
					}
					if (!isAnonymous)
					{
						return Snapshots.EmpireNameSnapshot.PresentationData.GetEmpireName(empireIndex, useColor);
					}
					return Snapshots.EmpireNameSnapshot.PresentationData.GetAnonymousName(useColor);
				}

				//*/

				// if we wanted to just change a line or two of the original method, here is how it would look for harmony patching
				// as we're not in the original method context, we use the instance to call other methods from the GameUtils class for example "IsMajorEmpire" become "__instance.IsMajorEmpire"
				// most properties/field/methods are "private" and could not be accessed directly like that if we hadn't "publicized" them
				// we'd had to use "reflection" to access those then, which use a different syntax I'm not comfortable with

				/*
				public static bool GetEmpireName(Amplitude.Mercury.UI.Helpers.GameUtils __instance, ref string __result, int empireIndex, EmpireColor useColor = EmpireColor.Tertiary, bool isEmpireNameOnTransparentBackground = false, bool isAnonymous = false)
				{
					if (isEmpireNameOnTransparentBackground)
					{
						ref EmpireNameInfo reference = ref isAnonymous ? ref Snapshots.EmpireNameSnapshot.PresentationData.AnonymousInfo : ref Snapshots.EmpireNameSnapshot.PresentationData.EmpireNamePerIndex[empireIndex];
						bool num = empireIndex == Snapshots.GameSnapshot.PresentationData.LocalEmpireInfo.EmpireIndex;
						bool flag = !__instance.IsMajorEmpire(empireIndex) || __instance.IsMajorEmpireKnownByLocalEmpire(empireIndex);
						string text = reference.ColorStringPerEmpireColorIndex[(int)useColor];
						string symbolIcon = reference.SymbolIcon;
						string text2 = ((num || flag) ? reference.EmpireRoughName : __instance.DataUtils.GetLocalizedTitle(__instance.FactionUnknown));
						_result = "<c=" + text + ">[SquircleBackground]</c>" + symbolIcon + " " + text2;
						return false;
					}
					if (!isAnonymous)
					{
						_result = Snapshots.EmpireNameSnapshot.PresentationData.GetEmpireName(empireIndex, useColor);
						return false;
					}
					_result = Snapshots.EmpireNameSnapshot.PresentationData.GetAnonymousName(useColor);
					return false;
				}
				//*/

			}
		}

		// Patching Refresh method from the EmpireBanner class
		// this is the left banner with the local player's information, and it doesn't call the original GetEmpireName from GameUtils
		// so yes, you may need to dig the code to find where you'll need to use the custom naming function to cover every cases
		// in the testing context of the current version "EmpireName (AvatarName)" it's surely not required to change it *here*, but you'll want it for "real" custom naming
		// using postfix as we want to change a field of the EmpireBanner instance that is set in the original method and don't want to fully replace the method (something we should avoid to reduce mod's maintenance between game's patch) 
		//*
		[HarmonyPatch(typeof(EmpireBanner))]
		public class EmpireBanner_Patch
		{
			[HarmonyPatch("Refresh")]
			[HarmonyPostfix]
			public static void Refresh(EmpireBanner __instance)
			{
				int empireIndex = Snapshots.GameSnapshot.PresentationData.LocalEmpireInfo.EmpireIndex;
				EmpireNameInfo empireNameInfo = Snapshots.EmpireNameSnapshot.PresentationData.EmpireNamePerIndex[empireIndex];
				__instance.cultureTitle.Text = GetFullMajorEmpireName(empireNameInfo, empireIndex);
			}
		}
		//*/

		// Patching RefreshTheirGroup method from the DiplomaticScreen class
		// that's the other player banner in diplomatic relation, as for EmpireBanner it doesn't call the original GetEmpireName from GameUtils
		// same as above, we may not need it here for "EmpireName (AvatarName)" as the Avatar is just below, but you'll want it for "real" custom naming
		// could have different naming functions for different contexts for example
		//*
		[HarmonyPatch(typeof(DiplomaticScreen))]
		public class DiplomaticScreen_Patch
		{
			[HarmonyPatch("RefreshTheirGroup")]
			[HarmonyPostfix]
			public static void RefreshTheirGroup(DiplomaticScreen __instance)
			{
				int otherEmpireIndex = Snapshots.DiplomaticCursorSnapshot.PresentationData.OtherEmpireIndex;
				EmpireNameInfo empireNameInfo = Snapshots.EmpireNameSnapshot.PresentationData.EmpireNamePerIndex[otherEmpireIndex];
				__instance.theirFactionName.Text = GetFullMajorEmpireName(empireNameInfo, otherEmpireIndex);
				__instance.theirName.Text = GetFullAvatarName(otherEmpireIndex);
			}
		}
		//*/

		// Patching InitializeOnStart from the MajorEmpire class
		// 
		//*
		[HarmonyPatch(typeof(MajorEmpire))]
		public class MajorEmpire_Patch
		{

			//*
			[HarmonyPostfix]
			[HarmonyPatch(nameof(InitializeOnStart))]
			public static void InitializeOnStart(MajorEmpire __instance)
			{
				SetCustomEmpireNames(__instance.Index);
			}
		}
		//*/

		// Patching methods from the EmpireNamesRepository class
		// 
		//*
		[HarmonyPatch(typeof(EmpireNamesRepository))]
		public class EmpireNamesRepository_Patch
		{
			// We're initializing the arrays that will be used to cache the custom Empire names here 
			// (EmpireNamesRepository is the class that does that for the normal names)
			// but we do not try to set the custom names yet, as at this point in loading some methods we'll need are not initialized yet
			//
			[HarmonyPatch("InitializeOnLoad")]
			[HarmonyPostfix]
			public static void InitializeOnLoad(EmpireNamesRepository __instance)
			{
				int numberOfEmpires = Amplitude.Mercury.Sandbox.Sandbox.NumberOfEmpires;
				FullEmpireNamePerEmpireIndex = new string[numberOfEmpires];
				LongEmpireNamePerEmpireIndex = new string[numberOfEmpires];
				FullAvatarNamePerEmpireIndex = new string[numberOfEmpires];

			}

			// After the game's update an Empire name, we also try to update the custom names
			//
			[HarmonyPatch("SetEmpireName")]
			[HarmonyPostfix]
			public static void SetEmpireName(EmpireNamesRepository __instance, ref EmpireNameInfo empireNameInfo, Empire empire, bool forceUpdate = false)
			{
				SetCustomEmpireNames(empireNameInfo, empire.Index);
			}
			//
		}
		//*/

		// Patching Load from the OptionsManager<GameOptionDefinition> class
		// That's how (AOM)'s framework add the custom options into the DB before it's used
		// 
		//*
		[HarmonyPatch(typeof(OptionsManager<GameOptionDefinition>))]
		public class GameOptions_Patch
		{
			[HarmonyPatch("Load")]
			[HarmonyPrefix]
			public static bool Load(OptionsManager<GameOptionDefinition> __instance)
			{
				GameOptionHelper.Initialize(EmpireNamingOption);
				return true;
			}
		}
		//*/

		// Patching Events to update names

		// Update name from civics after CivicChoiceChanged
		//*
		[HarmonyPatch(typeof(CultureManager))]
		public class CultureManager_Patch
		{
			[HarmonyPatch("SimulationEventRaised_CivicChoiceChanged")]
			[HarmonyPostfix]
			public static void SimulationEventRaised_CivicChoiceChanged(CultureManager __instance, object sender, SimulationEvent_CivicChoiceChanged e)
			{
				SetCustomEmpireNames(e.EmpireIndex);
			}
		}
		//*/

		// Update name from ideologies after NarrativeEventChoice
		//*
		[HarmonyPatch(typeof(Amplitude.Mercury.Analytics.AnalyticsEvent_NarrativeEventChoice))]
		public class AnalyticsEvent_NarrativeEventChoice_Patch
		{
			[HarmonyPatch("SimulationEventRaised_NarrativeEventChoice")]
			[HarmonyPostfix]
			public static void SimulationEventRaised_NarrativeEventChoice(Amplitude.Mercury.Analytics.AnalyticsEvent_NarrativeEventChoice __instance, object entity, SimulationEvent_NarrativeEventChoice simulationEvent)
			{
				SetCustomEmpireNames(simulationEvent.NarrativeEvent.MajorEmpire.Index);
			}
		}
		//*/

		// Update name from Empire size after TerritoryOwnerChanged
		//*
		[HarmonyPatch(typeof(ControlAreaManager))]
		public class ControlAreaManager_Patch
		{
			[HarmonyPatch("SimulationEventRaised_TerritoryOwnerChanged")]
			[HarmonyPostfix]
			public static void SimulationEventRaised_TerritoryOwnerChanged(ControlAreaManager __instance, object sender, SimulationEvent_TerritoryOwnerChanged e)
			{
				SetCustomEmpireNames(e.OldOwnerEmpireIndex);
				SetCustomEmpireNames(e.NewOwnerEmpireIndex);
			}
		}
		//*/

		#endregion

		#region Custom Naming

		// We're going to cache the names in those 3 arrays, to make things faster
		// as GetEmpireName in GameUtils is called a lot of times

		public static string[] FullEmpireNamePerEmpireIndex;
		public static string[] LongEmpireNamePerEmpireIndex;
		public static string[] FullAvatarNamePerEmpireIndex;

		// this class will be able to generate the names after we've given it the data on an Empire Governement/Politic/Size
		public class EmpireGovernement
        {
			// Civics
			public bool NaturalRight = false;
			public bool DivineMandate = false;
			public bool SmallCouncil = false;
			public bool Autarchy = false;
			public bool Republic = false;
			public bool Aristocracy = false;
			public bool Monarchy = false;
			public bool AbsoluteMonarchy = false;
			public bool ConstitutionalMonarchy = false;
			public bool OnePartyState = false;
			public bool Oligarchy = false;
			public bool DemocraticRepublic = false;

			// Axis (level value)
			public int Individualism;
			public int Collectivism;
			public int Authority;
			public int Liberty;

			// minimal axis level value to qualify a State
			int minAxisLevel = 6;

			//
			public string AvatarFullName;
			public string EmpireFullName;

			public bool IsSocialist()
            {
				return (Liberty >= minAxisLevel && Collectivism >= minAxisLevel);
            }
			public bool IsCommunist()
			{
				return (Authority >= minAxisLevel && Collectivism >= minAxisLevel);
			}
			public bool IsRepublican()
			{
				return (Liberty >= minAxisLevel && Individualism >= minAxisLevel);
			}
			public bool IsFascist()
			{
				return (Authority >= minAxisLevel && Collectivism >= minAxisLevel);
			}

			public void GenerateNames(string empireAdjective, EmpireSize empireSize, string avatarName, Gender avatarGender)
            {
				string title = string.Empty;
				bool isMale = avatarGender == Gender.Male;

				// Processing from higher level to lower

				// Early Modern and later Eras
				if (Aristocracy && DemocraticRepublic)
                {
					title = "Prime Minister";

                    if(IsSocialist())
					{
						EmpireFullName = empireAdjective + " Democratic Socialist Republic";
					}
                    else if (IsCommunist())
					{
						EmpireFullName = empireAdjective + " People's Democratic Republic";
					}
					else if (IsFascist())
					{
						EmpireFullName = empireAdjective + " Union";
					}
					else
					{
						EmpireFullName = empireAdjective + " Republic";
					}
				}
                else if (Republic && DemocraticRepublic)
				{
					title = "President";

					if (IsSocialist())
					{
						EmpireFullName = empireAdjective + " Democratic Socialist Republic";
					}
					else if (IsCommunist())
					{
						EmpireFullName = empireAdjective + " People's Democratic Republic";
					}
					else if (IsFascist())
					{
						EmpireFullName = empireAdjective + " Union";
					}
					else
					{
						EmpireFullName = empireAdjective + " Republic";
					}
				}
				else if (OnePartyState)
				{
					title = "Chairman";

					if (IsSocialist())
					{
						EmpireFullName = empireAdjective + " Socialist Republic";
					}
					else if (IsCommunist())
					{
						EmpireFullName = empireAdjective + " People's Republic";
					}
					else if (IsFascist())
					{
						EmpireFullName = empireAdjective + " State";
					}
					else
					{
						EmpireFullName = empireAdjective + " Republic";
					}
				}
				else if (Oligarchy)
				{
					title = isMale ? "Prince" : "Princess";

					if (IsSocialist())
					{
						EmpireFullName = empireAdjective + " Socialist Commonwealth";
					}
					else if (IsCommunist())
					{
						EmpireFullName = empireAdjective + " People's Commonwealth";
					}
					else if (IsFascist())
					{
						EmpireFullName = empireAdjective + " Junta";
					}
					else
					{
						EmpireFullName = empireAdjective + " Commonwealth";
					}
				}
				else if (AbsoluteMonarchy || ConstitutionalMonarchy)
				{
					title = isMale ? "King" : "Queen"; 
					EmpireFullName = empireAdjective + " Kingdom";
				}

				// Classical & Medieval
				else if (Republic)
				{
					switch(empireSize)
                    {
						case EmpireSize.Large:
                            {
								title = isMale ? "Most Serene Prince" : "Most Serene Princess";
								EmpireFullName = "Most Serene " + empireAdjective + " Republic";
								break;
							}
						case EmpireSize.Medium:
							{
								title = isMale ? "Serene Prince" : "Serene Princess";
								EmpireFullName = "Serene " + empireAdjective + " Republic";
								break;
							}
						case EmpireSize.Small:
							{
								title = isMale ? "Prince" : "Princess";
								EmpireFullName = empireAdjective + " Republic";
								break;
							}
					}

				}
				else if (Aristocracy)
				{
					switch (empireSize)
					{
						case EmpireSize.Large:
							{
								title = isMale ? "King" : "Queen";
								EmpireFullName = empireAdjective + " Commonwealth";
								break;
							}
						case EmpireSize.Medium:
							{
								title = isMale ? "Grand Prince" : "Grand Princess";
								EmpireFullName = "Grand " + empireAdjective + " principality";
								break;
							}
						case EmpireSize.Small:
							{
								title = isMale ? "Prince" : "Princess";
								EmpireFullName = empireAdjective + " Principality";
								break;
							}
					}
				}
				else if (Monarchy)
				{
					switch (empireSize)
					{
						case EmpireSize.Large:
							{
								title = isMale ? "King" : "Queen";
								EmpireFullName = empireAdjective + " Kingdom";
								break;
							}
						case EmpireSize.Medium:
							{
								title = isMale ? "Duc" : "Duchess";
								EmpireFullName = empireAdjective + " Duchy";
								break;
							}
						case EmpireSize.Small:
							{
								title = isMale ? "Count" : "Countess";
								EmpireFullName = empireAdjective + " County";
								break;
							}
					}
				}

				// Earlier Eras
				else if (Autarchy && DivineMandate)
				{
					title = isMale ? "God-King" : "God-Queen";
					EmpireFullName = empireAdjective + " Dynasty";
				}
				else if (Autarchy && NaturalRight)
				{
					title = isMale ? "King" : "Queen";
					EmpireFullName = empireAdjective + " Empire";
				}
				else if (SmallCouncil && DivineMandate)
				{
					title = "Judge";
					EmpireFullName = empireAdjective + " Kritarchy";
				}
				else if (SmallCouncil && NaturalRight)
				{
					title = "Senator";
					EmpireFullName = empireAdjective + " Republic";
				}
				else if (DivineMandate)
				{
					if(empireSize == EmpireSize.Large)
					{
						title = isMale ? "God-Emperor" : "God-Empress";
						EmpireFullName = empireAdjective + " Empire";
					}
					else
                    {
						title = isMale ? "God-King" : "God-Queen";
						EmpireFullName = empireAdjective + " Theocracy";
					}
				}
				else if (NaturalRight)
				{
					if (empireSize == EmpireSize.Large)
					{
						title = isMale ? "Patriarch" : "Matriarch";
						EmpireFullName = empireAdjective + " Civilization";
					}
					else
					{
						title = isMale ? "Elder" : "Eldess";
						EmpireFullName = empireAdjective + " Culture";
					}
				}
				else
				{
					if (empireSize == EmpireSize.Large)
					{
						title = isMale ? "High Chief" : "High Chiefess";
						EmpireFullName = empireAdjective + " Chiefdom";
					}
					else
					{
						title = isMale ? "Chief" : "Chiefess";
						EmpireFullName = empireAdjective + " Tribe";
					}
				}

				AvatarFullName = title != string.Empty ? title + " " + avatarName : avatarName;
            }

		}

		// Multiple instances of this class help to quickly get an Empire size from territory sizes by Era
		public class TerritorySize
        {
			int Medium;
			int Large;

			public TerritorySize(int medium, int large)
            {
				Medium = medium;
				Large = large;
			}

			public EmpireSize GetSize(int numTerritories)
            {
				if(numTerritories >= Large)
                {
					return EmpireSize.Large;
				}
				else if(numTerritories >= Medium)
				{
					return EmpireSize.Medium;
                }
				return EmpireSize.Small;
            }
		}

		// List of size from territories per Era index
		// Values may need tweaking, first value is the minimal for "medium" any value below is considered "small", second value is the minimal for large
		static IDictionary<int, TerritorySize> TerritorySizeByEraIndex = new Dictionary<int, TerritorySize> 
			{
				{0, new TerritorySize(0,2) },	// Neolithic 
				{1, new TerritorySize(3,6) },	// Ancient
				{2, new TerritorySize(3,9) },	// Classical
				{3, new TerritorySize(6,12) },	// Medieval
				{4, new TerritorySize(7,15) },	// Early Modern
				{5, new TerritorySize(8,18) },	// Industrial
				{6, new TerritorySize(9,21) },	// Contemporary
			};


		// And here is the main function that prepares the data we'll give to our "EmpireGovernement" class that will compute the custom names
		//
		public static void SetCustomEmpireNames(EmpireNameInfo empireNameInfo, int empireIndex)
		{
			string empireName = empireNameInfo.EmpireRoughName;
			string fullEmpireName = empireName;
			string longEmpireName = empireName;
			string fullAvatarName = string.Empty;

			if(empireIndex < Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires)
			{
				MajorEmpire majorEmpire = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires[empireIndex];

				if (majorEmpire != null)
				{
					// the good old "print" from Lua is not gone !
					// here is an example of using Amplitude's logging to the html files in the "\Documents\Humankind\Temporary Files" folder
					// I'm using it a lot to explore the content of the various object and what I can use.

					Diagnostics.LogError($"[SeelingCat] Custom naming for {empireName} (index = {empireIndex})"); // LogError text are Red, I use them to ID a section of my logs when quickly scrolling the (long) diagnostic file

					// creating a new instance of the EmpireGovernement class to input civics and Axis data
					EmpireGovernement gov = new EmpireGovernement();

					// iterating all civics to get governement choices
					// 
					int numCivics = majorEmpire.DepartmentOfDevelopment.Civics.Length;
					for (int c = 0; c < numCivics; c++)
					{
						Civic civic = majorEmpire.DepartmentOfDevelopment.Civics[c];
                        if(civic.CivicStatus == CivicStatuses.Enacted)
						{
							Diagnostics.LogWarning($"[SeelingCat] - Civic {civic.CivicDefinition.Name}, ActiveChoiceName = {civic.ActiveChoiceName}");
							switch (civic.ActiveChoiceName.ToString())
                            {
								// Founding Myths
								case "Civics_Government01_Choice01": // Natural Right
									{
										gov.NaturalRight = true;
										break;
									}
								case "Civics_Government01_Choice02": // Divine Mandate
									{
										gov.DivineMandate = true;
										break;
									}
								// Leadership
								case "Civics_Government02_Choice01": // Small Council
									{
										gov.SmallCouncil = true;
										break;
									}
								case "Civics_Government02_Choice02": // Autarchy
									{
										gov.Autarchy = true;
										break;
									}
								//Political Entitlement
								case "Civics_Government03_Choice01": // Aristocracy
									{
										gov.Aristocracy = true;
										break;
									}
								case "Civics_Government03_Choice02": // Republic
									{
										gov.Republic = true;
										break;
									}
								// Political Influence
								case "Civics_Government04_Choice01": // Monarchy
									{
										gov.Monarchy = true;
										break;
									}
								case "Civics_Government04_Choice02": // Aristocracy
									{
										gov.Aristocracy = true;
										break;
									}
								// Monarchy Power
								case "Civics_Government05_Choice01": // Absolute Monarchy
									{
										gov.AbsoluteMonarchy = true;
										break;
									}
								case "Civics_Government05_Choice02": // Constitutional Monarchy
									{
										gov.ConstitutionalMonarchy = true;
										break;
									}
								// Republic Evolution
								case "Civics_Government06_Choice01": // One-Party State
									{
										gov.OnePartyState = true;
										break;
									}
								case "Civics_Government06_Choice02": // Democratic Republic
									{
										gov.DemocraticRepublic = true;
										break;
									}
								// Aristocracy Evolution
								case "Civics_Government07_Choice01": // Oligarchy
									{
										gov.Oligarchy = true;
										break;
									}
								case "Civics_Government07_Choice02": // Democratic Republic
									{
										gov.DemocraticRepublic = true;
										break;
									}
							}
						}
					}

					int numAxis = majorEmpire.DepartmentOfDevelopment.IdeologicalAxes.Length;
					for (int a = 0; a < numAxis; a++)
                    {
						IdeologicalAxis axis = majorEmpire.DepartmentOfDevelopment.IdeologicalAxes[a];
						Diagnostics.LogWarning($"[SeelingCat] - Axis {axis.AxisName}, Value = {axis.Value}, BaseValue = {axis.BaseValue}, SectionIndex = {axis.SectionIndex},  Orientation = {axis.OrientationName}, OrientationIndex = {axis.OrientationIndex}");

						int level = System.Math.Abs(axis.Value); 

						switch(axis.OrientationName.ToString())
						{
							// Economic
							case "Liberalism": // Individualism
								{
									gov.Individualism = level;
									break;
								}
							case "Regulationism":
								{
									break;
								}
							case "Collectivism": // Collectivism
								{
									gov.Collectivism = level;
									break;
								}
							// Geopolitic
							case "Internationalism":
								{
									break;
								}
							case "Sovereignism":
								{
									break;
								}
							case "Nationalism":
								{
									break;
								}
							// Order
							case "Authoritarianism": // Authority
								{
									gov.Authority = level;
									break;
								}
							case "Republican":
								{
									break;
								}
							case "Anarchist": // Liberty
								{
									gov.Liberty = level;
									break;
								}
							// Social
							case "Progressive":
								{
									break;
								}
							case "Moderate":
								{
									break;
								}
							case "Conservative":
								{
									break;
								}
						}
					}

					// Quantify the Empire Size
					//
					EmpireSize empireSize = EmpireSize.Small;
					FixedPoint numTerritories = majorEmpire.TerritoryCount.Value;
					int empireEraIndex = majorEmpire.DepartmentOfDevelopment.CurrentEraIndex;

					Diagnostics.LogWarning($"[SeelingCat] Quantify Empire Size (numTerritories = {numTerritories}, eraIndex = {empireEraIndex})");

					if (TerritorySizeByEraIndex.TryGetValue(empireEraIndex, out TerritorySize territorySize)) // TryGetValue is an useful method of dictionaries, check if a key exist and return the corresponding value in an out parameter
					{
						// here the out value is the TerritorySize class corresponding to the Empire's current Era
						// that class return a quantifier (small, medium, large) based on a number (of territories)
						empireSize = territorySize.GetSize((int)numTerritories);
					}

					// Trying to get the Avatar gender...
					IAvatarService avatarService = Services.GetService<IAvatarService>();
					avatarService.TryGetGender(majorEmpire.AvatarSummary, out Gender gender);

					// Preparing to get the Avatar title
					//
					Diagnostics.LogWarning($"[SeelingCat] preparing {gender} Avatar full name...");

					// We get the user name for humans or the avatar name for AI
					string personaName = (majorEmpire.IsControlledByHuman ? majorEmpire.PlayerIdentifier.UserName : majorEmpire.PersonaName);

					// Preparing the Empire Full and long names
					//
					Diagnostics.LogWarning($"[SeelingCat] preparing the Empire Full and long names...");

					// The UIMapper has the value from the DB, including the adjective not used in the base game 
					FactionUIMapper empireUIMapper = Utils.DataUtils.GetUIMapper<FactionUIMapper>(majorEmpire.FactionDefinition.Name);

					string empirePrefix = string.Empty;
					string empirePostfix = string.Empty;
					string empireAdjective = Utils.TextUtils.Localize(empireUIMapper.Adjective);

					// We've filled the EmpireGovernement class with data earlier, we now use it to generate the names
					//
					Diagnostics.LogWarning($"[SeelingCat] generating names for empireAdjective = {empireAdjective}, empireSize = {empireSize}, personaName = {personaName}, gender = {gender}");
					gov.GenerateNames(empireAdjective, empireSize, personaName, gender);

					// Assigning the generated names
					fullAvatarName = gov.AvatarFullName;
					fullEmpireName = gov.EmpireFullName;

					// Override the Empire name in case of vassalization
					//
					if (majorEmpire.Liege.Entity != null)
                    {
						MajorEmpire liegeEmpire = majorEmpire.Liege.Entity;
						FactionUIMapper liegeUIMapper = Utils.DataUtils.GetUIMapper<FactionUIMapper>(liegeEmpire.FactionDefinition.Name);

						Diagnostics.LogWarning($"[SeelingCat] - Is vassal of {liegeEmpire.FactionDefinition.Name}, Era = {liegeEmpire.DepartmentOfDevelopment.CurrentEraIndex}");

						switch (liegeEmpire.DepartmentOfDevelopment.CurrentEraIndex)
						{
							case 1: // Ancient
								{
									empirePrefix = "Subjugated ";
									empirePostfix = " State";
									break;
								}
							case 2: // Classical
								{
									empirePrefix = "Tributary ";
									empirePostfix = " State";
									break;
								}
							case 3: // Medieval
								{
									empirePrefix = "Feudatory ";
									empirePostfix = " State";
									break;
								}
							case 4: // Early Modern
								{
									empirePostfix = " Viceroyalty";
									break;
								}
							case 5: // Industrial
								{
									empirePrefix = Utils.TextUtils.Localize(liegeUIMapper.Adjective);
									empirePostfix = " Protectorate";
									break;
								}
							case 6: // Contemporary
								{
									empirePrefix = Utils.TextUtils.Localize(liegeUIMapper.Adjective);
									empirePostfix = " State";
									break;
								}
							default: // In case of modded eras...
								{
									empirePrefix = "Vassal ";
									empirePostfix = " State";
									break;
								}
						}
						fullEmpireName = empirePrefix + empireAdjective + empirePostfix; // Utils.TextUtils.Localize("%FullCustomEmpireNameTitle", empirePrefix, empireName, empirePostfix)
					}


					// Set the longEmpireName based on the selected option
					//
					switch (empireNamingMode)
                    {
						case EmpireNamingMode.FullEmpire:
							{
								longEmpireName = fullEmpireName;
								break;
							}
						case EmpireNamingMode.FullAvatar:
							{
								longEmpireName = fullAvatarName;
								break;
							}
						case EmpireNamingMode.FullBoth:
							{
								longEmpireName = fullEmpireName + " (" + fullAvatarName + ")";
								break;
							}
						case EmpireNamingMode.EmpireAvatar:
							{
								longEmpireName += " (" + personaName + ")";
								break;
							}
						case EmpireNamingMode.EmpireFullAvatar:
							{
								longEmpireName = empireAdjective + " " + fullAvatarName;
								break;
							}
						case EmpireNamingMode.FullEmpireAvatar:
							{
								longEmpireName = fullEmpireName + " (" + personaName + ")";
								break;
							}
						default:
                            {
								longEmpireName = empireName;
								break;
                            }
					}

					Diagnostics.LogWarning($"[SeelingCat] fullEmpireName = {fullEmpireName}, longEmpireName = {longEmpireName}, fullAvatarName = {fullAvatarName}");
				}
			}
            else
            {
				// here we could generate names for minor factions
            }

			if(empireIndex < FullEmpireNamePerEmpireIndex.Length)
            {
				FullEmpireNamePerEmpireIndex[empireIndex] = fullEmpireName;
				LongEmpireNamePerEmpireIndex[empireIndex] = longEmpireName;
				FullAvatarNamePerEmpireIndex[empireIndex] = fullAvatarName;
			}
            else
            {
				Diagnostics.LogError($"[SeelingCat] Index out of bound for Custom naming of {empireName} (index = {empireIndex}, FullEmpireNamePerEmpireIndex.Length = {FullEmpireNamePerEmpireIndex.Length})");
			}
		}

		// We can have methods with the same name but a different signature (the number/type of arguments)
		// This one is called from part of the code where "empireNameInfo" is not already known, and where the empire index may not be valid
		public static void SetCustomEmpireNames(int empireIndex)
		{
			if(empireIndex < 0)
            {
				return;
            }
			EmpireNameInfo empireNameInfo = Snapshots.EmpireNameSnapshot.PresentationData.EmpireNamePerIndex[empireIndex];
			SetCustomEmpireNames(empireNameInfo, empireIndex);
		}

		// Finally the methods below get the names from the arrays we've cached them on
		// 
		public static string GetLongMajorEmpireName(EmpireNameInfo empireNameInfo, int empireIndex)
		{
			string empireName = empireNameInfo.EmpireRoughName;

			if (empireIndex < LongEmpireNamePerEmpireIndex.Length)
			{
				empireName = LongEmpireNamePerEmpireIndex[empireIndex];
			}

			return empireName;
		}
		public static string GetLongMajorEmpireName(int empireIndex)
		{
			EmpireNameInfo empireNameInfo = Snapshots.EmpireNameSnapshot.PresentationData.EmpireNamePerIndex[empireIndex];
			return GetLongMajorEmpireName(empireNameInfo, empireIndex);
		}
		public static string GetFullMajorEmpireName(EmpireNameInfo empireNameInfo, int empireIndex)
		{
			string empireName = empireNameInfo.EmpireRoughName;

			if (empireIndex < FullEmpireNamePerEmpireIndex.Length)
			{
				empireName = FullEmpireNamePerEmpireIndex[empireIndex];
			}

			return empireName;
		}
		public static string GetFullAvatarName(int empireIndex)
		{
			string avatarName = string.Empty;

			if (empireIndex < FullAvatarNamePerEmpireIndex.Length)
			{
				avatarName = FullAvatarNamePerEmpireIndex[empireIndex];
			}

			return avatarName;
		}

		#endregion
	}
}
