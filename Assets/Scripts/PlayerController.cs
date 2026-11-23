using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// PlayerController는 플레이어 캐릭터로서 Player 게임 오브젝트를 제어한다.
public class PlayerController : MonoBehaviour {
   public AudioClip deathClip; // 사망시 재생할 오디오 클립
   public float jumpForce = 700f; // 점프 힘

   public Key jumpKey = Key.Space;
   public Key altJumpKey = Key.UpArrow;

   public Transform visual;
   public ParticleSystem doubleJumpEffect;
   public float squashPower = 0.35f;
   public float squashTime = 0.25f;
   public GameObject label;

   public AudioClip landClip;
   public AudioClip doubleJumpClip;
   public AudioClip hitClip;

   public float jumpBuffer = 0.1f;

   public bool isDead = false;
   public float surviveTime = 0f;
   public string deathCause = "";

   private int jumpCount = 0; // 누적 점프 횟수
   private bool isGrounded = false; // 바닥에 닿았는지 나타냄

   private Rigidbody2D playerRigidbody; // 사용할 리지드바디 컴포넌트
   private Animator animator; // 사용할 애니메이터 컴포넌트
   private AudioSource playerAudio; // 사용할 오디오 소스 컴포넌트

   private float feetY;
   private Coroutine squashRoutine;
   private float jumpPressedTime = -10f;

   private void Start() {
        playerRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<AudioSource>();

        feetY = GetComponent<Collider2D>().bounds.min.y - transform.position.y;
   }

   private void Update() {
       if (isDead)
       {
            // 죽은 몸은 발판과 함께 뒤로 밀려난다
            transform.Translate(Vector3.left * GameManager.instance.scrollSpeed * Time.deltaTime, Space.World);
            return;
       }

       if (!GameManager.instance.isStarted)
            return;

       if (JumpPressed())
       {
            // 점프를 다 쓴 채로 누른 입력은 잠깐 기억해뒀다가 착지하는 순간 쓴다
            if (jumpCount >= 2)
                jumpPressedTime = Time.time;
            else
                TryJump();
       }
       else if (JumpReleased() && playerRigidbody.linearVelocity.y > 0)
       {
            playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, playerRigidbody.linearVelocity.y * 0.5f);
       }
   }

   public bool JumpPressed() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard[jumpKey].wasPressedThisFrame || (altJumpKey != Key.None && keyboard[altJumpKey].wasPressedThisFrame);
   }

   private bool JumpReleased() {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return keyboard[jumpKey].wasReleasedThisFrame || (altJumpKey != Key.None && keyboard[altJumpKey].wasReleasedThisFrame);
   }

   private void Die(string cause) {
        animator.SetTrigger("Die");
        playerAudio.clip = deathClip;
        playerAudio.Play();
        playerAudio.PlayOneShot(hitClip);

        playerRigidbody.linearVelocity = Vector2.zero;
        isDead = true;
        deathCause = cause;
        label.SetActive(false);

        GameManager.instance.OnPlayerDead(this);
   }

    void TryJump()
    {
        if (jumpCount >= 2) return;

        jumpCount++;

        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.AddForce(new Vector2(0, jumpForce));
        playerAudio?.Play();
        animator?.SetBool("Grounded", false);

        if (jumpCount == 2)
        {
            doubleJumpEffect.Play();
            playerAudio.PlayOneShot(doubleJumpClip);
        }
    }

   private void OnTriggerEnter2D(Collider2D other) {
        // 트리거 콜라이더를 가진 장애물과의 충돌을 감지
        if (other.tag == "Dead" && !isDead)
        {
            string cause = other.GetComponentInParent<Platform>() != null ? "SPIKES" : "FELL OFF";
            Die(cause);
        }
   }

   private void OnCollisionEnter2D(Collision2D collision) {
       // 바닥에 닿았음을 감지하는 처리
       if (collision.contacts[0].normal.y > 0.7f)
        {
            isGrounded = true;
            jumpCount = 0;
            animator?.SetBool("Grounded", true);

            float power = squashPower * Mathf.Clamp01(Mathf.Abs(collision.relativeVelocity.y) / 14f);
            if (!isDead && power > 0.05f)
            {
                if (squashRoutine != null)
                    StopCoroutine(squashRoutine);
                squashRoutine = StartCoroutine(Squash(power));
                playerAudio.PlayOneShot(landClip, power / squashPower);
            }

            if (!isDead && Time.time - jumpPressedTime <= jumpBuffer)
            {
                jumpPressedTime = -10f;
                TryJump();
            }
        }
   }

   private void OnCollisionExit2D(Collision2D collision) {
       // 바닥에서 벗어났음을 감지하는 처리
       isGrounded = false;
        animator?.SetBool("Grounded", false);
   }

   private IEnumerator Squash(float power) {
        float t = 0f;
        while (t < squashTime)
        {
            t += Time.deltaTime;
            float wave = Mathf.Cos(t / squashTime * Mathf.PI * 2f) * (1f - t / squashTime) * power;

            visual.localScale = new Vector3(1f + wave, 1f - wave, 1f);
            visual.localPosition = new Vector3(0f, feetY * wave, 0f);
            yield return null;
        }
        visual.localScale = Vector3.one;
        visual.localPosition = Vector3.zero;
   }
}
