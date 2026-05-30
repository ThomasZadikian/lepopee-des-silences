using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D _rb;
    private Vector2 _movement;
    private Animator _animator;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        _movement = Vector2.zero;

        if (Keyboard.current.zKey.isPressed || Keyboard.current.upArrowKey.isPressed) _movement.y = 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) _movement.y = -1;
        if (Keyboard.current.qKey.isPressed || Keyboard.current.leftArrowKey.isPressed) _movement.x = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) _movement.x = 1;

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _movement.normalized * moveSpeed;
    }

    private void UpdateAnimation()
    {
        if (_movement == Vector2.zero)
            _animator.Play("idel_down");
        else if (_movement.y < 0)
            _animator.Play("walk_down");
        else if (_movement.y > 0)
            _animator.Play("walk_up");
        else if (_movement.x < 0)
            _animator.Play("walk_left");
        else if (_movement.x > 0)
            _animator.Play("walk_right");
    }
}