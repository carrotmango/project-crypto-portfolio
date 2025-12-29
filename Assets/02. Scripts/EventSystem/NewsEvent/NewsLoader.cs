using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewsLoader : MonoBehaviour {

    [Header("UI")]
    public Image profileImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI contentText;

    public GameObject contentImageContainer;
    public Image contentImage;

    //[Header("Buttons")]
    //public Button likeButton;
    //public Button dislikeButton;

    private string eventKey;

    // =========================
    // 외부에서 호출
    // =========================
    public void Load(UIEventData data, DateTime eventTime) {

        eventKey = data.key;

        nameText.text = data.authorName;
        contentText.text = data.message;

        dateText.text = eventTime.ToString("MM/dd/yyyy");
        timeText.text = eventTime.ToString("HH:mm");

        TrySetSpriteOrHide(profileImage, "image/profile", data.profileImage, null);
        TrySetSpriteOrHide(contentImage, "image/content", data.contentImage, contentImageContainer);

        // 버튼 리스너 중복 방지
        //likeButton.onClick.RemoveAllListeners();
        //dislikeButton.onClick.RemoveAllListeners();

        //likeButton.onClick.AddListener(OnLike);
        //dislikeButton.onClick.AddListener(OnDislike);
    }

    // =========================
    // 버튼 콜백
    // =========================
    void OnLike() {
        Debug.Log($"[News] Like: {eventKey}");
    }

    void OnDislike() {
        Debug.Log($"[News] Dislike: {eventKey}");
    }

    // =========================
    // 유틸
    // =========================
    private void TrySetSpriteOrHide(
        Image target,
        string folder,
        string fileName,
        GameObject containerToToggle
    ) {
        if (string.IsNullOrEmpty(fileName)) {
            if (containerToToggle != null) containerToToggle.SetActive(false);
            else if (target != null) target.enabled = false;
            return;
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string path = $"{folder}/{baseName}";
        var sprite = Resources.Load<Sprite>(path);

        if (sprite == null) {
            if (containerToToggle != null) containerToToggle.SetActive(false);
            else if (target != null) target.enabled = false;
            return;
        }

        if (containerToToggle != null) containerToToggle.SetActive(true);
        if (target != null) {
            target.enabled = true;
            target.sprite = sprite;
        }
    }
}
