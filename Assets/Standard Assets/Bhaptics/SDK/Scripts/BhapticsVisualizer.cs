using Bhaptics.Tact.Unity;
using UnityEngine;


public class BhapticsVisualizer : MonoBehaviour
{
    private VisualFeedback[] visualFeedback;


    void Awake()
    {
        visualFeedback = GetComponentsInChildren<VisualFeedback>();
    }

    void Update()
    {
        if (!BhapticsManager.Init)
        {
            return;
        }

        var haptic = BhapticsManager.GetHaptic();

        if (haptic == null)
        {
            return;
        }

        foreach (var vis in visualFeedback)
        {
            var feedback = haptic.GetCurrentFeedback(BhapticsUtils.ToPositionType(vis.devicePos));
    
            vis.UpdateFeedback(feedback);
        }
    }
}