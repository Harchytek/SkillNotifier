using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

namespace SkillNotifier
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SkillNotifierPlugin : BaseUnityPlugin
    {
        public const string ModGUID = "Harchytek.SkillNotifier";
        public const string ModName = "SkillNotifier";
        public const string ModVersion = "1.3.0";

        public static ConfigEntry<bool> ShowXPNotifications;
        public static ConfigEntry<int> NotificationTextSizeXP;
        public static ConfigEntry<bool> SkipRunningSkillNotifications;

        public static ConfigEntry<bool> ShowSkillLevel;
        public static ConfigEntry<bool> ShowGainPourcent;
        public static ConfigEntry<bool> ShowGainXP;
        public static ConfigEntry<bool> ShowExtendedXP;
        public static ConfigEntry<bool> ShowCharacterXP;

        public static ConfigEntry<string> TextSkillLevel;
        public static ConfigEntry<bool> AutoSpaceSkillLevel; 
        public static ConfigEntry<string> TextTotalLevel;
        public static ConfigEntry<bool> AutoSpaceTotalLevel; 
        public static ConfigEntry<string> TextFallbackMessage;

        public enum LevelPos { BeforeSkill, AfterSkill, BeforeTotal }
        public static ConfigEntry<LevelPos> SkillLevelPosition;
        public enum TotalPos { Start, End } 
        public static ConfigEntry<TotalPos> CharacterXPPosition; 
        public enum GainXpPos { BeforeExtended, AfterExtended } 
        public static ConfigEntry<GainXpPos> GainXPPosition; 

        public static ConfigEntry<string> SkillSymbol;
        public static ConfigEntry<string> SymbolsLevel;    
        public static ConfigEntry<string> SymbolsPercent; 
        public static ConfigEntry<string> SymbolsGain;    
        public static ConfigEntry<string> SymbolsExtended; 
        public static ConfigEntry<string> SymbolsTotal;    

        public static ConfigEntry<Color> ColorSkillName;
        public static ConfigEntry<Color> ColorSkillLevel;
        public static ConfigEntry<Color> ColorPercentage;
        public static ConfigEntry<Color> ColorXPGain;
        public static ConfigEntry<Color> ColorExtendedXP;
        public static ConfigEntry<Color> ColorTotalLevel;

        public static string _hexSkillName;
        public static string _hexSkillLevel;
        public static string _hexPercentage;
        public static string _hexXPGain;
        public static string _hexExtendedXP;
        public static string _hexTotalLevel;

        public static float _cachedTotalLevel = -1f;

        void Awake()
        {
            // --- SECTION 1 : GENERAL ---
            ShowXPNotifications = Config.Bind("1 - General", "ShowXPNotifications", true, 
                new ConfigDescription("Enable or disable XP notifications.", null, new ConfigurationManagerAttributes { Order = 3 }));
            
            SkipRunningSkillNotifications = Config.Bind("1 - General", "SkipRunningSkillNotifications", true, 
                new ConfigDescription("Ignore notifications for the running skill.", null, new ConfigurationManagerAttributes { Order = 2 }));
            
            NotificationTextSizeXP = Config.Bind("1 - General", "NotificationTextSizeXP", 18, 
                new ConfigDescription("Text size of the notifications.", null, new ConfigurationManagerAttributes { Order = 1 }));

            // --- SECTION 2 : VISIBILITY ---
            ShowSkillLevel = Config.Bind("2 - Visibility", "ShowSkillLevel", true, 
                new ConfigDescription("Show the current skill level.", null, new ConfigurationManagerAttributes { Order = 5 }));
            
            ShowGainPourcent = Config.Bind("2 - Visibility", "ShowGainPourcent", true, 
                new ConfigDescription("Show the progress percentage.", null, new ConfigurationManagerAttributes { Order = 4 }));
            
            ShowGainXP = Config.Bind("2 - Visibility", "ShowGainXP", true, 
                new ConfigDescription("Show the actual XP gained.", null, new ConfigurationManagerAttributes { Order = 3 }));
            
            ShowExtendedXP = Config.Bind("2 - Visibility", "ShowExtendedXP", true, 
                new ConfigDescription("Show the extended XP ratio [Current/Needed].", null, new ConfigurationManagerAttributes { Order = 2 }));
            
            ShowCharacterXP = Config.Bind("2 - Visibility", "ShowCharacterXP", true, 
                new ConfigDescription("Show the total character level.", null, new ConfigurationManagerAttributes { Order = 1 }));

            // --- SECTION 3 : TEXTS ---
            TextSkillLevel = Config.Bind("3 - Text", "TextSkillLevel", "Level", 
                new ConfigDescription("Text displayed before the skill level.", null, new ConfigurationManagerAttributes { Order = 5 }));
            
            AutoSpaceSkillLevel = Config.Bind("3 - Text", "AutoSpaceSkillLevel", true, 
                new ConfigDescription("Automatically add a space after the skill level text.", null, new ConfigurationManagerAttributes { Order = 4 }));
            
            TextTotalLevel = Config.Bind("3 - Text", "TextTotalLevel", "Total:", 
                new ConfigDescription("Text displayed before the total level.", null, new ConfigurationManagerAttributes { Order = 3 }));
            
            AutoSpaceTotalLevel = Config.Bind("3 - Text", "AutoSpaceTotalLevel", true, 
                new ConfigDescription("Automatically add a space after the total level text.", null, new ConfigurationManagerAttributes { Order = 2 }));
            
            TextFallbackMessage = Config.Bind("3 - Text", "TextFallbackMessage", "you win something, but what?", 
                new ConfigDescription("Message displayed if all XP stats are hidden. Leave empty to disable.", null, new ConfigurationManagerAttributes { Order = 1 }));

            // --- SECTION 4 : POSITION ---
            SkillLevelPosition = Config.Bind("4 - Position", "SkillLevelPosition", LevelPos.AfterSkill, 
                new ConfigDescription("Position of the level: Before Skill Name, After Skill Name, or Before Total Level.", null, new ConfigurationManagerAttributes { Order = 3 }));
            
            GainXPPosition = Config.Bind("4 - Position", "GainXPPosition", GainXpPos.BeforeExtended, 
                new ConfigDescription("Position of the gained XP: Before or After the Extended XP.", null, new ConfigurationManagerAttributes { Order = 2 })); 
            
            CharacterXPPosition = Config.Bind("4 - Position", "CharacterXPPosition", TotalPos.End, 
                new ConfigDescription("Position of the total level: At the start or at the end of the message.", null, new ConfigurationManagerAttributes { Order = 1 }));

            // --- SECTION 5 : SYMBOLS ---
            SkillSymbol = Config.Bind("5 - Symbols", "SkillSymbol", "", 
                new ConfigDescription("Symbol prefix before the skill name (e.g., ●, ◆, ->).", null, new ConfigurationManagerAttributes { Order = 6 }));
            
            SymbolsLevel = Config.Bind("5 - Symbols", "SymbolsLevel", "[]", 
                new ConfigDescription("Symbols wrapping the skill level (e.g., [], (), {}).", null, new ConfigurationManagerAttributes { Order = 5 }));
            
            SymbolsPercent = Config.Bind("5 - Symbols", "SymbolsPercent", "", 
                new ConfigDescription("Symbols wrapping the percentage (e.g., [], (), {}).", null, new ConfigurationManagerAttributes { Order = 4 }));
            
            SymbolsGain = Config.Bind("5 - Symbols", "SymbolsGain", "()", 
                new ConfigDescription("Symbols wrapping the XP gain (e.g., [], (), {}).", null, new ConfigurationManagerAttributes { Order = 3 }));
            
            SymbolsExtended = Config.Bind("5 - Symbols", "SymbolsExtended", "()", 
                new ConfigDescription("Symbols wrapping the Extended XP (e.g., [], (), {}).", null, new ConfigurationManagerAttributes { Order = 2 }));
            
            SymbolsTotal = Config.Bind("5 - Symbols", "SymbolsTotal", "[]", 
                new ConfigDescription("Symbols wrapping the Total character level (e.g., [], (), {}).", null, new ConfigurationManagerAttributes { Order = 1 }));

            // --- SECTION 6 : COLORS ---
            ColorSkillName = Config.Bind("6 - Colors", "ColorSkillName", Color.white, 
                new ConfigDescription("Color of the skill name.", null, new ConfigurationManagerAttributes { Order = 6 }));
            
            ColorSkillLevel = Config.Bind("6 - Colors", "ColorSkillLevel", new Color(1f, 0.843f, 0f), 
                new ConfigDescription("Color of the skill level.", null, new ConfigurationManagerAttributes { Order = 5 }));
            
            ColorPercentage = Config.Bind("6 - Colors", "ColorPercentage", new Color(0.678f, 0.847f, 0.902f), 
                new ConfigDescription("Color of the progress percentage.", null, new ConfigurationManagerAttributes { Order = 4 }));
            
            ColorXPGain = Config.Bind("6 - Colors", "ColorXPGain", Color.green, 
                new ConfigDescription("Color of the XP gain.", null, new ConfigurationManagerAttributes { Order = 3 }));
            
            ColorExtendedXP = Config.Bind("6 - Colors", "ColorExtendedXP", new Color(1f, 0.843f, 0f), 
                new ConfigDescription("Color of the extended XP ratio [Current/Needed].", null, new ConfigurationManagerAttributes { Order = 2 }));
            
            ColorTotalLevel = Config.Bind("6 - Colors", "ColorTotalLevel", new Color(1f, 0.647f, 0f), 
                new ConfigDescription("Color of the total character level.", null, new ConfigurationManagerAttributes { Order = 1 }));

            UpdateHexColors();
            ColorSkillName.SettingChanged += (s, e) => UpdateHexColors();
            ColorSkillLevel.SettingChanged += (s, e) => UpdateHexColors();
            ColorPercentage.SettingChanged += (s, e) => UpdateHexColors();
            ColorXPGain.SettingChanged += (s, e) => UpdateHexColors();
            ColorExtendedXP.SettingChanged += (s, e) => UpdateHexColors();
            ColorTotalLevel.SettingChanged += (s, e) => UpdateHexColors();

            new Harmony(ModGUID).PatchAll();
        }

        private void UpdateHexColors()
        {
            _hexSkillName = "#" + ColorUtility.ToHtmlStringRGB(ColorSkillName.Value);
            _hexSkillLevel = "#" + ColorUtility.ToHtmlStringRGB(ColorSkillLevel.Value);
            _hexPercentage = "#" + ColorUtility.ToHtmlStringRGB(ColorPercentage.Value);
            _hexXPGain = "#" + ColorUtility.ToHtmlStringRGB(ColorXPGain.Value);
            _hexExtendedXP = "#" + ColorUtility.ToHtmlStringRGB(ColorExtendedXP.Value);
            _hexTotalLevel = "#" + ColorUtility.ToHtmlStringRGB(ColorTotalLevel.Value);
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        public static class Patch_RaiseSkill
        {
            private static readonly AccessTools.FieldRef<Skills, Dictionary<Skills.SkillType, Skills.Skill>> SkillDataRef = 
                AccessTools.FieldRefAccess<Skills, Dictionary<Skills.SkillType, Skills.Skill>>("m_skillData");

            public struct SkillState
            {
                public Skills.Skill Skill;
                public float LevelBefore;
                public float CalculatedXpBefore;
                public float NeededXpBefore;
            }

            private static readonly StringBuilder _mainBuilder = new StringBuilder(512);
            private static readonly StringBuilder _levelBuilder = new StringBuilder(128);
            private static readonly StringBuilder _characterXpBuilder = new StringBuilder(128);
            private static readonly StringBuilder _gainXpBuilder = new StringBuilder(128);
            private static readonly StringBuilder _extendedXpBuilder = new StringBuilder(128);
            private static readonly StringBuilder _tempContentBuilder = new StringBuilder(128);

            private static readonly Dictionary<Skills.SkillType, string> _skillNameCache = new Dictionary<Skills.SkillType, string>();

            private static void AppendBuilder(StringBuilder target, StringBuilder source)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    target.Append(source[i]);
                }
            }

            private static void BuildWithBrackets(StringBuilder target, string bracketInput, StringBuilder content, string hexColor)
            {
                target.Append("<color=").Append(hexColor).Append('>');
                if (!string.IsNullOrEmpty(bracketInput))
                {
                    target.Append(bracketInput[0]);
                    AppendBuilder(target, content);
                    if (bracketInput.Length >= 2) target.Append(bracketInput[1]);
                }
                else
                {
                    AppendBuilder(target, content);
                }
                target.Append("</color>");
            }

            [HarmonyPrefix]
            static void Prefix(Skills __instance, Skills.SkillType skillType, out SkillState __state)
            {
                __state = default;

                if (SkipRunningSkillNotifications.Value && skillType == Skills.SkillType.Run) return;
                if (!ShowXPNotifications.Value) return;

                var skillData = SkillDataRef(__instance);
                if (skillData != null && skillData.TryGetValue(skillType, out Skills.Skill skill))
                {
                    float lvlBefore = skill.m_level;
                    float neededXp = (float)Math.Round((Mathf.Pow(lvlBefore + 1f, 1.5f) * 0.5f + 0.5f) * 100f) / 100f;
                    float progressPercent = skill.GetLevelPercentage() * 100f;
                    float calcXp = (float)Math.Round(((progressPercent / 100f) * neededXp) * 100f) / 100f;

                    __state = new SkillState {
                        Skill = skill,
                        LevelBefore = lvlBefore,
                        NeededXpBefore = neededXp,
                        CalculatedXpBefore = calcXp
                    };
                }
            }

            [HarmonyPostfix]
            static void Postfix(Skills __instance, Skills.SkillType skillType, float factor, SkillState __state)
            {
                if (__state.Skill == null) return;

                Skills.Skill skill = __state.Skill;
                bool isLevelUp = skill.m_level > __state.LevelBefore;
                
                float neededXP = (float)Math.Round((Mathf.Pow(skill.m_level + 1f, 1.5f) * 0.5f + 0.5f) * 100f) / 100f;
                float progressPercent = skill.GetLevelPercentage() * 100f;
                float currentXP = (float)Math.Round(((progressPercent / 100f) * neededXP) * 100f) / 100f;

                float actualGain = isLevelUp 
                    ? (__state.NeededXpBefore - __state.CalculatedXpBefore) + currentXP 
                    : currentXP - __state.CalculatedXpBefore;

                actualGain = (float)Math.Round(actualGain * 100f) / 100f;

                if (!_skillNameCache.TryGetValue(skillType, out string skillName))
                {
                    string internalName = skill.m_info.m_skill.ToString();
                    string standardKey = "$skill_" + internalName.ToLower();
                    skillName = Localization.instance.Localize(standardKey);

                    if (skillName.StartsWith("$skill_"))
                    {
                        string alternativeTranslation = Localization.instance.Localize(internalName);
                        if (!string.IsNullOrEmpty(alternativeTranslation) && alternativeTranslation != internalName && !alternativeTranslation.StartsWith("$"))
                        {
                            skillName = alternativeTranslation;
                        }
                        else
                        {
                            skillName = System.Text.RegularExpressions.Regex.Replace(internalName, "([a-z])([A-Z])", "$1 $2");
                        }
                    }
                    _skillNameCache[skillType] = skillName;
                }

                _mainBuilder.Clear();
                _mainBuilder.Append("<size=").Append(NotificationTextSizeXP.Value).Append('>');

                if (ShowSkillLevel.Value)
                {
                    _levelBuilder.Clear();
                    _tempContentBuilder.Clear();
                    string prefixText = TextSkillLevel.Value;
                    if (AutoSpaceSkillLevel.Value && !string.IsNullOrEmpty(prefixText)) _tempContentBuilder.Append(prefixText).Append(' ');
                    else if (!string.IsNullOrEmpty(prefixText)) _tempContentBuilder.Append(prefixText);
                    
                    _tempContentBuilder.Append((int)skill.m_level);
                    BuildWithBrackets(_levelBuilder, SymbolsLevel.Value, _tempContentBuilder, _hexSkillLevel);
                }

                if (ShowCharacterXP.Value)
                {
                    if (_cachedTotalLevel < 0f || isLevelUp)
                    {
                        _cachedTotalLevel = 0f;
                        var dict = SkillDataRef(__instance);
                        if (dict != null)
                        {
                            foreach (KeyValuePair<Skills.SkillType, Skills.Skill> kvp in dict)
                            {
                                if (kvp.Value != null) _cachedTotalLevel += kvp.Value.m_level;
                            }
                        }
                    }

                    _characterXpBuilder.Clear();
                    _tempContentBuilder.Clear();
                    string prefixTotal = TextTotalLevel.Value;
                    if (AutoSpaceTotalLevel.Value && !string.IsNullOrEmpty(prefixTotal)) _tempContentBuilder.Append(prefixTotal).Append(' ');
                    else if (!string.IsNullOrEmpty(prefixTotal)) _tempContentBuilder.Append(prefixTotal);

                    _tempContentBuilder.Append((int)_cachedTotalLevel);
                    BuildWithBrackets(_characterXpBuilder, SymbolsTotal.Value, _tempContentBuilder, _hexTotalLevel);
                }

                if (ShowCharacterXP.Value && CharacterXPPosition.Value == TotalPos.Start)
                    AppendBuilder(_mainBuilder, _characterXpBuilder.Append(' '));

                if (!string.IsNullOrEmpty(SkillSymbol.Value)) 
                    _mainBuilder.Append(SkillSymbol.Value).Append(' ');
                
                if (ShowSkillLevel.Value && SkillLevelPosition.Value == LevelPos.BeforeSkill)
                    AppendBuilder(_mainBuilder, _levelBuilder.Append(' '));

                _mainBuilder.Append("<color=").Append(_hexSkillName).Append('>').Append(skillName).Append("</color>");

                if (ShowSkillLevel.Value && SkillLevelPosition.Value == LevelPos.AfterSkill)
                    AppendBuilder(_mainBuilder, _levelBuilder.Insert(0, ' '));

                if (ShowGainPourcent.Value || ShowGainXP.Value || ShowExtendedXP.Value)
                {
                    _mainBuilder.Append(" :");

                    if (ShowGainPourcent.Value)
                    {
                        _mainBuilder.Append(' ');
                        _tempContentBuilder.Clear();
                        _tempContentBuilder.Append(progressPercent.ToString("0.0#")).Append(" %");
                        BuildWithBrackets(_mainBuilder, SymbolsPercent.Value, _tempContentBuilder, _hexPercentage);
                    }

                    if (ShowGainXP.Value)
                    {
                        _gainXpBuilder.Clear();
                        _tempContentBuilder.Clear();
                        _tempContentBuilder.Append('+').Append(actualGain.ToString("0.##"));
                        BuildWithBrackets(_gainXpBuilder, SymbolsGain.Value, _tempContentBuilder, _hexXPGain);
                    }

                    if (ShowExtendedXP.Value)
                    {
                        _extendedXpBuilder.Clear();
                        _tempContentBuilder.Clear();
                        _tempContentBuilder.Append(currentXP.ToString("0.##")).Append('/').Append(neededXP.ToString("0.##"));
                        BuildWithBrackets(_extendedXpBuilder, SymbolsExtended.Value, _tempContentBuilder, _hexExtendedXP);
                    }

                    if (GainXPPosition.Value == GainXpPos.BeforeExtended)
                    {
                        if (ShowGainXP.Value) AppendBuilder(_mainBuilder, _gainXpBuilder.Insert(0, ' '));
                        if (ShowExtendedXP.Value) AppendBuilder(_mainBuilder, _extendedXpBuilder.Insert(0, ' '));
                    }
                    else
                    {
                        if (ShowExtendedXP.Value) AppendBuilder(_mainBuilder, _extendedXpBuilder.Insert(0, ' '));
                        if (ShowGainXP.Value) AppendBuilder(_mainBuilder, _gainXpBuilder.Insert(0, ' '));
                    }
                }
                else if (!string.IsNullOrEmpty(TextFallbackMessage.Value))
                {
                    _mainBuilder.Append(' ').Append(TextFallbackMessage.Value);
                }

                if (ShowSkillLevel.Value && SkillLevelPosition.Value == LevelPos.BeforeTotal)
                    AppendBuilder(_mainBuilder, _levelBuilder.Insert(0, ' '));

                if (ShowCharacterXP.Value && CharacterXPPosition.Value == TotalPos.End)
                    AppendBuilder(_mainBuilder, _characterXpBuilder.Insert(0, ' '));

                _mainBuilder.Append("</size>");

                if (MessageHud.instance != null)
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, _mainBuilder.ToString());
            }
        }
    }

    public class ConfigurationManagerAttributes
    {
        public int? Order;
    }
}