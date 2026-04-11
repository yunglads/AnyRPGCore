using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnyRPG {
    public class SkillTrainerManagerServer : InteractableOptionManager {

        public void LearnSkill(UnitController sourceUnitController, Interactable interactable, int componentIndex, string skillName)
        {
            Dictionary<int, InteractableOptionComponent> currentInteractables = interactable.GetCurrentInteractables(sourceUnitController);
            if (currentInteractables[componentIndex] is SkillTrainerComponent)
            {
                (currentInteractables[componentIndex] as SkillTrainerComponent).LearnSkill(sourceUnitController, skillName);
            }
        }

        //public void LearnSkill(UnitController sourceUnitController, Interactable interactable, int componentIndex, string skillName)
        //{
        //    Dictionary<int, InteractableOptionComponent> currentInteractables = interactable.GetCurrentInteractables(sourceUnitController);
        //    if (currentInteractables.Values.FirstOrDefault(c => c is SkillTrainerComponent) is SkillTrainerComponent trainer)
        //        trainer.LearnSkill(sourceUnitController, skillName);
        //}

    }

}