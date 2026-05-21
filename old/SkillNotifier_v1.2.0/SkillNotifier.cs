using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace SkillNotifier
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SkillNotifierPlugin : BaseUnityPlugin
    {
        public const string ModGUID = "Harchytek.SkillNotifier";
        public const string ModName = "SkillNotifier";
        public const string ModVersion = "1.2.0";

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
            private static readonly FieldInfo SkillDataField = typeof(Skills).GetField("m_skillData", BindingFlags.NonPublic | BindingFlags.Instance);
            private static float _levelBefore;
            private static float _calculatedXpBefore; 
            private static float _neededXpBefore;

            private static (string start, string end) GetBrackets(string input)
            {
                if (string.IsNullOrEmpty(input)) return ("", "");
                if (input.Length >= 2) return (input[0].ToString(), input[1].ToString());
                return (input, ""); 
            }

            [HarmonyPrefix]
            static void Prefix(Skills __instance, Skills.SkillType skillType)
            {
                if (SkipRunningSkillNotifications.Value && skillType == Skills.SkillType.Run) return;
                if (!ShowXPNotifications.Value) return;

                var skillData = SkillDataField?.GetValue(__instance) as Dictionary<Skills.SkillType, Skills.Skill>;
                if (skillData != null && skillData.TryGetValue(skillType, out Skills.Skill skill))
                {
                    _levelBefore = skill.m_level;

                    _neededXpBefore = (float)Math.Round((Mathf.Pow(skill.m_level + 1f, 1.5f) * 0.5f + 0.5f) * 100f) / 100f;
                    float progressPercentBefore = skill.GetLevelPercentage() * 100f;
                    _calculatedXpBefore = (float)Math.Round(((progressPercentBefore / 100f) * _neededXpBefore) * 100f) / 100f;
                }
            }

            [HarmonyPostfix]
            static void Postfix(Skills __instance, Skills.SkillType skillType, float factor = 1f)
            {
                if (SkipRunningSkillNotifications.Value && skillType == Skills.SkillType.Run) return;
                if (!ShowXPNotifications.Value) return;

                var skillData = SkillDataField?.GetValue(__instance) as Dictionary<Skills.SkillType, Skills.Skill>;

                if (skillData != null && skillData.TryGetValue(skillType, out Skills.Skill skill))
                {
                    bool isLevelUp = skill.m_level > _levelBefore;
                    
                    float neededXP = (float)Math.Round((Mathf.Pow(skill.m_level + 1f, 1.5f) * 0.5f + 0.5f) * 100f) / 100f;
                    float progressPercent = skill.GetLevelPercentage() * 100f;

                    float currentXP = (float)Math.Round(((progressPercent / 100f) * neededXP) * 100f) / 100f;

                    float actualGain = 0f;
                    if (isLevelUp)
                    {
                        actualGain = (_neededXpBefore - _calculatedXpBefore) + currentXP;
                    }
                    else
                    {
                        actualGain = currentXP - _calculatedXpBefore;
                    }

                    actualGain = (float)Math.Round(actualGain * 100f) / 100f;

                    string internalName = skill.m_info.m_skill.ToString().ToLower();
                    string skillName = Localization.instance.Localize("$skill_" + internalName);

                    string message = $"<size={NotificationTextSizeXP.Value}>";

                    string levelText = "";
                    if (ShowSkillLevel.Value)
                    {
                        var b = GetBrackets(SymbolsLevel.Value);
                        string prefixText = TextSkillLevel.Value;
                        if (AutoSpaceSkillLevel.Value && !string.IsNullOrEmpty(prefixText)) prefixText += " ";
                        levelText = $"<color={_hexSkillLevel}>{b.start}{prefixText}{skill.m_level:0}{b.end}</color>";
                    }

                    string characterXPText = "";
                    if (ShowCharacterXP.Value)
                    {
                        if (_cachedTotalLevel < 0f || isLevelUp)
                        {
                            _cachedTotalLevel = 0f;
                            foreach (var s in skillData.Values) { _cachedTotalLevel += s.m_level; }
                        }

                        var b = GetBrackets(SymbolsTotal.Value);
                        string prefixTotal = TextTotalLevel.Value;
                        if (AutoSpaceTotalLevel.Value && !string.IsNullOrEmpty(prefixTotal)) prefixTotal += " ";
                        characterXPText = $"<color={_hexTotalLevel}>{b.start}{prefixTotal}{_cachedTotalLevel:0}{b.end}</color>";
                    }

                    if (ShowCharacterXP.Value && CharacterXPPosition.Value == TotalPos.Start)
                        message += characterXPText + " ";

                    if (!string.IsNullOrEmpty(SkillSymbol.Value)) message += $"{SkillSymbol.Value} ";
                    
                    if (ShowSkillLevel.Value && SkillLevelPosition.Value == LevelPos.BeforeSkill)
                        message += levelText + " ";

                    message += $"<color={_hexSkillName}>{skillName}</color>";

                    if (ShowSkillLevel.Value && SkillLevelPosition.Value == LevelPos.AfterSkill)
                        message += " " + levelText;

                    if (ShowGainPourcent.Value || ShowGainXP.Value || ShowExtendedXP.Value)
                    {
                        bool willShowPercent = ShowGainPourcent.Value;
                        
                        if (willShowPercent || ShowGainXP.Value || ShowExtendedXP.Value)
                        {
                            message += " :";
                        }

                        if (willShowPercent)
                        {
                            var b = GetBrackets(SymbolsPercent.Value);
                            message += $" <color={_hexPercentage}>{b.start}{progressPercent:0.0#} %{b.end}</color>";
                        }

                        string gainXpText = "";
                        if (ShowGainXP.Value)
                        {
                            var b = GetBrackets(SymbolsGain.Value);
                            gainXpText = $" <color={_hexXPGain}>{b.start}+{actualGain:0.##}{b.end}</color>";
                        }

                        string extendedXpText = "";
                        if (ShowExtendedXP.Value)
                        {
                            var b = GetBrackets(SymbolsExtended.Value);
                            extendedXpText = $" <color={_hexExtendedXP}>{b.start}{currentXP:0.##}/{neededXP:0.##}{b.end}</color>";
                        }

                        if (GainXPPosition.Value == GainXpPos.BeforeExtended)
                        {
                            message += gainXpText + extendedXpText;
                        }
                        else
                        {
                            message += extendedXpText + gainXpText;
                        }
                    }
                    else if (!string.IsNullOrEmpty(TextFallbackMessage.Value))
                    {
                        message += " " + TextFallbackMessage.Value;
                    }

                    if (ShowSkillLevel.Value && SkillLevelPosition.Value == LevelPos.BeforeTotal)
                        message += " " + levelText;

                    if (ShowCharacterXP.Value && CharacterXPPosition.Value == TotalPos.End)
                        message += " " + characterXPText;

                    message += "</size>";

                    if (MessageHud.instance != null)
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message);
                }
            }
        }
    }

    public class ConfigurationManagerAttributes
    {
        public int? Order;
    }
}