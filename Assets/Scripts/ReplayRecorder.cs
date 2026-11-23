using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReplayRecorder : MonoBehaviour {
    public CameraEffect cameraEffect;
    public Camera replayCamera;
    public GameObject replayWindow;
    public RawImage replayImage;
    public float recordSeconds = 1.25f;
    public float playSpeed = 0.5f;

    private class Frame {
        public float time;
        public Vector3[] positions;
        public Vector3[] scales;
        public bool[] actives;
        public Sprite[] sprites;
    }

    private List<Transform> targets = new List<Transform>();
    private List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private List<Animator> animators = new List<Animator>();
    private Queue<Frame> frames = new Queue<Frame>();
    private bool recording = true;

    private void Awake() {
        RenderTexture texture = new RenderTexture(640, 360, 24);
        replayCamera.targetTexture = texture;
        replayImage.texture = texture;

        replayCamera.enabled = false;
        replayWindow.SetActive(false);
    }

    private void LateUpdate() {
        if (!recording || !GameManager.instance.isStarted || Time.deltaTime == 0f)
            return;

        if (targets.Count == 0)
            CollectTargets();

        while (frames.Count > 0 && Time.time - frames.Peek().time > recordSeconds)
        {
            frames.Dequeue();
        }

        Frame frame = new Frame();
        frame.time = Time.time;
        frame.positions = new Vector3[targets.Count];
        frame.scales = new Vector3[targets.Count];
        frame.actives = new bool[targets.Count];
        frame.sprites = new Sprite[renderers.Count];

        for (int i = 0; i < targets.Count; i++)
        {
            frame.positions[i] = targets[i].position;
            frame.scales[i] = targets[i].localScale;
            frame.actives[i] = targets[i].gameObject.activeSelf;
        }
        for (int i = 0; i < renderers.Count; i++)
        {
            frame.sprites[i] = renderers[i].sprite;
        }
        frames.Enqueue(frame);
    }

    private void CollectTargets() {
        ScrollingObject[] scrollings = FindObjectsByType<ScrollingObject>(FindObjectsInactive.Include);
        foreach (ScrollingObject scrolling in scrollings)
        {
            targets.Add(scrolling.transform);

            Platform platform = scrolling.GetComponent<Platform>();
            if (platform != null)
            {
                foreach (GameObject obstacle in platform.obstacles)
                {
                    targets.Add(obstacle.transform);
                }
            }
        }

        AddPlayer(GameManager.instance.player1);
        if (GameManager.instance.playerCount == 2)
            AddPlayer(GameManager.instance.player2);
    }

    private void AddPlayer(PlayerController player) {
        targets.Add(player.transform);
        targets.Add(player.visual);
        renderers.Add(player.visual.GetComponent<SpriteRenderer>());
        animators.Add(player.GetComponent<Animator>());
    }

    public IEnumerator Play(PlayerController focus) {
        recording = false;
        Time.timeScale = 0f;

        Frame[] list = frames.ToArray();
        if (list.Length < 2)
            yield break;

        replayCamera.enabled = true;
        replayWindow.SetActive(true);

        foreach (Animator animator in animators)
        {
            animator.enabled = false;
        }
        foreach (ParticleSystem particle in FindObjectsByType<ParticleSystem>())
        {
            particle.Clear();
        }

        int focusIndex = targets.IndexOf(focus.transform);
        float start = list[0].time;
        float end = list[list.Length - 1].time;
        float playTime = start;
        int index = 0;

        while (playTime < end)
        {
            playTime += Time.unscaledDeltaTime * playSpeed;

            while (index < list.Length - 2 && list[index + 1].time <= playTime)
            {
                index++;
            }

            Frame a = list[index];
            Frame b = list[index + 1];
            float t = Mathf.Clamp01(Mathf.InverseLerp(a.time, b.time, playTime));

            Apply(a, b, t);

            Vector3 focusPos = Vector3.Lerp(a.positions[focusIndex], b.positions[focusIndex], t);
            cameraEffect.SetZoom(focusPos, Mathf.SmoothStep(0f, 1f, (playTime - start) / (end - start)));

            yield return null;
        }
    }

    private void Apply(Frame a, Frame b, float t) {
        for (int i = 0; i < targets.Count; i++)
        {
            Vector3 pos = a.positions[i];

            // 발판이 재배치되며 순간이동한 구간은 보간하지 않는다
            if ((b.positions[i] - pos).sqrMagnitude < 25f)
                pos = Vector3.Lerp(pos, b.positions[i], t);

            targets[i].position = pos;
            targets[i].localScale = Vector3.Lerp(a.scales[i], b.scales[i], t);
            targets[i].gameObject.SetActive(a.actives[i]);
        }
        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].sprite = a.sprites[i];
        }
    }
}
