using UnityEngine;
using UnityEngine.UI;

public class CircleLayoutGroup : LayoutGroup
{
    public float radius = 400; // Radius of the circle
    public float startAngle = -18f; // Starting angle in degrees
    public bool clockwise = true;
    public bool randomizeStartAngle = false; // Optional randomization toggle

    public override void CalculateLayoutInputHorizontal() { }
    public override void CalculateLayoutInputVertical() { }

    public override void SetLayoutHorizontal() { ArrangeChildren(); }
    public override void SetLayoutVertical() { ArrangeChildren(); }

    private void ArrangeChildren()
    {
        if (randomizeStartAngle) { startAngle = Random.Range(0f, 360f); }
        int childCount = transform.childCount;
        if (childCount == 0) return;

        float angleStep = 360f / childCount;
        float currentAngle = startAngle;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = (RectTransform)transform.GetChild(i);
            if (child == null) continue;

            float radians = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radians) * radius;
            float y = Mathf.Sin(radians) * radius;

            child.anchoredPosition = new Vector2(x, y);
            child.localRotation = Quaternion.identity;

            currentAngle += clockwise ? -angleStep : angleStep;
        }
    }
}
