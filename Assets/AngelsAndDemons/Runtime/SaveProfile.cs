using System;
using System.IO;
using UnityEngine;

namespace AngelsAndDemons
{
    [Serializable]
    public sealed class SaveProfileData
    {
        public int gold = 600;
        public int gems = 10;
        public int clearedAngelStage = 0; // 0..10 (Clearing 10 unlocks Demon Campaign per original game rule)
        public int clearedDemonStage = 0; // 0..10
        public bool demonUnlockedOverride = false;
        public int selectedFaction = 0; // 0 = Angel, 1 = Demon
        public int selectedAngelCommander = 0; // 0..5 (Hinata, Ino, Sakura, Temari, Tsunade, Konan)
        public int selectedDemonCommander = 4; // 0..5 (Default Lucifer = 4)
        public int[] angelUnitLevels = new int[] { 1, 1, 1, 1, 1, 1 };
        public int[] demonUnitLevels = new int[] { 1, 1, 1, 1, 1, 1 };
        public int[] commanderSkillLevels = new int[] { 1, 1, 1 };
        public int castleLevel = 1;
        public int economyMetaLevel = 1;
        public int[] stageStars = new int[20];
    }

    public sealed class SaveProfile
    {
        public SaveProfileData Data { get; private set; } = new SaveProfileData();

        // Original Morisoft "Angels & Demons" rule: Clear Angel Campaign (Stage 10) to unlock Demon Campaign
        public bool IsDemonCampaignUnlocked => Data.clearedAngelStage >= 10 || Data.demonUnlockedOverride;

        public FactionKind ActiveFaction =>
            (Data.selectedFaction == 1 && IsDemonCampaignUnlocked) ? FactionKind.Demon : FactionKind.Angel;

        public int GetCommanderSlot(FactionKind faction) =>
            faction == FactionKind.Angel
                ? Mathf.Clamp(Data.selectedAngelCommander, 0, 5)
                : Mathf.Clamp(Data.selectedDemonCommander, 0, 5);

        public void SetCommanderSlot(FactionKind faction, int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, 5);
            if (faction == FactionKind.Angel) Data.selectedAngelCommander = slotIndex;
            else Data.selectedDemonCommander = slotIndex;
        }

        public int GetUnitLevel(FactionKind faction, int slotIndex)
        {
            var arr = faction == FactionKind.Angel ? Data.angelUnitLevels : Data.demonUnitLevels;
            if (arr == null || arr.Length < 6) return 1;
            return Mathf.Max(1, arr[Mathf.Clamp(slotIndex, 0, 5)]);
        }

        public int GetUnitUpgradeCost(FactionKind faction, int slotIndex)
        {
            int lv = GetUnitLevel(faction, slotIndex);
            return 120 + (lv - 1) * 85 + slotIndex * 40;
        }

        public bool TryUpgradeUnit(FactionKind faction, int slotIndex)
        {
            int lv = GetUnitLevel(faction, slotIndex);
            if (lv >= 20) return false;
            int cost = GetUnitUpgradeCost(faction, slotIndex);
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            if (faction == FactionKind.Angel) Data.angelUnitLevels[slotIndex] = lv + 1;
            else Data.demonUnitLevels[slotIndex] = lv + 1;
            return true;
        }

        public int GetSkillLevel(int skillIndex)
        {
            if (Data.commanderSkillLevels == null || Data.commanderSkillLevels.Length < 3) return 1;
            return Mathf.Max(1, Data.commanderSkillLevels[Mathf.Clamp(skillIndex, 0, 2)]);
        }

        public int GetSkillUpgradeCost(int skillIndex)
        {
            int lv = GetSkillLevel(skillIndex);
            return 160 + (lv - 1) * 110 + skillIndex * 50;
        }

        public bool TryUpgradeSkill(int skillIndex)
        {
            skillIndex = Mathf.Clamp(skillIndex, 0, 2);
            int lv = GetSkillLevel(skillIndex);
            if (lv >= 15) return false;
            int cost = GetSkillUpgradeCost(skillIndex);
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            Data.commanderSkillLevels[skillIndex] = lv + 1;
            return true;
        }

        public int GetCastleUpgradeCost() => 150 + (Data.castleLevel - 1) * 100;
        public bool TryUpgradeCastle()
        {
            if (Data.castleLevel >= 20) return false;
            int cost = GetCastleUpgradeCost();
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            Data.castleLevel++;
            return true;
        }

        public int GetEconomyUpgradeCost() => 140 + (Data.economyMetaLevel - 1) * 95;
        public bool TryUpgradeEconomy()
        {
            if (Data.economyMetaLevel >= 20) return false;
            int cost = GetEconomyUpgradeCost();
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            Data.economyMetaLevel++;
            return true;
        }

        public bool IsStageUnlocked(int stageNumber)
        {
            stageNumber = Mathf.Clamp(stageNumber, 1, 20);
            if (stageNumber <= 10)
            {
                return stageNumber <= Data.clearedAngelStage + 1;
            }
            if (!IsDemonCampaignUnlocked) return false;
            int demonIndex = stageNumber - 10; // 1..10
            return demonIndex <= Data.clearedDemonStage + 1;
        }

        public void RecordStageVictory(int stageNumber, int stars, int goldReward, int gemReward)
        {
            stageNumber = Mathf.Clamp(stageNumber, 1, 20);
            stars = Mathf.Clamp(stars, 1, 3);
            Data.gold += Mathf.Max(0, goldReward);
            Data.gems += Mathf.Max(0, gemReward);
            int idx = stageNumber - 1;
            if (Data.stageStars == null || Data.stageStars.Length < 20) Data.stageStars = new int[20];
            if (stars > Data.stageStars[idx]) Data.stageStars[idx] = stars;

            if (stageNumber <= 10)
            {
                if (stageNumber > Data.clearedAngelStage) Data.clearedAngelStage = stageNumber;
            }
            else
            {
                int demonStage = stageNumber - 10;
                if (demonStage > Data.clearedDemonStage) Data.clearedDemonStage = demonStage;
            }
        }

        static string SavePath => Path.Combine(Application.persistentDataPath, "angels_and_demons_save_v1.json");

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(SavePath, json);
            }
            catch
            {
                // Ignore IO errors in restricted environments
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    var loaded = JsonUtility.FromJson<SaveProfileData>(json);
                    if (loaded != null)
                    {
                        if (loaded.angelUnitLevels == null || loaded.angelUnitLevels.Length < 6)
                            loaded.angelUnitLevels = new int[] { 1, 1, 1, 1, 1, 1 };
                        if (loaded.demonUnitLevels == null || loaded.demonUnitLevels.Length < 6)
                            loaded.demonUnitLevels = new int[] { 1, 1, 1, 1, 1, 1 };
                        if (loaded.commanderSkillLevels == null || loaded.commanderSkillLevels.Length < 3)
                            loaded.commanderSkillLevels = new int[] { 1, 1, 1 };
                        if (loaded.stageStars == null || loaded.stageStars.Length < 20)
                            loaded.stageStars = new int[20];
                        Data = loaded;
                    }
                }
            }
            catch
            {
                Data = new SaveProfileData();
            }
        }
    }
}
