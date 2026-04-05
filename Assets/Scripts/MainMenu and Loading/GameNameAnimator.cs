using UnityEngine;
using TMPro;
using System.Collections;

public class GameNameAnimator : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float bounceHeight = 5f;
    public float speed = 0.1f;
    public ScreenShake screenShake;

    void Start()
    {
        if (text == null)
            text = GetComponent<TextMeshProUGUI>();

        StartCoroutine(AnimateText());

        if (screenShake != null)
            screenShake.StartShakeLoop();
    }

    IEnumerator AnimateText()
    {
        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;

        while (true)
        {
            int charIndex = Random.Range(0, textInfo.characterCount);
            if (!textInfo.characterInfo[charIndex].isVisible)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            StartCoroutine(BounceChar(charIndex));
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator BounceChar(int index)
    {
        text.ForceMeshUpdate();
        var textInfo = text.textInfo;

        int matIndex = textInfo.characterInfo[index].materialReferenceIndex;
        int vertIndex = textInfo.characterInfo[index].vertexIndex;

        Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;

        float t = 0;

        while (t < speed)
        {
            float offset = Mathf.Lerp(0, bounceHeight, t / speed);
            for (int j = 0; j < 4; j++)
                vertices[vertIndex + j].y += offset;

            text.UpdateVertexData();
            t += Time.deltaTime;
            yield return null;
        }

        t = 0;

        while (t < speed)
        {
            float offset = Mathf.Lerp(bounceHeight, 0, t / speed);
            for (int j = 0; j < 4; j++)
                vertices[vertIndex + j].y += offset - bounceHeight;

            text.UpdateVertexData();
            t += Time.deltaTime;
            yield return null;
        }
    }
}