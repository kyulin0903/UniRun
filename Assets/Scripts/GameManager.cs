using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 게임 오버 상태를 표현하고, 게임 점수와 UI를 관리하는 게임 매니저
// 씬에는 단 하나의 게임 매니저만 존재할 수 있다.
public class GameManager : MonoBehaviour {
    public static GameManager instance; // 싱글톤을 할당할 전역 변수
    public const string DeathKey = "LastDeathDistance";

    public bool isGameover = false; // 게임 오버 상태
    public bool isStarted = false;
    public int playerCount = 1;

    public Text scoreText; // 점수를 출력할 UI 텍스트
    public Text distanceText;
    public Text resultText;
    public Text recordText;
    public GameObject gameoverUI; // 게임 오버시 활성화 할 UI 게임 오브젝트
    public GameObject modePanel;

    public PlayerController player1;
    public PlayerController player2;
    public CameraEffect cameraEffect;
    public ReplayRecorder replay;

    public float startSpeed = 7f;
    public float maxSpeed = 14f;
    public float acceleration = 0.05f;

    public RawImage frozenView;

    public float hitStopTime = 0.1f;
    public float restartLock = 0.7f;
    public float recordShowTime = 2f;

    public float scrollSpeed = 0f;
    public float distance = 0f;

    private static int nextMode = 0;

    private int score = 0; // 게임 점수
    private float elapsed = 0f;
    private Coroutine deathRoutine;
    private RenderTexture frozenTexture;
    private float restartTime = 0f;
    private bool recordBroken = false;


    // 게임 시작과 동시에 싱글톤을 구성
    void Awake() {
        // 싱글톤 변수 instance가 비어있는가?
        if (instance == null)
        {
            // instance가 비어있다면(null) 그곳에 자기 자신을 할당
            instance = this;
        }
        else
        {
            // 씬에 두개 이상의 GameManager 오브젝트가 존재한다는 의미.
            // 싱글톤 오브젝트는 하나만 존재해야 하므로 자신의 게임 오브젝트를 파괴
            Debug.LogWarning("씬에 두개 이상의 게임 매니저가 존재합니다!");
            Destroy(gameObject);
        }
    }

    void Start() {
        if (nextMode != 0)
        {
            StartGame(nextMode);
            nextMode = 0;
        }
    }

    void Update() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!isStarted)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
                StartGame(1);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                StartGame(2);
            return;
        }

        if (isGameover)
        {
            scrollSpeed = 0f;

            if (!gameoverUI.activeSelf || Time.unscaledTime < restartTime)
                return;

            bool jumped = player1.JumpPressed() || (playerCount == 2 && player2.JumpPressed());

            if (jumped)
            {
                Restart(playerCount);
            }
            else if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Restart(1);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Restart(2);
            }
            return;
        }

        elapsed += Time.deltaTime;
        scrollSpeed = Mathf.Min(maxSpeed, startSpeed + acceleration * elapsed);
        distance += scrollSpeed * Time.deltaTime;
        distanceText.text = (int)distance + " m";
    }

    void StartGame(int mode) {
        playerCount = mode;
        isStarted = true;
        modePanel.SetActive(false);

        if (mode == 2)
        {
            player1.altJumpKey = Key.None;
            player2.gameObject.SetActive(true);
            player1.label.SetActive(true);
            player2.label.SetActive(true);

            scoreText.gameObject.SetActive(false);
            Physics2D.IgnoreCollision(player1.GetComponent<Collider2D>(), player2.GetComponent<Collider2D>());
        }
    }

    void Restart(int mode) {
        nextMode = mode;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 점수를 증가시키는 메서드
    public void AddScore(int newScore) {
        if (!isGameover)
        {
            score += newScore;
            scoreText.text = "Score : " + score;
        }
    }

    public void NewRecord() {
        recordBroken = true;
        StartCoroutine(ShowRecordText());
    }

    private IEnumerator ShowRecordText() {
        recordText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(recordShowTime);
        recordText.gameObject.SetActive(false);
    }

    public void OnPlayerDead(PlayerController dead) {
        dead.surviveTime = elapsed;
        cameraEffect.Shake();

        bool allDead = player1.isDead && (playerCount == 1 || player2.isDead);

        if (allDead)
        {
            isGameover = true;

            if (playerCount == 1)
            {
                PlayerPrefs.SetFloat(DeathKey, distance);
                PlayerPrefs.Save();
            }
        }

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);
        deathRoutine = StartCoroutine(DeathRoutine(dead, allDead));
    }

    private IEnumerator DeathRoutine(PlayerController last, bool allDead) {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopTime);
        Time.timeScale = 1f;

        if (!allDead)
            yield break;

        FreezeScreen();
        recordText.gameObject.SetActive(false);
        ShowResult();
        gameoverUI.SetActive(true);
        restartTime = Time.unscaledTime + restartLock;

        // 작은 창에 마지막 장면이 그대로 남도록 리플레이가 끝나도 시간은 멈춘 채로 둔다
        yield return StartCoroutine(replay.Play(last));
    }

    private void FreezeScreen() {
        Camera cam = cameraEffect.GetComponent<Camera>();
        frozenTexture = new RenderTexture(Mathf.Max(Screen.width, 16), Mathf.Max(Screen.height, 16), 24);

        cam.targetTexture = frozenTexture;
        cam.Render();
        cam.targetTexture = null;

        frozenView.texture = frozenTexture;
        frozenView.gameObject.SetActive(true);
    }

    private void OnDestroy() {
        if (frozenTexture != null)
            frozenTexture.Release();
    }

    private void ShowResult() {
        if (playerCount == 1)
        {
            string text = "Distance : " + (int)distance + " m";
            if (recordBroken)
                text += "\n<size=18><color=#ffd84d>NEW RECORD!</color></size>";

            text += "\n<size=10> \n</size><size=18>" + DeathLine(player1) + "</size>";
            resultText.text = text;
            return;
        }

        string winner = "DRAW";
        if (!Mathf.Approximately(player1.surviveTime, player2.surviveTime))
        {
            winner = player1.surviveTime > player2.surviveTime ? "P1 WIN!" : "P2 WIN!";
        }

        resultText.text = winner + "\nP1 " + player1.surviveTime.ToString("F1") + " sec   P2 " + player2.surviveTime.ToString("F1") + " sec";
    }

    private string DeathLine(PlayerController player) {
        return player.deathCause == "SPIKES" ? "You hugged a spike." : "You forgot how to fly.";
    }
}
