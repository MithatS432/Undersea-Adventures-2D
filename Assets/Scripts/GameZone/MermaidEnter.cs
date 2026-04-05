using UnityEngine;
using System.Collections;

public class MermaidEnter : MonoBehaviour
{
    public GameObject[] mermaidObjects;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        foreach (var obj in mermaidObjects)
        {
            obj.SetActive(true);
        }

        StartCoroutine(DisableAfterTime());
    }

    private IEnumerator DisableAfterTime()
    {
        yield return new WaitForSeconds(2f);

        foreach (var obj in mermaidObjects)
        {
            obj.SetActive(false);
        }
    }
    public void WinTrigger()
    {
        anim.SetTrigger("Win");
    }
    public void LoseTrigger()
    {
        anim.SetTrigger("Lose");
    }
    public void LowMovesTrigger()
    {
        anim.SetTrigger("LowMoves");
    }
}