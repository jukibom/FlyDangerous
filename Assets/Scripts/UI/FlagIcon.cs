using Core.Player;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FlagIcon : MonoBehaviour {
    [SerializeField] private SpriteAtlas flagSpriteAtlas;
    private Image _image;

    private void OnEnable() {
        _image = GetComponent<Image>();
    }

    public void SetFlag(Flag flag) {
        var flagSprite = flagSpriteAtlas.GetSprite(flag.Filename);
        _image.enabled = flagSprite != null;
        if (flagSprite != null) _image.sprite = flagSprite;
    }
}