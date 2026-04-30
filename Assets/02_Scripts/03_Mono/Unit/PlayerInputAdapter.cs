using UnityEngine;
using UnityEngine.InputSystem;

namespace PublicFramework
{
    /// <summary>
    /// New Input System 기반 플레이어 컨트롤 어댑터. UnitController 와 같은 GameObject 에 부착.
    /// 책임: Move 액션(Vector2) → transform 이동, Skill 액션(Button) → UnitController.CastSkill.
    /// InputActionReference 비어있어도 안전 (해당 입력만 비활성).
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    [DisallowMultipleComponent]
    public class PlayerInputAdapter : MonoBehaviour
    {
        [Header("입력 액션 (선택)")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _skillAction;

        [Header("스킬 시전")]
        [SerializeField] private SkillData _skill;

        [Header("이동")]
        [SerializeField] private float _moveSpeed = 5f;

        private UnitController _controller;
        private Vector2 _moveInput;

        public Vector2 MoveInput => _moveInput;

        private void Awake()
        {
            _controller = GetComponent<UnitController>();
        }

        private void OnEnable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.performed += OnMovePerformed;
                _moveAction.action.canceled += OnMoveCanceled;
                _moveAction.action.Enable();
            }
            if (_skillAction != null && _skillAction.action != null)
            {
                _skillAction.action.performed += OnSkillPerformed;
                _skillAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.performed -= OnMovePerformed;
                _moveAction.action.canceled -= OnMoveCanceled;
                _moveAction.action.Disable();
            }
            if (_skillAction != null && _skillAction.action != null)
            {
                _skillAction.action.performed -= OnSkillPerformed;
                _skillAction.action.Disable();
            }
            _moveInput = Vector2.zero;
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;

        private void OnSkillPerformed(InputAction.CallbackContext ctx)
        {
            if (_skill == null || string.IsNullOrEmpty(_skill.SkillId)) return;
            if (_controller == null || !_controller.IsAlive) return;

            string targetId = FindNearestEnemyInstanceId();
            _controller.CastSkill(_skill.SkillId, targetId);
        }

        private string FindNearestEnemyInstanceId()
        {
            UnitController[] all = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            UnitController nearest = null;
            float nearestSqr = float.MaxValue;
            Vector3 origin = transform.position;

            foreach (UnitController u in all)
            {
                if (u == _controller) continue;
                if (!u.IsAlive) continue;
                float sqr = (u.transform.position - origin).sqrMagnitude;
                if (sqr >= nearestSqr) continue;
                nearestSqr = sqr;
                nearest = u;
            }

            return nearest != null ? nearest.InstanceId : null;
        }

        private void Update()
        {
            if (_controller == null || !_controller.IsAlive) return;
            if (_moveInput.sqrMagnitude < 0.0001f) return;

            Vector3 delta = new Vector3(_moveInput.x, _moveInput.y, 0f) * (_moveSpeed * Time.deltaTime);
            transform.Translate(delta, Space.World);
        }
    }
}
