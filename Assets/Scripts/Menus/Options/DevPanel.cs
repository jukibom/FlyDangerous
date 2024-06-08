using System.Globalization;
using Core;
using Core.ShipModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;


public class DevPanel : MonoBehaviour {
    [SerializeField] private InputField massTextField;
    [SerializeField] private InputField maxSpeedTextField;
    [SerializeField] private InputField maxBoostSpeedTextField;
    [SerializeField] private InputField maxThrustTextField;
    [SerializeField] private InputField maxAngularVelocityTextField;
    [SerializeField] private InputField dragTextField;
    [SerializeField] private InputField angularDragTextField;
    [SerializeField] private InputField torqueThrustMultiplierTextField;
    [SerializeField] private InputField throttleMultiplierTextField;
    [SerializeField] private InputField latHMultiplierTextField;
    [SerializeField] private InputField latVMultiplierTextField;
    [SerializeField] private InputField pitchMultiplierTextField;
    [SerializeField] private InputField rollMultiplierTextField;
    [SerializeField] private InputField yawMultiplierTextField;
    [SerializeField] private InputField thrustBoostMultiplierTextField;
    [SerializeField] private InputField torqueBoostMultiplierTextField;
    [SerializeField] private InputField totalBoostTimeTextField;
    [SerializeField] private InputField totalBoostRotationalTimeTextField;
    [SerializeField] private InputField boostMaxSpeedDropOffTimeTextField;
    [SerializeField] private InputField boostRechargeTimeTextField;
    [SerializeField] private InputField boostCapacitorCostTextField;
    [SerializeField] private InputField boostCapacitorRechargeRateTextField;
    [SerializeField] private InputField boostMaxDivertablePowerTextField;
    [SerializeField] private InputField intertialTensorMultiplierTextField;
    [SerializeField] private InputField minUserLimitedVelocityTextField;
    [SerializeField] private InputField boostDivertEfficiencyTextField;
    [SerializeField] private InputField boostSpoolUpTimeTextField;

    private bool _initialised; 

    // Start is called before the first frame update
    private void Start() {
        var defaults = ShipParameters.CreateDefaults();

        
        boostSpoolUpTimeTextField.transform.parent.transform.parent.gameObject.SetActive(false); // remove once audio issues are fixed

        massTextField.placeholder.GetComponent<Text>().text =
            defaults.mass.ToString(CultureInfo.InvariantCulture);
        maxSpeedTextField.placeholder.GetComponent<Text>().text =
            defaults.maxSpeed.ToString(CultureInfo.InvariantCulture);
        maxBoostSpeedTextField.placeholder.GetComponent<Text>().text =
            defaults.maxBoostSpeed.ToString(CultureInfo.InvariantCulture);
        maxThrustTextField.placeholder.GetComponent<Text>().text =
            defaults.maxThrust.ToString(CultureInfo.InvariantCulture);
        maxAngularVelocityTextField.placeholder.GetComponent<Text>().text =
            defaults.maxAngularVelocity.ToString(CultureInfo.InvariantCulture);
        dragTextField.placeholder.GetComponent<Text>().text =
            defaults.drag.ToString(CultureInfo.InvariantCulture);
        angularDragTextField.placeholder.GetComponent<Text>().text =
            defaults.angularDrag.ToString(CultureInfo.InvariantCulture);
        torqueThrustMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.torqueThrustMultiplier.ToString(CultureInfo.InvariantCulture);
        throttleMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.throttleMultiplier.ToString(CultureInfo.InvariantCulture);
        latHMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.latHMultiplier.ToString(CultureInfo.InvariantCulture);
        latVMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.latVMultiplier.ToString(CultureInfo.InvariantCulture);
        pitchMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.pitchMultiplier.ToString(CultureInfo.InvariantCulture);
        rollMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.rollMultiplier.ToString(CultureInfo.InvariantCulture);
        yawMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.yawMultiplier.ToString(CultureInfo.InvariantCulture);
        thrustBoostMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.thrustBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        torqueBoostMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.torqueBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        totalBoostTimeTextField.placeholder.GetComponent<Text>().text =
            defaults.totalBoostTime.ToString(CultureInfo.InvariantCulture);
        totalBoostRotationalTimeTextField.placeholder.GetComponent<Text>().text =
            defaults.totalBoostRotationalTime.ToString(CultureInfo.InvariantCulture);
        boostMaxSpeedDropOffTimeTextField.placeholder.GetComponent<Text>().text =
            defaults.boostMaxSpeedDropOffTime.ToString(CultureInfo.InvariantCulture);
        boostMaxDivertablePowerTextField.placeholder.GetComponent<Text>().text =
            defaults.boostMaxDivertablePower.ToString(CultureInfo.InvariantCulture);
        boostRechargeTimeTextField.placeholder.GetComponent<Text>().text =
            defaults.boostRechargeTime.ToString(CultureInfo.InvariantCulture);
        boostCapacitorCostTextField.placeholder.GetComponent<Text>().text =
            defaults.boostCapacitorPercentCost.ToString(CultureInfo.InvariantCulture);
        boostCapacitorRechargeRateTextField.placeholder.GetComponent<Text>().text =
            defaults.boostCapacityPercentChargeRate.ToString(CultureInfo.InvariantCulture);
        intertialTensorMultiplierTextField.placeholder.GetComponent<Text>().text =
            defaults.inertiaTensorMultiplier.ToString(CultureInfo.InvariantCulture);
        minUserLimitedVelocityTextField.placeholder.GetComponent<Text>().text =
            defaults.minUserLimitedVelocity.ToString(CultureInfo.InvariantCulture);
        boostDivertEfficiencyTextField.placeholder.GetComponent<Text>().text =
            defaults.boostDivertEfficiency.ToString(CultureInfo.InvariantCulture);
        boostSpoolUpTimeTextField.placeholder.GetComponent<Text>().text =
            defaults.boostSpoolUpTime.ToString(CultureInfo.InvariantCulture);

        UpdateTextFields(Game.Instance.ShipParameters);
    }

