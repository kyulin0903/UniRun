using UnityEngine;

public class CameraEffect : MonoBehaviour {
    public float shakeTime = 0.3f;
    public float shakePower = 0.4f;
    public float zoomSize = 2.8f;
    public float skyHalfHeight = 5.12f;

    private Camera cam;
    private Vector3 homePosition;
    private float homeSize;

    private float shakeLeft = 0f;
    private Vector3 zoomTarget;
    private float zoomWeight = 0f;

    private void Awake() {
        cam = GetComponent<Camera>();
        homePosition = transform.position;
        homeSize = cam.orthographicSize;
    }

    public void Shake() {
        shakeLeft = shakeTime;
    }

    // weight가 0이면 원래 화면, 1이면 target을 중심으로 완전히 줌인
    public void SetZoom(Vector3 target, float weight) {
        zoomTarget = new Vector3(target.x, target.y, homePosition.z);
        zoomWeight = weight;
    }

    private void LateUpdate() {
        Vector3 pos = Vector3.Lerp(homePosition, zoomTarget, zoomWeight);
        cam.orthographicSize = Mathf.Lerp(homeSize, zoomSize, zoomWeight);

        // 줌인한 화면이 하늘 배경의 위아래 끝을 넘지 않도록 막는다
        float limit = Mathf.Max(0f, skyHalfHeight - cam.orthographicSize);
        pos.y = Mathf.Clamp(pos.y, -limit, limit);

        if (shakeLeft > 0f)
        {
            shakeLeft -= Time.unscaledDeltaTime;
            pos += (Vector3)(Random.insideUnitCircle * shakePower * Mathf.Clamp01(shakeLeft / shakeTime));
        }

        transform.position = pos;
    }
}
