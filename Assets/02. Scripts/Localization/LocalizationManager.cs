using System.Collections.Generic;
using UnityEngine;
using System.Linq; // [추가] ToDictionary를 쓰려면 이게 필수입니다!

[System.Serializable]
public class LangItem {
    public string key;
    public string kr;
    public string en;
    public string jp;
}

[System.Serializable]
public class LangWrapper {
    public List<LangItem> items;
}

public static class LocalizationManager {
    private static Dictionary<string, LangItem> langDict = new();
    private static int currentLangIndex = 1;

    public static void LoadData() {
        // [경로 수정] 하단 2번 설명 참고!
        TextAsset jsonFile = Resources.Load<TextAsset>("Localization/lang");

        if (jsonFile == null) {
            Debug.LogError("Localization JSON 파일을 찾을 수 없습니다! 경로를 확인하세요.");
            return;
        }

        LangWrapper wrapper = JsonUtility.FromJson<LangWrapper>(jsonFile.text);

        // ToDictionary는 System.Linq가 있어야 작동합니다.
        langDict = wrapper.items.ToDictionary(x => x.key);
    }

    public static string GetText(string key) {
        // [핵심 수정] 데이터가 없으면 여기서 즉시 로드!
        if (langDict == null || langDict.Count == 0) {
            LoadData();
        }

        if (langDict == null || !langDict.ContainsKey(key)) {
            Debug.LogWarning($"[Localization] 키를 찾을 수 없음: {key}");
            return key; // 키가 없으면 키값이라도 보여줌
        }

        return currentLangIndex switch {
            0 => langDict[key].en,
            1 => langDict[key].kr,
            _ => langDict[key].kr
        };
    }

    public static void SetLanguage(int index) {
        currentLangIndex = index;

        // 현재 씬에 살아있는 놈들만 다 가져옵니다.
        var localizers = Object.FindObjectsByType<UILocalizer>(FindObjectsSortMode.None);

        foreach (var l in localizers) {
            // [수정] l이 null이 아니고, 실제 게임 오브젝트가 살아있는지 체크!
            if (l != null && l.gameObject != null) {
                l.Refresh();
            }
        }
    }
}