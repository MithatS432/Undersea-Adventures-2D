using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    public Transform cam;
    public float duration = 0.1f;
    public float magnitude = 1f;
    public float interval = 5f;

    public void StartShakeLoop()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeLoop());
    }

    IEnumerator ShakeLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            yield return ShakeRoutine();
        }
    }

    IEnumerator ShakeRoutine()
    {
        if (cam == null) yield break;

        Vector3 original = cam.localPosition;
        float t = 0;

        while (t < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.localPosition = original + new Vector3(x, y, 0);

            t += Time.deltaTime;
            yield return null;
        }

        cam.localPosition = original;
    }
}