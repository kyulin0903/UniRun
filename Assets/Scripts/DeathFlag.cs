using UnityEngine;

public class DeathFlag : MonoBehaviour {
    public GameObject flag;
    public ParticleSystem celebration;
    public AudioClip recordClip;

    private float deathDistance;
    private float playerX;
    private bool passed = false;

    private void Start() {
        deathDistance = PlayerPrefs.GetFloat(GameManager.DeathKey, -1f);
        playerX = GameManager.instance.player1.transform.position.x;
    }

    private void Update() {
        GameManager gm = GameManager.instance;

        // 죽은 거리에 도달하는 순간 깃발이 플레이어 위치에 오도록 거리로 위치를 계산
        float x = playerX + deathDistance - gm.distance;
        transform.position = new Vector3(x, 0f, 0f);

        bool show = gm.isStarted && gm.playerCount == 1 && deathDistance > 0f && x > -14f && x < 14f;
        flag.SetActive(show);

        if (!passed && gm.isStarted && gm.playerCount == 1 && !gm.isGameover && deathDistance > 0f && gm.distance >= deathDistance)
        {
            passed = true;
            celebration.Play();
            AudioSource.PlayClipAtPoint(recordClip, Camera.main.transform.position);
            gm.NewRecord();
        }
    }
}
