using AnyRPG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AnyRPG {
    public class SkillBookPanel : PagedWindowContents {

        [SerializeField]
        private List<SkillButton> skillButtons = new List<SkillButton>();

        private PlayerManagerClient playerManagerClient = null;

        public override void Configure(SystemGameManager systemGameManager) {
            base.Configure(systemGameManager);
            playerManagerClient = systemGameManager.PlayerManagerClient;

            foreach (SkillButton skillButton in skillButtons) {
                skillButton.Configure(systemGameManager);
            }
            pageSize = skillButtons.Count;
        }

        protected override void PopulatePages()
        {
            //Debug.Log("SkillBookUI.CreatePages()");
            SkillContentList page = new SkillContentList();

            var sortedSkills = playerManagerClient.UnitController.CharacterSkillManager.SkillList.Values.OrderBy(skill => skill.ResourceName).ToList();

            foreach (Skill playerSkill in sortedSkills)
            {
                page.skills.Add(playerSkill);
                if (page.skills.Count == pageSize)
                {
                    pages.Add(page);
                    page = new SkillContentList();
                }
            }
            if (page.skills.Count > 0)
            {
                pages.Add(page);
            }

            AddSkills();
        }

        public void AddSkills()
        {
            //Debug.Log("SkillBookUI.AddSkills()");
            if (pages.Count > 0)
            {
                for (int i = 0; i < pageSize; i++)
                {
                    //for (int i = 0; i < pages[pageIndex].Count - 1; i++) {
                    //Debug.Log("SkillBookUI.AddSkills(): i: " + i);
                    if (i < (pages[pageIndex] as SkillContentList).skills.Count)
                    {
                        //Debug.Log("adding skill");
                        skillButtons[i].gameObject.SetActive(true);
                        skillButtons[i].AddSkill((pages[pageIndex] as SkillContentList).skills[i]);
                    }
                    else
                    {
                        //Debug.Log("clearing skill");
                        skillButtons[i].ClearSkill();
                        skillButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        //protected override void PopulatePages() {
        //    //Debug.Log("SkillBookUI.CreatePages()");
        //    SkillContentList page = new SkillContentList();
        //    foreach (CharacterSkillData characterSkillData in playerManagerClientClient.UnitController.CharacterSkillManager.SkillList.Values) {
        //        page.skills.Add(characterSkillData);
        //        if (page.skills.Count == pageSize) {
        //            pages.Add(page);
        //            page = new SkillContentList();
        //        }
        //    }
        //    if (page.skills.Count > 0) {
        //        pages.Add(page);
        //    }
        //    AddSkills();
        //}

        //public void AddSkills() {
        //    //Debug.Log("SkillBookUI.AddSkills()");
        //    if (pages.Count > 0) {
        //        for (int i = 0; i < pageSize; i++) {
        //            //for (int i = 0; i < pages[pageIndex].Count - 1; i++) {
        //            //Debug.Log("SkillBookUI.AddSkills(): i: " + i);
        //            if (i < (pages[pageIndex] as SkillContentList).skills.Count) {
        //                //Debug.Log("adding skill");
        //                skillButtons[i].gameObject.SetActive(true);
        //                skillButtons[i].AddSkill((pages[pageIndex] as SkillContentList).skills[i]);
        //            } else {
        //                //Debug.Log("clearing skill");
        //                skillButtons[i].ClearSkill();
        //                skillButtons[i].gameObject.SetActive(false);
        //            }
        //        }
        //    }
        //}

        /*
        public override void LoadPage(int pageIndex) {
            base.LoadPage(pageIndex);
            AddSkills();
        }
        */

        public override void AddPageContent() {
            base.AddPageContent();
            AddSkills();
        }

        public override void ClearButtons() {
            base.ClearButtons();
            foreach (SkillButton btn in skillButtons) {
                btn.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (playerManagerClient?.UnitController != null)
            {
                playerManagerClient.UnitController.UnitEventController.OnGainSkillXP += HandleSkillXPChanged;
                playerManagerClient.UnitController.UnitEventController.OnSkillLevelChanged += HandleSkillLevelChanged;
            }
        }

        private void OnDisable()
        {
            if (playerManagerClient?.UnitController != null)
            {
                playerManagerClient.UnitController.UnitEventController.OnGainSkillXP -= HandleSkillXPChanged;
                playerManagerClient.UnitController.UnitEventController.OnSkillLevelChanged -= HandleSkillLevelChanged;
            }
        }

        private void HandleSkillXPChanged(string skillName, float xp, int nodeLevel)
        {
            RefreshVisibleSkills(skillName);
        }

        private void HandleSkillLevelChanged(string skillName, int level)
        {
            RefreshVisibleSkills(skillName);
        }

        private void RefreshVisibleSkills(string skillName)
        {
            foreach (SkillButton button in skillButtons)
            {
                if (button.HasSkill(skillName))
                {
                    button.Refresh();
                }
            }
        }
    }

    public class SkillContentList : PagedContentList {
        public List<Skill> skills = new List<Skill>();
    }
}