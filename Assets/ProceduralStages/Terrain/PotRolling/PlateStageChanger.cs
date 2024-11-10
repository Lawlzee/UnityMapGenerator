using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlateStageChanger : MonoBehaviour
{
    public static PlateStageChanger instance;
    private float _delay;
    //private HGTextMeshProUGUI _stageTextBox;
    public float totalDelay = 5;
    public UnityAction onDelayFinished;

    public void Awake()
    {
        enabled = false;
        instance = this;
        ObjectivePanelController.collectObjectiveSources += ObjectivePanelController_collectObjectiveSources;
    }

    private void ObjectivePanelController_collectObjectiveSources(RoR2.CharacterMaster characterMaster, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectives)
    {
        objectives.Add(new ObjectivePanelController.ObjectiveSourceDescriptor()
        {
            master = characterMaster,
            objectiveType = typeof(PlateObjectiveTracker),
            source = gameObject
        });
    }

    public void OnEnable()
    {
        //var mapNameCluster = (GameObject.Find("HUDSimple(Clone)") ?? GameObject.Find("RiskUI(Clone)")).transform
        //    .Find("MainContainer")
        //    .Find("MapNameCluster");
        //
        //mapNameCluster.GetComponent<AssignStageToken>().enabled = false;
        //mapNameCluster.GetComponent<TypewriteTextController>().enabled = false;
        //mapNameCluster.gameObject.SetActive(true);
        //
        //_stageTextBox = mapNameCluster
        //    .Find("MainText")
        //    .GetComponent<HGTextMeshProUGUI>();
        //
        //_stageTextBox.SetText("");
        
        _delay = totalDelay;
    }

    public void Update()
    {
        _delay -= Time.deltaTime;
        //_stageTextBox.SetText(_delay.ToString("0.00"));
        if (_delay < 0)
        {
            onDelayFinished();
            enabled = false;
        }
    }

    public void OnDisable()
    {
        _delay = totalDelay;
        //if (_stageTextBox != null)
        //{
        //    _stageTextBox.transform.parent.gameObject.SetActive(false);
        //}
    }

    public void OnDestroy()
    {
        ObjectivePanelController.collectObjectiveSources -= ObjectivePanelController_collectObjectiveSources;
    }

    private class PlateObjectiveTracker : ObjectivePanelController.ObjectiveTracker
    {
        public override string GenerateString()
        {
            if (instance._delay == instance.totalDelay)
            {
                return "Push the pot on the plate";
            }

            return string.Format(instance._delay.ToString("0.00"));
        }

        public override bool IsDirty() => true;
    }
}