using UnityEngine;

public class Goblin : Monster {
    public override void Init(Vector2 direction, float speed) {
        base.Init(direction, speed);
        moveSpeed = 5f; // Goblin 고유 속도
    }
}
