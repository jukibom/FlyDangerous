
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FlagIcon : MonoBehaviour {
    [SerializeField] private SpriteAtlas flagSpriteAtlas;
    private Image _image;

    private void OnEnable() {
        _image = GetComponent<Image>();
        SetFlag("gb");
    }

    public void SetFlag(string isoFlag) {
        var flag = flagSpriteAtlas.GetSprite(isoFlag);
        if (flag != null) {
            _image.sprite = flag;
        }
    }
}
