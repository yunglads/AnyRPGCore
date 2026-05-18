using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnyRPG
{
    public class CharacterSkillManager : ConfiguredClass
    {
        UnitController unitController;

        private Dictionary<string, Skill> skillList = new Dictionary<string, Skill>();
        private Dictionary<string, SkillProgress> skillProgress = new Dictionary<string, SkillProgress>();

        // game manager references
        protected PlayerManagerClient playerManager = null;
        protected SystemEventManager systemEventManager = null;
        protected TownBuildingManager townManager = null;

        public Dictionary<string, Skill> SkillList { get => skillList; }

        //public List<string> MySkillList { get => skillList;}

        [Header("Skill Leveling")]

        [Tooltip("Entry XP amount. All following levels are multiplied by XP scaling")]
        [SerializeField] private float entryXP = 50f;

        [Tooltip("Max Level for leveling skills")]
        [SerializeField] private int skillLevelCap = 100;

        [Tooltip("XP multiplier per level")]
        [SerializeField] private float skillXPMultiplier = 0.2f;

        [Tooltip("Penalty per level above node level")]
        [SerializeField] private float xpLevelPenalty = 0.25f;

        [Tooltip("Minimum XP that can be granted")]
        [SerializeField] private int minXPGranted = 5;

        private float scaledXP;

        public float EntryXP { get => entryXP; set => entryXP = value; }
        public int SkillLevelCap { get => skillLevelCap; set => skillLevelCap = value; }
        public float SkillXPMultiplier { get => skillXPMultiplier; set => skillXPMultiplier = value; }
        public float XPLevelPenalty { get => xpLevelPenalty; set => xpLevelPenalty = value; }
        public int MinXPGranted { get => minXPGranted; set => minXPGranted = value; }

        public float ScaledXP;

        public CharacterSkillManager(UnitController unitController, SystemGameManager systemGameManager)
        {
            this.unitController = unitController;
            Configure(systemGameManager);
        }

        public override void SetGameManagerReferences()
        {
            base.SetGameManagerReferences();
            playerManager = systemGameManager.PlayerManagerClient;
            systemEventManager = systemGameManager.SystemEventManager;
            townManager = systemGameManager.TownManager;
        }

        public void UpdateSkillList(int newLevel)
        {
            //Debug.Log("CharacterSkillManager.UpdateSkillList()");
            foreach (Skill skill in systemDataFactory.GetResourceList<Skill>())
            {
                if (!HasSkill(skill) && skill.RequiredLevel <= newLevel && skill.AutoLearn == true)
                {
                    LearnSkill(skill);
                    //InitializeSkillProgress(skill);
                }
            }
        }

        public bool HasSkill(Skill checkSkill)
        {
            if (checkSkill == null)
            {
                Debug.LogWarning("CharacterSkillManager.HasSkill: checkSkill is null!");
                return false;
            }

            bool hasIt = skillList.ContainsKey(checkSkill.ResourceName);
            //Debug.Log($"HasSkill({checkSkill.ResourceName}): {hasIt}");
            return hasIt;
        }

        public void LearnSkill(Skill newSkill)
        {
            //Debug.Log($"{unitController.gameObject.name}.CharacterSkillManager.LearnSkill({newSkill.ResourceName})");

            if (!skillList.ContainsKey(newSkill.ResourceName))
            {
                skillList[newSkill.ResourceName] = newSkill;

                foreach (AbilityProperties ability in newSkill.AbilityList)
                {
                    unitController.CharacterAbilityManager.LearnAbility(ability);
                }

                foreach (Recipe recipe in systemDataFactory.GetResourceList<Recipe>())
                {
                    if (unitController.CharacterStats.Level >= recipe.RecipeLevel && recipe.AutoLearn == true && newSkill.AbilityList.Contains(recipe.CraftAbility))
                    {
                        unitController.CharacterRecipeManager.LearnRecipe(recipe);
                    }
                }

                InitializeSkillProgress(newSkill);

                //Debug.Log($"Successfully added skill: {newSkill.ResourceName} to skillList. Total skills: {skillList.Count}");

                unitController.UnitEventController.NotifyOnLearnSkill(newSkill);
            }
            else
            {
                Debug.LogWarning($"Skill {newSkill.ResourceName} already learned, skipping.");
            }
        }

        public void LoadSkill(string skillName)
        {
            //Debug.Log("CharacterSkillManager.LoadSkill()");

            // don't crash on loading old save Data
            if (skillName == null || skillName == string.Empty)
            {
                return;
            }
            if (!skillList.ContainsKey(skillName))
            {
                Skill skill = skillName != null ? systemDataFactory.GetResource<Skill>(skillName)
                : systemDataFactory.GetResource<WeaponSkill>(skillName) as Skill;
            }
        }

        public void UnLearnSkill(Skill oldSkill)
        {
            if (oldSkill == null) return;

            if (skillList.ContainsKey(oldSkill.ResourceName))
            {
                skillList.Remove(oldSkill.ResourceName);

                if (skillProgress.ContainsKey(oldSkill.ResourceName))
                {
                    skillProgress.Remove(oldSkill.ResourceName);
                }

                foreach (AbilityProperties ability in oldSkill.AbilityList)
                {
                    unitController.CharacterAbilityManager.UnlearnAbility(ability);
                }
                //Debug.Log($"Unlearned skill: {oldSkill.ResourceName}");
            }

            unitController.UnitEventController.NotifyOnUnLearnSkill(oldSkill);
        }

        public void InitializeSkillProgress(Skill skill)
        {
            if (!skillProgress.ContainsKey(skill.ResourceName))
            {
                skillProgress.Add(skill.ResourceName, new SkillProgress(skill));
            }
        }

        public void GainSkillXP(Skill skill, float xp, int nodeLevel)
        {
            if (!skillProgress.ContainsKey(skill.ResourceName))
                return;

            SkillProgress prog = skillProgress[skill.ResourceName];
            scaledXP = ScaleXP(prog.level, nodeLevel, xp);
            prog.xp += Mathf.Round(scaledXP);

            ScaledXP = scaledXP;

            CheckLevelUp(prog);
        }

        private float ScaleXP(int playerLevel, int nodeLevel, float baseXP)
        {
            int diff = nodeLevel - playerLevel;

            // harder content = bonus XP
            if (diff > 0)
            {
                return baseXP * (1f + diff * skillXPMultiplier);
            }

            // equal difficulty
            if (diff == 0)
                return baseXP;

            // easier content = penalty
            float penalty = 1f / (1f + (-diff) * xpLevelPenalty);
            float scaled = baseXP * penalty;

            return Mathf.Max(minXPGranted, scaled);
        }

        public void RequestGainSkillXP(Skill skill, float xp, int nodeLevel)
        {
            FishNetUnitController fishNet = unitController.GetComponent<FishNetUnitController>();

            //explicitly call each possible skill gain
            if (fishNet == null)
            {
                // True offline mode (no networking at all)
                GainSkillXP(skill, xp, nodeLevel);
                return;
            }

            if (fishNet.IsServerStarted)
            {
                // Already on server
                GainSkillXP(skill, xp, nodeLevel);
                fishNet.HandleGainSkillXP(skill.ResourceName, unitController.CharacterSkillManager.GetSkillXP(skill.ResourceName), nodeLevel, (int)ScaledXP);
            }
            else
            {
                // Client asks server
                fishNet.HandleGainSkillXPServer(skill.ResourceName, xp, nodeLevel);
            }
        }

        public void SetSkillXP(Skill skill, float xp)
        {
            SkillProgress prog = skillProgress[skill.ResourceName];
            prog.xp = xp;
        }

        public float GetSkillXP(string skillName)
        {
            if (!skillProgress.ContainsKey(skillName))
                return 0;

            return skillProgress[skillName].xp;
        }

        private int GetXPRequiredForLevel(int level)
        {
            int roundedXP = Mathf.RoundToInt(entryXP * Mathf.Pow(level, skillXPMultiplier));
            return roundedXP;
        }

        private void CheckLevelUp(SkillProgress prog)
        {
            FishNetUnitController fishNet = unitController.GetComponent<FishNetUnitController>();

            if (prog.level <= skillLevelCap)
            {
                float needed = GetXPRequiredForLevel(prog.level);

                if (prog.xp >= needed)
                {
                    prog.xp -= needed;
                    prog.level++;

                    LevelUpSkillEffect();

                    if (fishNet == null || fishNet.IsServerInitialized)
                    {
                        // already server/offline > sync directly
                        if (fishNet != null)
                            fishNet.HandleSetSkillLevel(prog.skill.ResourceName, prog.level);
                    }
                    else
                    {
                        // client > ask server
                        fishNet.HandleSetSkillLevelServer(prog.skill.ResourceName, prog.level);
                    }
                    Debug.Log($"{prog.skill.ResourceName} leveled to {prog.level}");
                }
                //Debug.Log($"Players {prog.skill} Level: {prog.level} Current XP: {prog.xp} XP Needed: {needed -= prog.xp}");
            }
            else
            {
                Debug.Log("Skill level cap reached. Cant earn anymore xp");
                prog.level = skillLevelCap;
            }
        }

        public void SetSkillLevel(Skill skill, int level)
        {
            SkillProgress prog = skillProgress[skill.ResourceName];
            prog.level = level;
        }

        private void LevelUpSkillEffect()
        {
            if (systemConfigurationManager.LevelUpEffect != null)
            {
                playerManager.PlayLevelUpEffects(unitController, 0);
            }

        }

        public Skill GetAnySkill(string skillName)
        {
            Skill skill = systemDataFactory.GetResource<Skill>(skillName);
            if (skill == null)
            {
                skill = systemDataFactory.GetResource<WeaponSkill>(skillName) as Skill;
            }
            return skill;
        }

        public int GetSkillLevel(Skill skill)
        {
            if (!skillProgress.ContainsKey(skill.ResourceName))
            {
                return 1;
            }

            return skillProgress[skill.ResourceName].level;
        }

        public SkillProgress GetSkillProgress(Skill skill)
        {
            if (!skillProgress.ContainsKey(skill.ResourceName))
            {
                skillProgress[skill.ResourceName] = new SkillProgress(skill);
            }
            return skillProgress[skill.ResourceName];
        }

        public float GetXPToNextLevel(Skill skill)
        {
            SkillProgress prog = GetSkillProgress(skill);
            return GetXPRequiredForLevel(prog.level);
        }

        public float GetSkillProgressPercent(Skill skill)
        {
            SkillProgress prog = GetSkillProgress(skill);

            float needed = GetXPRequiredForLevel(prog.level);
            return prog.xp / needed;
        }

        public void AddDeathXP()
        {
            Skill dyingSkill = systemDataFactory.GetResource<Skill>("Dying");

            if (!SkillList.ContainsKey("Dying"))
            {
                LearnSkill(dyingSkill);
            }

            RequestGainSkillXP(dyingSkill, 25f, 1);
        }

        public void AddWeaponXP(UnitProfile enemy, WeaponSkill associatedSkill)
        {
            if (associatedSkill != null && !HasSkill(associatedSkill))
            {
                LearnSkill(associatedSkill);
            }

            foreach (Skill skill in SkillList.Values.Cast<WeaponSkill>())
            {
                if (skill.ResourceName == associatedSkill.ResourceName)
                {
                    RequestGainSkillXP(skill, enemy.BaseXP, enemy.EnemyLevel);
                }
                else
                {
                    Debug.LogWarning($"{associatedSkill.ResourceName} does not match any skill in list");
                }
            }
        }

        public List<SkillSaveData> GetSkillSaveData()
        {
            List<SkillSaveData> saveList = new List<SkillSaveData>();

            foreach (var kvp in skillProgress)
            {
                SkillProgress prog = kvp.Value;

                SkillSaveData data = new SkillSaveData();
                data.SkillName = prog.skill.ResourceName;
                data.SkillLevel = prog.level;
                data.SkillXP = prog.xp;

                //Debug.Log($"Saving - Skill Name: {data.SkillName}, Level: {data.SkillLevel}, XP: {data.SkillXP}");

                saveList.Add(data);
            }

            return saveList;
        }

        public void LoadSkillProgress(List<SkillSaveData> savedSkills)
        {
            foreach (SkillSaveData saved in savedSkills)
            {
                // Try to load as regular Skill first
                //Skill skill = systemDataFactory.GetResource<Skill>(saved.SkillName);

                //// If not found, try WeaponSkill
                //if (skill == null)
                //{
                //    skill = systemDataFactory.GetResource<WeaponSkill>(saved.SkillName) as Skill;
                //}

                Skill skill = GetAnySkill(saved.SkillName);

                if (skill == null)
                {
                    //Debug.LogWarning($"Could not load skill: {saved.SkillName}");
                    continue;
                }

                if (!skillList.ContainsKey(saved.SkillName))
                    LearnSkill(skill);

                skillProgress[saved.SkillName].level = saved.SkillLevel;
                skillProgress[saved.SkillName].xp = saved.SkillXP;

                //Debug.Log($"Loaded - Skill Name: {saved.SkillName}, Level: {saved.SkillLevel}, XP: {saved.SkillXP}");
            }
        }
    }
}