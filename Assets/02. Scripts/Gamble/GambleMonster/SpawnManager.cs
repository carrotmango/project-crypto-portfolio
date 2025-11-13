using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour {
    [Header("몬스터 및 아이템 프리팹")]
    [SerializeField] private GameObject[] monsters;
    [SerializeField] private GameObject[] items;
    [SerializeField] private Transform coinContainer; 

    public void DropCoin(Vector3 dropPos, int amount) {
        for (int i = 0; i < amount; i++) {
            int randomIndex = Random.Range(0, items.Length);
            GameObject item = Instantiate(items[randomIndex], dropPos, Quaternion.identity, coinContainer); // 수정

            Rigidbody2D rb = item.GetComponent<Rigidbody2D>();
            if (rb != null) {
                float forceX = Random.Range(-2f, 2f);
                float forceY = Random.Range(4f, 6f);
                rb.AddForce(new Vector2(forceX, forceY), ForceMode2D.Impulse);
            }
        }
    }

    public void SpawnMonsterInside(Transform parent) {
        int index = Random.Range(0, monsters.Length);
        Vector2 viewPos = new Vector2(Random.value, Random.value);
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(new Vector3(viewPos.x, viewPos.y, 10f));
        worldPos.z = 0;

        GameObject monster = Instantiate(monsters[index], worldPos, Quaternion.identity, parent);
        monster.tag = "Monster";

        Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        float speed = Random.Range(1.5f, 3.5f);

        Monster monsterScript = monster.GetComponent<Monster>();
        if (monsterScript != null) {
            monsterScript.Init(dir, speed);
        }
    }

    public int CurrentMonsterCount() {
        return transform.childCount;
    }

    public void DespawnAllMonsters() {
        var monsters = GameObject.FindGameObjectsWithTag("Monster");
        Debug.Log($"[몬스터 제거] 찾은 몬스터 수: {monsters.Length}");

        foreach (var monster in monsters) {
            Destroy(monster);
        }
    }
}
