using AnyRPG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnyRPG
{
    [System.Serializable]
    public class ItemPrerequisite : ConfiguredClass, IPrerequisite
    {
        private SystemEventManager systemEventManager = null;

        public event System.Action<UnitController> OnStatusUpdated = delegate { };

        [SerializeField]
        [ResourceSelector(resourceType = typeof(Item))]
        private string prerequisiteName = string.Empty;

        [Tooltip("If set, item must be equipped in this specific slot")]
        [SerializeField]
        [ResourceSelector(resourceType = typeof(EquipmentSlotProfile))]
        private string requiredSlotName = string.Empty;

        [Tooltip("How many of this item are required")]
        [SerializeField]
        private int requiredCount = 1;

        [Tooltip("If true, the item must be equipped. If false, just needs to be in inventory.")]
        [SerializeField]
        private bool requireEquipped = false;

        private bool prerequisiteMet = false;

        private string ownerName = null;

        private Item prerequisiteItem = null;

        private EquipmentSlotProfile requiredSlot = null;

        public override void SetGameManagerReferences()
        {
            base.SetGameManagerReferences();
            systemEventManager = systemGameManager.SystemEventManager;
        }

        public void HandleItemCountChanged(UnitController unitController, Item item)
        {
            //Debug.Log($"ItemPrerequisite.HandleItemCountChanged({item.DisplayName})");

            // Only update if it's the item we care about
            if (item == prerequisiteItem)
            {
                UpdateStatus(unitController);
            }
        }

        // ADD THIS: Subscribe to equipment changes
        public void HandleEquipmentChanged(EquipmentSlotProfile equipmentSlotProfile, InstantiatedEquipment instantiatedEquipment)
        {
            //Debug.Log($"ItemPrerequisite.HandleEquipmentChanged({equipmentSlotProfile.ResourceName})");

            // Only care if we're checking for equipped items
            if (requireEquipped && prerequisiteItem is Equipment)
            {
                // If a specific slot is required, only update if that slot changed
                if (requiredSlot != null)
                {
                    if (equipmentSlotProfile == requiredSlot)
                    {
                        UpdateStatus(systemGameManager.PlayerManagerClient.UnitController);
                    }
                }
                else
                {
                    // No specific slot, update on any equipment change
                    UpdateStatus(systemGameManager.PlayerManagerClient.UnitController);
                }
            }
        }

        public void UpdateStatus(UnitController sourceUnitController, bool notify = true)
        {
            //Debug.Log($"ItemPrerequisite.UpdateStatus({sourceUnitController.gameObject.name}): item: {prerequisiteItem.ResourceName}, required: {requiredCount}");

            bool originalResult = prerequisiteMet;

            if (prerequisiteItem == null)
            {
                Debug.LogError($"ItemPrerequisite.UpdateStatus(): prerequisiteItem IS NULL FOR {prerequisiteName}! FIX THIS!");
                return;
            }

            // Check if item requirement is met
            if (requireEquipped)
            {
                // Check if item is equipped (and optionally in the right slot)
                prerequisiteMet = IsItemEquipped(sourceUnitController);
            }
            else
            {
                // Check inventory count
                int itemCount = sourceUnitController.CharacterInventoryManager.GetItemCount(prerequisiteItem.ResourceName);
                prerequisiteMet = (itemCount >= requiredCount);
            }

            // Notify if status changed
            if (prerequisiteMet != originalResult && notify == true)
            {
                //Debug.Log($"ItemPrerequisite.UpdateStatus(): {prerequisiteItem.DisplayName} status changed to {prerequisiteMet}");
                OnStatusUpdated(sourceUnitController);
            }
        }

        private bool IsItemEquipped(UnitController unitController)
        {
            if (prerequisiteItem is Equipment equipment)
            {
                // If a specific slot is required, check only that slot
                if (requiredSlot != null)
                {
                    if (unitController.CharacterEquipmentManager.CurrentEquipment.ContainsKey(requiredSlot))
                    {
                        var equipSlot = unitController.CharacterEquipmentManager.CurrentEquipment[requiredSlot];
                        return (equipSlot.InstantiatedEquipment != null &&
                                equipSlot.InstantiatedEquipment.Equipment == equipment);
                    }
                    return false;
                }

                // No specific slot required - check if equipped anywhere
                foreach (var equipSlot in unitController.CharacterEquipmentManager.CurrentEquipment.Values)
                {
                    if (equipSlot.InstantiatedEquipment != null &&
                        equipSlot.InstantiatedEquipment.Equipment == equipment)
                    {
                        return true;
                    }
                }
                return false;
            }

            // Non-equipment items can't be equipped
            return false;
        }

        public virtual bool IsMet(UnitController sourceUnitController)
        {
            //Debug.Log($"ItemPrerequisite.IsMet(): {prerequisiteItem.DisplayName} returning {prerequisiteMet}");
            return prerequisiteMet;
        }

        public void SetupScriptableObjects(SystemGameManager systemGameManager, string ownerName)
        {
            this.ownerName = ownerName;
            Configure(systemGameManager);

            prerequisiteItem = null;
            if (prerequisiteName != null && prerequisiteName != string.Empty)
            {
                Item tmpPrerequisiteItem = systemDataFactory.GetResource<Item>(prerequisiteName);
                if (tmpPrerequisiteItem != null)
                {
                    //Debug.Log($"ItemPrerequisite.SetupScriptableObjects(): setting: {prerequisiteName}");
                    prerequisiteItem = tmpPrerequisiteItem;
                }
                else
                {
                    Debug.LogError($"ItemPrerequisite.SetupScriptableObjects(): Could not find item: {prerequisiteName} while initializing an item prerequisite for {ownerName}. CHECK INSPECTOR");
                }
            }
            else
            {
                Debug.LogError($"ItemPrerequisite.SetupScriptableObjects(): prerequisiteName was empty while initializing an item prerequisite for {ownerName}. CHECK INSPECTOR");
            }

            // Load the required equipment slot if specified
            requiredSlot = null;
            if (requiredSlotName != null && requiredSlotName != string.Empty)
            {
                EquipmentSlotProfile tmpRequiredSlot = systemDataFactory.GetResource<EquipmentSlotProfile>(requiredSlotName);
                if (tmpRequiredSlot != null)
                {
                    requiredSlot = tmpRequiredSlot;
                }
                else
                {
                    Debug.LogError($"ItemPrerequisite.SetupScriptableObjects(): Could not find equipment slot: {requiredSlotName} while initializing an item prerequisite for {ownerName}. CHECK INSPECTOR");
                }
            }

            // Subscribe to item count changes if we have a valid item
            if (prerequisiteItem != null)
            {
                systemEventManager.OnItemCountChanged += HandleItemCountChanged;

                // Also subscribe to equipment changes if we're checking equipped status
                if (requireEquipped)
                {
                    systemEventManager.OnAddEquipment += HandleEquipmentChanged;
                    systemEventManager.OnRemoveEquipment += HandleEquipmentChanged;
                }
            }
        }

        public void CleanupScriptableObjects()
        {
            if (systemEventManager != null)
            {
                systemEventManager.OnItemCountChanged -= HandleItemCountChanged;
                systemEventManager.OnAddEquipment -= HandleEquipmentChanged;
                systemEventManager.OnRemoveEquipment -= HandleEquipmentChanged;
            }
        }
    }
}