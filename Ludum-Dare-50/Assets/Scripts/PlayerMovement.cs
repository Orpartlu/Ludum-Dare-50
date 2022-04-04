using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float Speed = 5f;

    [SerializeField]
    private float JumpForce = 12f;

    [SerializeField]
    private float JumpBuffer = 0.1f;

    [FormerlySerializedAs("MAXFallSpeed")]
    [SerializeField]
    private float MaxFallSpeed = -25f;

    [FormerlySerializedAs("groudCheck")]
    [SerializeField]
    private Transform GroundCheck;

    [FormerlySerializedAs("checkRadius")]
    [SerializeField]
    private float CheckRadius;

    [FormerlySerializedAs("whatIsGround")]
    [SerializeField]
    private LayerMask WhatIsGround;

    [SerializeField]
    private Rigidbody2D MyRigidbody2D;

    private readonly Collider2D[] _overlapResults = new Collider2D[2]; //can only detect 2 collisions

    public bool OnGround => _isGrounded;
    
    private bool _doJump;

    private bool _isGrounded;

    private float _lastJumpPressed;

    private float _moveInput;

    private Animator _playerMovementAnimator;

    private bool _canMove = true;

    
    private SpriteRenderer[] _allSpriteRenderers;
    
    private bool HasBufferedJump => _lastJumpPressed + JumpBuffer > Time.time;

    private Action<GameState> _myOnGameStateChangeEvent;
    private bool _isFacingRight;

    private void Awake()
    {
        _doJump = false;
        _lastJumpPressed = float.MinValue;
        _playerMovementAnimator = GetComponentInChildren<Animator>();
        _allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        _canMove = GameManager.Instance.CurrentState == GameState.Playing;
        _myOnGameStateChangeEvent = OnGameStateChange;
        GameManager.Instance.OnGameStateChange += _myOnGameStateChangeEvent;
    }

    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                _canMove = true;
                break;
            case GameState.Dead:
                OnGameStateDeath();
                break;
        }
    }

    private void OnGameStateDeath()
    {
        StopMovement(true);
        
        DeathAnimation();
        
        GameManager.Instance.UpdateGameState(GameState.Reload);
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameStateChange -= _myOnGameStateChangeEvent;
    }

    private void DeathAnimation()
    {
        Sequence sq = DOTween.Sequence();
        sq.Append(gameObject.transform.DORotate(endValue: new Vector3(x: 0, y: 0, z: 180 * (_isFacingRight ? -1 : 1)),
            duration: 0.5f));
        sq.Join(gameObject.transform.DOMoveY(endValue: transform.position.y - 10f, duration: 1f));
        foreach (SpriteRenderer sprite in _allSpriteRenderers)
        {
            if (sprite == null)
            {
                continue;
            }

            sq.Join(sprite.DOFade(endValue: 0, duration: 1f));
        }
    }

    private void Update()
    {
        if (!_canMove)
        {
            return;
        }
        
        DoJump(Input.GetButtonDown("Jump"));

        float horizontalAxis = Input.GetAxisRaw("Horizontal");

        SetFacingDirectionAnimation(horizontalAxis);
        SetIsRunningAnimation(horizontalAxis);

        if (_isGrounded && (_doJump || HasBufferedJump))
        {
            var sp = DOTween.Sequence();
            sp.Append(transform.DOScale(new Vector3(1, 0.5f, 1), 0.02f).From(Vector3.one).OnComplete(
                () =>
                {
                    MyRigidbody2D.velocity = Vector2.up * JumpForce;
                    _isGrounded = false;
                }));
            sp.Append(transform.DOScale(new Vector3(0.5f, 1, 1), 0.2f));
            sp.Append(transform.DOScale(new Vector3(1, 1, 1), 0.5f));
        }
    }

    private void DoJump(bool jumpButtonPressed)
    {
        if (jumpButtonPressed)
        {
            _lastJumpPressed = Time.time;
            _doJump = true;
        }
        else
        {
            _doJump = false;
        }
    }

    private void FixedUpdate()
    {
        _isGrounded = IsGrounded();
        
        if (!_canMove)
        {
            return;
        }
        _moveInput = Input.GetAxisRaw("Horizontal");
        MyRigidbody2D.velocity = new Vector2(x: _moveInput * Speed, y: Mathf.Max(a: MaxFallSpeed, b: MyRigidbody2D.velocity.y));
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircleNonAlloc(point: GroundCheck.position, radius: CheckRadius, results: _overlapResults, layerMask: WhatIsGround) > 0;
    }

    private void OnDrawGizmos()
    {
        // Bounds
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center: GroundCheck.position, radius: CheckRadius);
    }

    private void SetFacingDirectionAnimation(float horizontalAxis)
    {
        bool oldIsFacingRight = _playerMovementAnimator.GetBool("IsFacingRight");
        if (!oldIsFacingRight && horizontalAxis == 0)
        {
            return;
        }
        
        _isFacingRight = horizontalAxis >= 0;
        _playerMovementAnimator.SetBool(name: "IsFacingRight", value: _isFacingRight);
    }

    private void SetIsRunningAnimation(float horizontalAxis)
    {
        bool isHorizontalButtonPressed = horizontalAxis != 0f;
        _playerMovementAnimator.SetBool(name: "IsRunning", value: isHorizontalButtonPressed);
    }

    public void OnFoundExit()
    {
        StopMovement(false);
    }

    private void StopMovement(bool stopPhysics)
    {
        _canMove = false;
        if(MyRigidbody2D != null)
        {
            if (stopPhysics)
            {
                MyRigidbody2D.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                MyRigidbody2D.velocity = Vector3.zero;
            }
        }
    }
}