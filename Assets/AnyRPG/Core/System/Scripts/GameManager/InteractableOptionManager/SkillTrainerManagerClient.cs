using AnyRPG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnyRPG {
    public class SkillTrainerManagerClient : InteractableOptionManager {

        private SkillTrainerComponent skillTrainerComponent = null;

        public SkillTrainerComponent SkillTrainerComponent { get => skillTrainerComponent; set => skillTrainerComponent = value; }

        public void SetSkillTrainer(SkillTrainerComponent skillTrainerComponent, int componentIndex, int choiceIndex) {
            //Debug.Log("ClassChangeManager.SetDisplayClass(" + characterClass + ")");
            this.skillTrainerComponent = skillTrainerComponent;

            BeginInteraction(skillTrainerComponent, componentIndex, choiceIndex);
        }

        public void RequestLearnSkill(UnitController sourceUnitController, string skillName) {

            if (systemGameManager.GameMode == GameMode.Local) {
                skillTrainerComponent.LearnSkill(sourceUnitController, skillName);
            } else {
                networkManagerClient.RequestLearnSkill(skillTrainerComponent.Interactable, componentIndex, skillName);
            }
        }

        public void UnlearnSkill(UnitController sourceUnitController, Skill skill) {
            sourceUnitController.CharacterSkillManager.UnLearnSkill(skill);
        }

        public bool SkillIsKnown(UnitController sourceUnitController, Skill skill) {
            return sourceUnitController.CharacterSkillManager.HasSkill(skill);
        }

        public override void EndInteraction() {
            base.EndInteraction();

            skillTrainerComponent = null;
        }


    }

}