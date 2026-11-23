using UnityEngine;

// 게임 오브젝트를 계속 왼쪽으로 움직이는 스크립트
public class ScrollingObject : MonoBehaviour {
    private void Update() {
        transform.Translate(Vector3.left * GameManager.instance.scrollSpeed * Time.deltaTime);
    }
}
