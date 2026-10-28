using System.Globalization;
using Core;
using Core.ShipModel;
using FdUI;
using UnityEngine;
using UnityEngine.UI;

public class DevPanel : MonoBehaviour {
    [SerializeField] private TMPro.TMP_InputField massTextField;
    [SerializeField] private TMPro.TMP_InputField maxSpeedTextField;
    [SerializeField] private TMPro.TMP_InputField maxBoostSpeedTextField;
    [SerializeField] private TMPro.TMP_InputField maxThrustTextField;
    [SerializeField] private TMPro.TMP_InputField maxAngularVelocityTextField;
    [SerializeField] private TMPro.TMP_InputField dragTextField;
    [SerializeField] private TMPro.TMP_InputField angularDragTextField;
    [SerializeField] private TMPro.TMP_InputField torqueThrustMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField throttleMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField latHMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField latVMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField pitchMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField rollMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField yawMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField thrustBoostMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField torqueBoostMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField totalBoostTimeTextField;
    [SerializeField] private TMPro.TMP_InputField totalBoostRotationalTimeTextField;
    [SerializeField] private TMPro.TMP_InputField boostMaxSpeedDropOffTimeTextField;
    [SerializeField] private TMPro.TMP_InputField boostRechargeTimeTextField;
    [SerializeField] private TMPro.TMP_InputField boostCapacitorCostTextField;
    [SerializeField] private TMPro.TMP_InputField boostCapacitorRechargeRateTextField;
    [SerializeField] private TMPro.TMP_InputField boostMaxDivertablePowerTextField;
    [SerializeField] private TMPro.TMP_InputField intertialTensorMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField minUserLimitedVelocityTextField;
    [SerializeField] private Checkbox useAltBoostersCheckbox;
    [SerializeField] private TMPro.TMP_InputField boosterForceMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField boosterVelocityMultiplierTextField;
    [SerializeField] private TMPro.TMP_InputField boosterThrustMultiplierTextField;

    private bool _initialised;

    // Start is called before the first frame update
    private void Start() {
        var defaults = ShipParameters.Defaults;

        massTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.mass.ToString(CultureInfo.InvariantCulture);
        maxSpeedTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.maxSpeed.ToString(CultureInfo.InvariantCulture);
        maxBoostSpeedTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.maxBoostSpeed.ToString(CultureInfo.InvariantCulture);
        maxThrustTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.maxThrust.ToString(CultureInfo.InvariantCulture);
        maxAngularVelocityTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.maxAngularVelocity.ToString(CultureInfo.InvariantCulture);
        dragTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.drag.ToString(CultureInfo.InvariantCulture);
        angularDragTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.angularDrag.ToString(CultureInfo.InvariantCulture);
        torqueThrustMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.torqueThrustMultiplier.ToString(CultureInfo.InvariantCulture);
        throttleMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.throttleMultiplier.ToString(CultureInfo.InvariantCulture);
        latHMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.latHMultiplier.ToString(CultureInfo.InvariantCulture);
        latVMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.latVMultiplier.ToString(CultureInfo.InvariantCulture);
        pitchMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.pitchMultiplier.ToString(CultureInfo.InvariantCulture);
        rollMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.rollMultiplier.ToString(CultureInfo.InvariantCulture);
        yawMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.yawMultiplier.ToString(CultureInfo.InvariantCulture);
        thrustBoostMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.thrustBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        torqueBoostMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.torqueBoostMultiplier.ToString(CultureInfo.InvariantCulture);
        totalBoostTimeTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.totalBoostTime.ToString(CultureInfo.InvariantCulture);
        totalBoostRotationalTimeTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.totalBoostRotationalTime.ToString(CultureInfo.InvariantCulture);
        boostMaxSpeedDropOffTimeTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boostMaxSpeedDropOffTime.ToString(CultureInfo.InvariantCulture);
        boostMaxDivertablePowerTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boostMaxDivertablePower.ToString(CultureInfo.InvariantCulture);
        boostRechargeTimeTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boostRechargeTime.ToString(CultureInfo.InvariantCulture);
        boostCapacitorCostTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boostCapacitorPercentCost.ToString(CultureInfo.InvariantCulture);
        boostCapacitorRechargeRateTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boostCapacityPercentChargeRate.ToString(CultureInfo.InvariantCulture);
        intertialTensorMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.inertiaTensorMultiplier.ToString(CultureInfo.InvariantCulture);
        minUserLimitedVelocityTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.minUserLimitedVelocity.ToString(CultureInfo.InvariantCulture);
        useAltBoostersCheckbox.isChecked = defaults.useAltBoosters;
        boosterForceMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boosterForceMultiplier.ToString(CultureInfo.InvariantCulture);
        boosterVelocityMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boosterVelocityMultiplier.ToString(CultureInfo.InvariantCulture);
        boosterThrustMultiplierTextField.placeholder.GetComponent<TMPro.TMP_Text>().text =
            defaults.boosterThrustMultiplier.ToString(CultureInfo.InvariantCulture);

        UpdateTextFields(Game.Instance.ShipParameters);
    }

    public void RestoreDefaults() {
        UpdateTextFields(ShipParameters.Defaults);
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
        useAltBoostersCheckbox.isChecked = parameters.useAltBoosters;
        boosterForceMultiplierTextField.text =
            parameters.boosterForceMultiplier.ToString(CultureInfo.InvariantCulture);
        boosterVelocityMultiplierTextField.text =
            parameters.boosterVelocityMultiplier.ToString(CultureInfo.InvariantCulture);
        boosterThrustMultiplierTextField.text =
            parameters.boosterThrustMultiplier.ToString(CultureInfo.InvariantCulture);

        _initialised = true;
    }

    public ShipParameters GetFlightParams() {
        if (!_initialised) return Game.Instance.ShipParameters;

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
            boostSpoolUpTime = 1, // can't currently change this
            useAltBoosters = useAltBoostersCheckbox.isChecked,
            boosterForceMultiplier =
                float.Parse(boosterForceMultiplierTextField.text, CultureInfo.InvariantCulture),
            boosterVelocityMultiplier =
                float.Parse(boosterVelocityMultiplierTextField.text, CultureInfo.InvariantCulture),
            boosterThrustMultiplier =
                float.Parse(boosterThrustMultiplierTextField.text, CultureInfo.InvariantCulture),
            
        };
    }
}