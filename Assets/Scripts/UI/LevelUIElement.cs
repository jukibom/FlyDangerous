using Core.MapData;
using UnityEngine;
using UnityEngine.UI;

public class LevelUIElement : MonoBehaviour {
    [SerializeField] private Text levelName;
    [SerializeField] private Image thumbnail;
    private Level _levelData;

    public Level LevelData {
        get => _levelData;
        set {
            _levelData = value;
            levelName.text = _levelData.Name.ToUpper();
            thumbnail.sprite = _levelData.Thumbnail;
        }
    }
}