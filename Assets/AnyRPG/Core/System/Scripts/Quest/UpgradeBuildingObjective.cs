using AnyRPG;
using UnityEngine;

[System.Serializable]
public class UpgradeBuildingObjective : QuestObjective
{
    public string buildingID;
    public BuildingPhase requiredPhase;
    private UnitController cachedUnitController;
    private TownBuildingManager cachedTownManager;

    public override void OnAcceptQuest(UnitController sourceUnitController, QuestBase quest, bool printMessages = true)
    {
        cachedUnitController = sourceUnitController;
        cachedTownManager = systemGameManager.TownManager;
        base.OnAcceptQuest(sourceUnitController, quest, printMessages);
        //cachedTownManager = systemGameManager.TownManager;
        //TownBuildingManager manager = systemGameManager.TownManager;
        if (cachedTownManager != null)
        {
            //Debug.Log(cachedTownManager.GetInstanceID());
            //Debug.Log($"Objective subscribing to manager: {cachedTownManager.GetInstanceID()}");
            cachedTownManager.OnBuildingUpgraded += HandleBuildingUpgraded;
        }
        else
        {
            Debug.LogError("TownBuildingManager not found during quest accept");
        }    
    }

    public override void OnAbandonQuest(UnitController sourceUnitController)
    {
        base.OnAbandonQuest(sourceUnitController);

        //TownBuildingManager manager = systemGameManager.TownManager;
        //TownBuildingManager manager = GameObject.FindFirstObjectByType<TownBuildingManager>();
        if (cachedTownManager != null)
        {
            //Debug.Log($"Unsubscribing from manager instance: {cachedTownManager.GetInstanceID()}");
            cachedTownManager.OnBuildingUpgraded -= HandleBuildingUpgraded;
        }
    }

    private void HandleBuildingUpgraded(string id, BuildingPhase phase)
    {
        //Debug.Log("EVENT RECEIVED IN OBJECTIVE");

        if (id != buildingID)
        {
            Debug.Log($"{id} doesnt match {buildingID} returning");
            return;
        }
            

        if (phase != requiredPhase)
        {
            Debug.Log($"{phase} doesnt match {requiredPhase} returning");
            return;
        }
            

        bool completeBefore = IsComplete(cachedUnitController);

        SetCurrentAmount(cachedUnitController, CurrentAmount(cachedUnitController) + 1);

        if (!completeBefore && IsComplete(cachedUnitController))
        {
            // optional message
        }

        questBase.CheckCompletion(cachedUnitController);
        Debug.Log($"Quest Completed???");
        //Debug.Log($"ID: {id} matches with buildingID: {buildingID} and phase: {phase} matches with requiredPhase: " +
            //$"{requiredPhase} quest should complete.");
    }
}
