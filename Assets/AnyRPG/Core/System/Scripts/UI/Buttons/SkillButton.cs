using AnyRPG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnyRPG {
    public class SkillButton : TransparencyButton {

        [SerializeField]
        protected string skillName = string.Empty;

        protected Skill currentSkill = null;

        //protected CharacterSkillData characterSkillData = null;

        [SerializeField]
        protected Image icon = null;

        [SerializeField]
        protected TextMeshProUGUI skillNameText = null;

        [SerializeField]
        protected TextMeshProUGUI description = null;

        [SerializeField]
        protected TextMeshProUGUI levelText = null;
        [SerializeField]
        protected TextMeshProUGUI inBarXpText = null;
        [SerializeField]
        protected Slider xpBar;

        // game manager references
        private PlayerManagerClient playerManagerClient = null;

        public override void SetGameManagerReferences() {
            base.SetGameManagerReferences();
            playerManagerClient = systemGameManager.PlayerManagerClient;
        }

        //public void AddSkill(CharacterSkillData newSkill) {
        //    Debug.Log("SkillButton.AddSkill(" + (skillName != null && skillName != string.Empty ? skillName : "null") + ")");
        //    characterSkillData = newSkill;
        //    if (characterSkillData != null) {
        //        icon.sprite = characterSkillData.Skill.Icon;
        //        icon.color = Color.white;
        //        skillNameText.text = characterSkillData.Skill.DisplayName;
        //        string levelString = string.Empty;
        //        string extranewline = string.Empty;
        //        if (characterSkillData.Skill.UseSkillLevels) {
        //            levelString = $"Level {characterSkillData.SkillLevel}/{characterSkillData.Skill.GetSkillCapForLevel(playerManagerClient.UnitController.CharacterStats.Level)}\n";
        //            extranewline = "\n";
        //            if (characterSkillData.Skill.UseSkillExperience) {
        //                levelString += $"Experience ({characterSkillData.SkillExperience}/{characterSkillData.Skill.GetExperienceRequiredForLevel(characterSkillData.SkillLevel)})";
        //            }
        //        }
        //        description.text = $"{levelString}<size=12>{extranewline}<i>{characterSkillData.Skill.GetDescription()}</i></size>";
        //    } else {
        //        Debug.Log("SkillButton.AddSkill(): failed to get skill!!!");
        //    }
        //}

        //public void ClearSkill() {
        //    icon.sprite = null;
        //    icon.color = new Color32(0, 0, 0, 0);
        //    skillNameText.text = string.Empty;
        //    description.text = string.Empty;
        //}

        public void AddSkill(Skill newSkill)
        {
            Debug.Log("SkillButton.AddSkill(" + (skillName != null && skillName != string.Empty ? skillName : "null") + ")");
            currentSkill = newSkill;
            Debug.Log($"currentSkill: {currentSkill} newSkill: {newSkill}");
            if (currentSkill != null)
            {
                icon.sprite = currentSkill.Icon;
                icon.color = Color.white;
                skillNameText.text = currentSkill.DisplayName;
                description.text = currentSkill.GetDescription();

                CharacterSkillManager skillManager = playerManagerClient.UnitController.CharacterSkillManager;
                SkillProgress prog = skillManager.GetSkillProgress(currentSkill);
                levelText.text = $"Level: {prog.level} / {skillManager.SkillLevelCap}";
                inBarXpText.text = prog.xp.ToString() + " / " + skillManager.GetXPToNextLevel(currentSkill).ToString();
                xpBar.value = skillManager.GetSkillProgressPercent(currentSkill);

            }
            else
            {
                Debug.Log("SkillButton.AddSkill(): failed to get skill!!!");
            }
        }

        public void ClearSkill()
        {
            icon.sprite = null;
            icon.color = new Color32(0, 0, 0, 0);
            skillNameText.text = string.Empty;
            description.text = string.Empty;
        }

        public bool HasSkill(string skillName)
        {
            return currentSkill != null && currentSkill.ResourceName == skillName;
        }

        public void Refresh()
        {
            if (currentSkill == null)
                return;
            //float xp = playerManager.UnitController.CharacterSkillManager.GetSkillXP(currentSkill.ToString());
            //int level = playerManager.UnitController.CharacterSkillManager.GetSkillLevel(currentSkill);
            CharacterSkillManager skillManager = playerManagerClient.UnitController.CharacterSkillManager;
            SkillProgress prog = skillManager.GetSkillProgress(currentSkill);
            levelText.text = $"LVL {prog.level}";
            inBarXpText.text = prog.xp.ToString() + " / " + skillManager.GetXPToNextLevel(currentSkill).ToString();
            xpBar.value = skillManager.GetSkillProgressPercent(currentSkill);
        }
    }

}