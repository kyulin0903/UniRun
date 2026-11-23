using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour {
    public Gradient skyColor;
    public Gradient platformTone;
    public Sprite[] toneSprites;
    public SpriteRenderer[] skies;
    public SpriteRenderer[] characters;
    public float cycleDistance = 400f;
    public float characterTint = 0.15f;

    private Color[] characterColors;
    private List<SpriteRenderer> toneRenderers;

    private void Start() {
        characterColors = new Color[characters.Length];
        for (int i = 0; i < characters.Length; i++)
        {
            characterColors[i] = characters[i].color;
        }
    }

    private void Update() {
        float t = (GameManager.instance.distance % cycleDistance) / cycleDistance;
        Color color = skyColor.Evaluate(t);

        for (int i = 0; i < skies.Length; i++)
        {
            skies[i].color = color;
        }

        if (toneRenderers == null)
        {
            toneRenderers = new List<SpriteRenderer>();
            foreach (ScrollingObject scrolling in FindObjectsByType<ScrollingObject>(FindObjectsInactive.Include))
            {
                // 하늘은 위에서 따로 칠하므로 발판 쪽만 모은다
                if (scrolling.GetComponent<BackgroundLoop>() != null)
                    continue;

                foreach (SpriteRenderer original in scrolling.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    toneRenderers.Add(MakeToneLayer(original));
                }
            }
        }

        // 곱하기만으로는 초록이 주황이나 분홍이 될 수 없어서, 같은 모양의 흰색 그림에 색을 입혀 위에 덮는다
        Color tone = platformTone.Evaluate(t);
        for (int i = 0; i < toneRenderers.Count; i++)
        {
            toneRenderers[i].color = tone;
        }

        Color characterColor = Color.Lerp(Color.white, color, characterTint);
        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].color = characterColors[i] * characterColor;
        }
    }

    private SpriteRenderer MakeToneLayer(SpriteRenderer original) {
        GameObject layer = new GameObject("Tone");
        layer.transform.SetParent(original.transform, false);

        SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = FindToneSprite(original.sprite);
        renderer.sortingLayerID = original.sortingLayerID;
        renderer.sortingOrder = original.sortingOrder + 1;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        return renderer;
    }

    private Sprite FindToneSprite(Sprite original) {
        for (int i = 0; i < toneSprites.Length; i++)
        {
            if (toneSprites[i].name == original.name + "_Tone")
                return toneSprites[i];
        }
        return original;
    }
}
