using System.Globalization;
using Core;
using Core.Player;
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
    [SerializeField] private InputField intertialTensorMultiplierTextField;
    [SerializeField] private InputField minUserLimitedVelocityTextField;

    private bool _initialised;

    // Start is called before the first frame update
    private void Start() {
        var defaults = ShipPlayer.ShipParameterDefaults;

        massTextField.placeholder.GetComponent<Text>().text = defaults.mass.ToString(CultureInfo.InvariantCulture);
        maxSpeedTextField.placeholder.GetComponent<Text>().text = defaults.maxSpeed.ToString(CultureInfo.InvariantCulture);
        maxBoostSpeedTextField.placeholder.GetComponent<Text>().text = defaults.maxBoostSpeed.ToString(CultureInfo.InvariantCulture);
        maxThrustTextField.placeholder.GetComponent<Text>().text = defaults.maxThrust.ToString(CultureInfo.InvariantCulture);
        maxAngularVelocityTextField.placeholder.GetComponent<Text>().text = defaults.maxAngularVelocity.ToString(CultureInfo.InvariantCulture);
        dragTextField.placeholder.GetComponent<Text>().text = defaults.drag.ToString(CultureInfo.InvariantCulture);
        angularDragTextField.placeholder.GetComponent<Text>().text = defaults.angularDrag.ToString(CultureInfo.InvariantCulture);
        torqueThrustMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.torqueThrustMultiplier.ToString(CultureInfo.InvariantCulture);
        throttleMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.throttleMultiplier.ToString(CultureInfo.InvariantCulture);
        latHMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.latHMultiplier.ToString(CultureInfo.InvariantCulture);
        latVMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.latVMultiplier.ToString(CultureInfo.InvariantCulture);
        pitchMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.pitchMultiplier.ToString(CultureInfo.InvariantCulture);
        rollMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.rollMultiplier.ToString(CultureInfo.InvariantCulture);
        yawMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.yawMultiplier.ToString(CultureInfo.InvariantCulture);
        thrustBoostMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.thrustBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        torqueBoostMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.torqueBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        totalBoostTimeTextField.placeholder.GetComponent<Text>().text = defaults.totalBoostTime.ToString(CultureInfo.InvariantCulture);
        totalBoostRotationalTimeTextField.placeholder.GetComponent<Text>().text = defaults.totalBoostRotationalTime.ToString(CultureInfo.InvariantCulture);
        boostMaxSpeedDropOffTimeTextField.placeholder.GetComponent<Text>().text = defaults.boostMaxSpeedDropOffTime.ToString(CultureInfo.InvariantCulture);
        boostRechargeTimeTextField.placeholder.GetComponent<Text>().text = defaults.boostRechargeTime.ToString(CultureInfo.InvariantCulture);
        boostCapacitorCostTextField.placeholder.GetComponent<Text>().text = defaults.boostCapacitorPercentCost.ToString(CultureInfo.InvariantCulture);
        boostCapacitorRechargeRateTextField.placeholder.GetComponent<Text>().text =
            defaults.boostCapacityPercentChargeRate.ToString(CultureInfo.InvariantCulture);
        intertialTensorMultiplierTextField.placeholder.GetComponent<Text>().text = defaults.inertiaTensorMultiplier.ToString(CultureInfo.InvariantCulture);
        minUserLimitedVelocityTextField.placeholder.GetComponent<Text>().text = defaults.minUserLimitedVelocity.ToString(CultureInfo.InvariantCulture);

        UpdateTextFields(Game.Instance.ShipParameters);
    }

    public void RestoreDefaults() {
        UpdateTextFields(ShipPlayer.ShipParameterDefaults);
    }

    public void CopyToClipboard() {
        GUIUtility.systemCopyBuffer = GetFlightParams().ToJsonString();
    }

    public void LoadFromClipboard() {
        var data = GUIUtility.systemCopyBuffer;
        var parameters = ShipParameters.FromJsonString(data);
        if (parameters != null) UpdateTextFields(parameters);
    }

    // Update is called once per frame
    public void UpdateTextFields(ShipParameters parameters) {
        massTextField.text = parameters.mass.ToString(CultureInfo.InvariantCulture);
        maxSpeedTextField.text = parameters.maxSpeed.ToString(CultureInfo.InvariantCulture);
        maxBoostSpeedTextField.text = parameters.maxBoostSpeed.ToString(CultureInfo.InvariantCulture);
        maxThrustTextField.text = parameters.maxThrust.ToString(CultureInfo.InvariantCulture);
        maxAngularVelocityTextField.text = parameters.maxAngularVelocity.ToString(CultureInfo.InvariantCulture);
        dragTextField.text = parameters.drag.ToString(CultureInfo.InvariantCulture);
        angularDragTextField.text = parameters.angularDrag.ToString(CultureInfo.InvariantCulture);
        torqueThrustMultiplierTextField.text = parameters.torqueThrustMultiplier.ToString(CultureInfo.InvariantCulture);
        throttleMultiplierTextField.text = parameters.throttleMultiplier.ToString(CultureInfo.InvariantCulture);
        latHMultiplierTextField.text = parameters.latHMultiplier.ToString(CultureInfo.InvariantCulture);
        latVMultiplierTextField.text = parameters.latVMultiplier.ToString(CultureInfo.InvariantCulture);
        pitchMultiplierTextField.text = parameters.pitchMultiplier.ToString(CultureInfo.InvariantCulture);
        rollMultiplierTextField.text = parameters.rollMultiplier.ToString(CultureInfo.InvariantCulture);
        yawMultiplierTextField.text = parameters.yawMultiplier.ToString(CultureInfo.InvariantCulture);
        thrustBoostMultiplierTextField.text = parameters.thrustBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        torqueBoostMultiplierTextField.text = parameters.torqueBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        totalBoostTimeTextField.text = parameters.totalBoostTime.ToString(CultureInfo.InvariantCulture);
        totalBoostRotationalTimeTextField.text = parameters.totalBoostRotationalTime.ToString(CultureInfo.InvariantCulture);
        boostMaxSpeedDropOffTimeTextField.text = parameters.boostMaxSpeedDropOffTime.ToString(CultureInfo.InvariantCulture);
        boostRechargeTimeTextField.text = parameters.boostRechargeTime.ToString(CultureInfo.InvariantCulture);
        boostCapacitorCostTextField.text = parameters.boostCapacitorPercentCost.ToString(CultureInfo.InvariantCulture);
        boostCapacitorRechargeRateTextField.text = parameters.boostCapacityPercentChargeRate.ToString(CultureInfo.InvariantCulture);
        intertialTensorMultiplierTextField.text = parameters.inertiaTensorMultiplier.ToString(CultureInfo.InvariantCulture);
        minUserLimitedVelocityTextField.text = parameters.minUserLimitedVelocity.ToString(CultureInfo.InvariantCulture);

        _initialised = true;
    }

    public ShipParameters GetFlightParams() {
        if (!_initialised) return ShipPlayer.ShipParameterDefaults;

        return new ShipParameters {
            mass = float.Parse(massTextField.text),
            maxSpeed = float.Parse(maxSpeedTextField.text),
            maxBoostSpeed = float.Parse(maxBoostSpeedTextField.text),
            maxThrust = float.Parse(maxThrustTextField.text),
            maxAngularVelocity = float.Parse(maxAngularVelocityTextField.text),
            drag = float.Parse(dragTextField.text),
            angularDrag = float.Parse(angularDragTextField.text),
            torqueThrustMultiplier = float.Parse(torqueThrustMultiplierTextField.text),
            throttleMultiplier = float.Parse(throttleMultiplierTextField.text),
            latHMultiplier = float.Parse(latHMultiplierTextField.text),
            latVMultiplier = float.Parse(latVMultiplierTextField.text),
            pitchMultiplier = float.Parse(pitchMultiplierTextField.text),
            rollMultiplier = float.Parse(rollMultiplierTextField.text),
            yawMultiplier = float.Parse(yawMultiplierTextField.text),
            thrustBoostMultiplier = float.Parse(thrustBoostMultiplierTextField.text),
            torqueBoostMultiplier = float.Parse(torqueBoostMultiplierTextField.text),
            totalBoostTime = float.Parse(totalBoostTimeTextField.text),
            totalBoostRotationalTime = float.Parse(totalBoostRotationalTimeTextField.text),
            boostMaxSpeedDropOffTime = float.Parse(boostMaxSpeedDropOffTimeTextField.text),
            boostRechargeTime = float.Parse(boostRechargeTimeTextField.text),
            boostCapacitorPercentCost = float.Parse(boostCapacitorCostTextField.text),
            boostCapacityPercentChargeRate = float.Parse(boostCapacitorRechargeRateTextField.text),
            inertiaTensorMultiplier = float.Parse(intertialTensorMultiplierTextField.text),
            minUserLimitedVelocity = float.Parse(minUserLimitedVelocityTextField.text)
        };
    }
}