    public void EnableLegacyTextfields() {
        massTextField.transform.parent.transform.parent.gameObject.SetActive(true);
        intertialTensorMultiplierTextField.transform.parent.transform.parent.gameObject.SetActive(true);
    }

    public void RestoreDefaults() {
        UpdateTextFields(ShipParameters.CreateDefaults());
    }

    public void CopyToClipboard() {
        var json = GetFlightParams().ToJsonString();

        var jObject = JObject.Parse(json);
        if ((float)jObject.GetValue("mass") == ShipParameters.CreateDefaults().mass)
            jObject.Remove("mass");
        if ((float)jObject.GetValue("inertiaTensorMultiplier") == ShipParameters.CreateDefaults().inertiaTensorMultiplier)
            jObject.Remove("inertiaTensorMultiplier");
        if ((float)jObject.GetValue("boostSpoolUpTime") == ShipParameters.CreateDefaults().boostSpoolUpTime) // again remove oncce audio issues are fixed. 
            jObject.Remove("boostSpoolUpTime");

        GUIUtility.systemCopyBuffer = jObject.ToString(Formatting.Indented);
    }

    public void LoadFromClipboard() {
        var data = GUIUtility.systemCopyBuffer.ToString();
        var parameters = ShipParameters.FromJsonString(data);
        if (parameters != null)
        {
            if (Game.Instance.ShipParameters.mass == ShipParameters.CreateDefaults().mass && Game.Instance.ShipParameters.inertiaTensorMultiplier == ShipParameters.CreateDefaults().inertiaTensorMultiplier) //Nececary IF stamement because ShipParameters can be changed before the menu is opened for the first time.  
                EnableLegacyTextfields();
            UpdateTextFields(parameters);
        }
    }

    // Update is called once per frame
    public void UpdateTextFields(ShipParameters parameters) {

        if (Game.Instance.ShipParameters.mass == ShipParameters.CreateDefaults().mass && Game.Instance.ShipParameters.inertiaTensorMultiplier == ShipParameters.CreateDefaults().inertiaTensorMultiplier) //Nececary IF stamement because ShipParameters can be changed before the menu is opened for the first time.  
            EnableLegacyTextfields();

        massTextField.text =
            parameters.mass.ToString(CultureInfo.InvariantCulture);
        maxSpeedTextField.text =
            parameters.maxSpeed.ToString(CultureInfo.InvariantCulture);
        maxBoostSpeedTextField.text =
            parameters.maxBoostSpeed.ToString(CultureInfo.InvariantCulture);
        maxThrustTextField.text =
            parameters.maxThrust.ToString(CultureInfo.InvariantCulture);
        maxAngularVelocityTextField.text =
            parameters.maxAngularVelocity.ToString(CultureInfo.InvariantCulture);
        dragTextField.text =
            parameters.drag.ToString(CultureInfo.InvariantCulture);
        angularDragTextField.text =
            parameters.angularDrag.ToString(CultureInfo.InvariantCulture);
        torqueThrustMultiplierTextField.text =
            parameters.torqueThrustMultiplier.ToString(CultureInfo.InvariantCulture);
        throttleMultiplierTextField.text =
            parameters.throttleMultiplier.ToString(CultureInfo.InvariantCulture);
        latHMultiplierTextField.text =
            parameters.latHMultiplier.ToString(CultureInfo.InvariantCulture);
        latVMultiplierTextField.text =
            parameters.latVMultiplier.ToString(CultureInfo.InvariantCulture);
        pitchMultiplierTextField.text =
            parameters.pitchMultiplier.ToString(CultureInfo.InvariantCulture);
        rollMultiplierTextField.text =
            parameters.rollMultiplier.ToString(CultureInfo.InvariantCulture);
        yawMultiplierTextField.text =
            parameters.yawMultiplier.ToString(CultureInfo.InvariantCulture);
        thrustBoostMultiplierTextField.text =
            parameters.thrustBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        torqueBoostMultiplierTextField.text =
            parameters.torqueBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        totalBoostTimeTextField.text =
            parameters.totalBoostTime.ToString(CultureInfo.InvariantCulture);
        totalBoostRotationalTimeTextField.text =
            parameters.totalBoostRotationalTime.ToString(CultureInfo.InvariantCulture);
        boostMaxSpeedDropOffTimeTextField.text =
            parameters.boostMaxSpeedDropOffTime.ToString(CultureInfo.InvariantCulture);
        boostMaxDivertablePowerTextField.text =
            parameters.boostMaxDivertablePower.ToString(CultureInfo.InvariantCulture);
        boostRechargeTimeTextField.text =
            parameters.boostRechargeTime.ToString(CultureInfo.InvariantCulture);
        boostCapacitorCostTextField.text =
            parameters.boostCapacitorPercentCost.ToString(CultureInfo.InvariantCulture);
        boostCapacitorRechargeRateTextField.text =
            parameters.boostCapacityPercentChargeRate.ToString(CultureInfo.InvariantCulture);
        intertialTensorMultiplierTextField.text =
            parameters.inertiaTensorMultiplier.ToString(CultureInfo.InvariantCulture);
        minUserLimitedVelocityTextField.text =
            parameters.minUserLimitedVelocity.ToString(CultureInfo.InvariantCulture);
        boostDivertEfficiencyTextField.text =
            parameters.boostDivertEfficiency.ToString(CultureInfo.InvariantCulture);
        boostSpoolUpTimeTextField.text =
            parameters.boostSpoolUpTime.ToString(CultureInfo.InvariantCulture);

        _initialised = true;
    }

