using UnityEngine;
using UnityEngine.UI;

public class MedalsScreen : MonoBehaviour {
    [SerializeField] private GameObject newPersonalBest;
    [SerializeField] private GameObject noMedalAwarded;
    [SerializeField] private GameObject bronzeMedal;
    [SerializeField] private GameObject silverMedal;
    [SerializeField] private GameObject goldMedal;
    [SerializeField] private GameObject authorMedal;
    [SerializeField] private Text resultText;

    private void OnEnable() {
        noMedalAwarded.SetActive(false);
        bronzeMedal.SetActive(false);
        silverMedal.SetActive(false);
        goldMedal.SetActive(false);
        authorMedal.SetActive(false);
    }

    public void ShowAnimation(uint medalCount, bool personalBest, string result) {
        newPersonalBest.SetActive(personalBest);
        resultText.text = result;

        // TODO: animations
        switch (medalCount) {
            case 1:
                bronzeMedal.SetActive(true);
                break;
            case 2:
                silverMedal.SetActive(true);
                break;
            case 3:
                goldMedal.SetActive(true);
                break;
            case 4:
                authorMedal.SetActive(true);
                break;
            default:
                noMedalAwarded.SetActive(true);
                break;
        }
    }
}