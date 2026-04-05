using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class Tile : MonoBehaviour
{
    public int x;
    public int y;
    public int type;

    public bool isSpecial;
    public bool isHorizontal;
    public bool isVertical;
    public bool isSmallBomb;
    public bool isLargeBomb;
    public bool isColorBomb;

    public bool isUniversal = false;

    private Vector2 startPos;
    private bool isDragging;

    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsProcessing)
        {
            isDragging = false;
            return;
        }

        HandlePointer();
    }

    void HandlePointer()
    {
        if (IsPointerDown(out Vector2 screenPos))
        {
            if (IsPointerOnMe(screenPos))
            {
                if (PuzzleManager.Instance.IsHammerMode)
                {
                    PuzzleManager.Instance.UseHammerOn(this);
                    return;
                }

                startPos = screenPos;
                isDragging = true;
            }
        }

        if (IsPointerUp(out Vector2 endPos) && isDragging)
        {
            Vector2 delta = endPos - startPos;

            if (delta.magnitude > 50f)
            {
                Vector2 dir;

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    dir = delta.x > 0 ? Vector2.right : Vector2.left;
                else
                    dir = delta.y > 0 ? Vector2.up : Vector2.down;

                PuzzleManager.Instance.TrySwap(this, dir);
            }

            isDragging = false;
        }
    }

    bool IsPointerDown(out Vector2 pos)
    {
        pos = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    bool IsPointerUp(out Vector2 pos)
    {
        pos = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    bool IsPointerOnMe(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}