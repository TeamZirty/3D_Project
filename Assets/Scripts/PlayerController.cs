/*
 * Desc: 기본 캐릭터 컨트롤러 스크립트 (이동, 점프, 중력 처리)
 * 이 스크립트는 CharacterController를 사용하여 플레이어 캐릭터를 제어합니다.
 * WASD 키로 이동하고, Space 바로 점프합니다.
 * Unity의 Input System 사용을 전제로 합니다.
 */
using UnityEngine;
using UnityEngine.InputSystem; // Input System 사용을 위해 추가

// RequireComponent : 스크립트를 컴포넌트에 추가할때 해당 컴포넌트가 없으면 게임 오브젝트에 추가해줌 
[RequireComponent(typeof(CharacterController))] 
[RequireComponent(typeof(PlayerInput))]      

public class PlayerController : MonoBehaviour
{
    // 이동 관련 변수
    [Header("Movement Settings")] 
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 720.0f; // 캐릭터가 이동 방향으로 회전하는 속도 (초당 각도)

    // 점프 및 중력 관련 변수
    [Header("Jump & Gravity Settings")]
    public float jumpHeight = 1.2f; 
    public float gravityMultiplier = 2.0f; // 기본 중력 값에 곱해줄 값 (더 묵직한 느낌을 위해)
    private float _gravityValue;

    // 내부 참조 및 상태 변수
    private CharacterController _controller;
    private PlayerInput _playerInput;
    private Vector3 _playerVelocity;    // 캐릭터의 현재 수직 속도 (중력 및 점프)
    private bool _groundedPlayer;       // 캐릭터가 바닥에 닿아있는지 여부

    // Input Actions 참조
    private InputAction _moveAction;
    private InputAction _jumpAction;

    private Vector2 _currentMoveInput;
    private bool _jumpPressedThisFrame = false;

    private Transform _mainCameraTransform; // 메인 카메라의 Transform 참조

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();

        // 메인 카메라 찾기
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("Main Camera not found. Please ensure a camera is tagged as 'MainCamera'.");
        }


        // Input Actions 에셋에서 정의한 ActionMap "Player"의 Action들 할당
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];

        // 중력 값 계산
        _gravityValue = Physics.gravity.y * gravityMultiplier;
    }

    void Update()
    {
        HandleInput();      // 입력 값 읽기
        ApplyGravity();     // 중력 및 점프 처리
        MovePlayer();       // 이동 및 회전 처리
    }


    //vector2입력읽어오고, 점프입력을 매 프레임 확인
    void HandleInput()
    {
        _currentMoveInput = _moveAction.ReadValue<Vector2>();
        if (_jumpAction.triggered)
        {
            _jumpPressedThisFrame = true;
        }
    }

    void ApplyGravity()
    {
        _groundedPlayer = _controller.isGrounded;

        // 캐릭터가 땅에 닿아있고, 수직 속도가 0 또는 음수(떨어지고 있지 않음)라면 수직 속도 초기화
        if (_groundedPlayer && _playerVelocity.y < 0)
        {
            _playerVelocity.y = -0.5f; // 땅에 확실히 붙어있도록 아주 작은 음수 값 적용 (선택적)
        }

        // 점프 처리: 땅에 있고, 점프 키가 눌렸다면
        if (_jumpPressedThisFrame && _groundedPlayer)
        {
            // 점프에 필요한 초기 수직 속도 계산: v = sqrt(h * -2 * g)
            // 여기서 g는 이미 음수이므로 -2 * _gravityValue 대신 -2 / _gravityValue 사용 또는 _gravityValue의 절대값 사용
            // 또는 Physics.gravity.y를 직접 사용: Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y * gravityMultiplier);
            // 좀 더 직관적으로: _playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * _gravityValue); (단, _gravityValue가 양수여야 함)
            // 아래는 _gravityValue가 음수임을 가정하고 수정한 식입니다.
            _playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * (_gravityValue / gravityMultiplier)); // gravityMultiplier로 나눈 원본 중력값을 사용
        }
        _jumpPressedThisFrame = false; // 점프 입력은 매 프레임 처리 후 리셋

        // 지속적으로 중력 적용 (시간에 따라 속도 증가)
        _playerVelocity.y += _gravityValue * Time.deltaTime;

        // 최종 수직 이동 적용
        _controller.Move(_playerVelocity * Time.deltaTime);
    }

    void MovePlayer()
    {
        // 입력 값 (x, y)를 3D 이동 벡터로 변환 (카메라 기준)
        Vector3 moveInputDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);

        // 카메라가 바라보는 방향을 기준으로 이동 방향 계산
        if (_mainCameraTransform != null)
        {
            // 카메라의 정면 방향 (y축은 무시하고 정규화)
            Vector3 cameraForward = Vector3.Scale(_mainCameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            // 카메라의 오른쪽 방향 (y축은 무시하고 정규화)
            Vector3 cameraRight = Vector3.Scale(_mainCameraTransform.right, new Vector3(1, 0, 1)).normalized;

            // 최종 이동 방향 = (카메라 정면 * 입력 Z) + (카메라 오른쪽 * 입력 X)
            moveInputDirection = (cameraForward * moveInputDirection.z + cameraRight * moveInputDirection.x);
        }
        else // 카메라가 없으면 월드 축 기준으로 이동
        {
            moveInputDirection = new Vector3(_currentMoveInput.x, 0, _currentMoveInput.y);
        }


        if (moveInputDirection.magnitude >= 0.1f) // 약간의 Deadzone을 두어 미세한 입력 무시
        {
            // 이동 적용
            _controller.Move(moveInputDirection.normalized * moveSpeed * Time.deltaTime);

            // 캐릭터가 이동 방향을 바라보도록 부드럽게 회전
            Quaternion targetRotation = Quaternion.LookRotation(moveInputDirection.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}