    public ShipParameters GetFlightParams() {

        if (!_initialised) return ShipParameters.CreateDefaults();

        return new ShipParameters {
            mass =
                float.Parse(massTextField.text, CultureInfo.InvariantCulture),
            maxSpeed =
                float.Parse(maxSpeedTextField.text, CultureInfo.InvariantCulture),
            maxBoostSpeed =
                float.Parse(maxBoostSpeedTextField.text, CultureInfo.InvariantCulture),
            maxThrust =
                float.Parse(maxThrustTextField.text, CultureInfo.InvariantCulture),
            maxAngularVelocity =
                float.Parse(maxAngularVelocityTextField.text, CultureInfo.InvariantCulture),
            drag =
                float.Parse(dragTextField.text, CultureInfo.InvariantCulture),
            angularDrag =
                float.Parse(angularDragTextField.text, CultureInfo.InvariantCulture),
            torqueThrustMultiplier =
                float.Parse(torqueThrustMultiplierTextField.text, CultureInfo.InvariantCulture),
            throttleMultiplier =
                float.Parse(throttleMultiplierTextField.text, CultureInfo.InvariantCulture),
            latHMultiplier =
                float.Parse(latHMultiplierTextField.text, CultureInfo.InvariantCulture),
            latVMultiplier =
                float.Parse(latVMultiplierTextField.text, CultureInfo.InvariantCulture),
            pitchMultiplier =
                float.Parse(pitchMultiplierTextField.text, CultureInfo.InvariantCulture),
            rollMultiplier =
                float.Parse(rollMultiplierTextField.text, CultureInfo.InvariantCulture),
            yawMultiplier =
                float.Parse(yawMultiplierTextField.text, CultureInfo.InvariantCulture),
            thrustBoostMultiplier =
                float.Parse(thrustBoostMultiplierTextField.text, CultureInfo.InvariantCulture),
            torqueBoostMultiplier =
                float.Parse(torqueBoostMultiplierTextField.text, CultureInfo.InvariantCulture),
            totalBoostTime =
                float.Parse(totalBoostTimeTextField.text, CultureInfo.InvariantCulture),
            totalBoostRotationalTime =
                float.Parse(totalBoostRotationalTimeTextField.text, CultureInfo.InvariantCulture),
            boostMaxSpeedDropOffTime =
                float.Parse(boostMaxSpeedDropOffTimeTextField.text, CultureInfo.InvariantCulture),
            boostMaxDivertablePower =
                float.Parse(boostMaxDivertablePowerTextField.text, CultureInfo.InvariantCulture),
            boostRechargeTime =
                float.Parse(boostRechargeTimeTextField.text, CultureInfo.InvariantCulture),
            boostCapacitorPercentCost =
                float.Parse(boostCapacitorCostTextField.text, CultureInfo.InvariantCulture),
            boostCapacityPercentChargeRate =
                float.Parse(boostCapacitorRechargeRateTextField.text, CultureInfo.InvariantCulture),
            inertiaTensorMultiplier =
                float.Parse(intertialTensorMultiplierTextField.text, CultureInfo.InvariantCulture),
            minUserLimitedVelocity =
                float.Parse(minUserLimitedVelocityTextField.text, CultureInfo.InvariantCulture),
            boostDivertEfficiency =
                float.Parse(boostDivertEfficiencyTextField.text, CultureInfo.InvariantCulture),
            boostSpoolUpTime =
                float.Parse(boostSpoolUpTimeTextField.text, CultureInfo.InvariantCulture),
        };
    }
}