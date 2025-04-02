using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPawn : Pawn
{
    [Header("Movement Settings")]
    [SerializeField] private float moveInputDeadzone = 0.1f;
    
    private Vector2 moveInput;
    private bool canMove = true;

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    protected override void HandleMovement()
    {
        if (!canMove || moveInput.magnitude < moveInputDeadzone) return;

        Vector2Int direction = new Vector2Int(
            Mathf.RoundToInt(moveInput.x),
            Mathf.RoundToInt(moveInput.y)
        );

        if (TryMove(direction))
        {
            canMove = false;
            Invoke(nameof(ResetMovement), 0.2f); // Add small delay between moves
        }
    }

    private void ResetMovement()
    {
        canMove = true;
    }

    private void Update()
    {
        HandleMovement();
    }
}