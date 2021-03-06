using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
	public float accelerationSpeed;
	public float friction;
	public float jumpImpulse;
	public float gravity;
	public int movementIterativeDepth = 3;
	public float rotationalUprightSpeed = 20f;
	
	public bool lockMouse = true;
	public float sensitivityScale = 1f;
	public float minTilt = -90f;
	public float maxTilt = 90f;
	public Vector3 velocity;

	[Header("Debug")]
	
	[SerializeField] private Vector3 _accumulatedMouseDelta;
	private Transform _cameraTransform;
	private Transform _tf;
	private CapsuleCollider _capsule;
	private Rigidbody _rb;

	private void Start()
	{
		if (lockMouse)
			Cursor.lockState = CursorLockMode.Locked;
		
		_cameraTransform = Camera.main.transform;
		_tf = transform;
		_capsule = GetComponent<CapsuleCollider>();
		_rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		if (_tf.up.y < 1f)
		{
			Vector3 currentUp = _tf.up;
			Vector3 currentForward = _tf.forward;
			
			Vector3 targetUp = Vector3.up;
			Vector3 targetForward = new Vector3(currentForward.x, 0f, currentForward.z).normalized;

			float t = Time.fixedDeltaTime * rotationalUprightSpeed;
			Vector3 newForward = Vector3.Slerp(currentForward, targetForward, t);
			Vector3 newUp = Vector3.Slerp(currentUp, targetUp, t);

			if (newUp.y > 0.999f)
			{
				newUp = targetUp;
				newForward = targetForward;
			}

			_tf.rotation = Quaternion.LookRotation(newForward, newUp);
		}
		
		float forwardInput = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
		float rightInput = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
		bool jumpInput = Input.GetKey(KeyCode.Space);
		
		Vector3 acceleration = (forwardInput * _tf.forward + rightInput * _tf.right).normalized * accelerationSpeed;
		velocity += (acceleration - velocity * friction) * Time.fixedDeltaTime;
		velocity -=  Vector3.up * (Time.fixedDeltaTime * gravity);
		
		if(jumpInput && IsGrounded())
		{
			velocity += _tf.up * jumpImpulse;
		}

		Vector3 deltaToMove = velocity * Time.fixedDeltaTime;
		
		Vector3 pos = _tf.position;
		Vector3 center = pos + _capsule.center;
		
		Vector3 p1 = center + Vector3.up * (_capsule.height * 0.5f) - _capsule.radius * Vector3.up;
		Vector3 p2 = center - Vector3.up * (_capsule.height * 0.5f) + _capsule.radius * Vector3.up;
		
		for(int i = 0; i < movementIterativeDepth; i++)
		{
			float maxDistance = deltaToMove.magnitude;
			bool blockingHit = Physics.CapsuleCast(p1, p2, _capsule.radius, deltaToMove.normalized, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
			float time = Mathf.InverseLerp(0f, maxDistance, hit.distance);
		
			Vector3 currentDelta = deltaToMove * (blockingHit ? time : 1f);
			_tf.position += currentDelta;
			deltaToMove -= currentDelta;
			
			if(deltaToMove.IsNearlyZero())
				break;
			
			if(blockingHit)
			{
				Vector3 depenetrationDelta = Vector3.Dot(hit.normal, velocity) * hit.normal;
				velocity -= depenetrationDelta;

				deltaToMove -= Vector3.Dot(deltaToMove, hit.normal) * hit.normal;
			}
		}
		
		Collider[] colliders = Physics.OverlapCapsule(p1, p2, _capsule.radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

		if (colliders.Length > 1)
		{
			for (int i = 0; i < colliders.Length; i++)
			{
				if (ReferenceEquals(colliders[i], _capsule))
					continue;

				Physics.ComputePenetration(_capsule, pos, _tf.rotation, colliders[i], colliders[i].transform.position,
					colliders[i].transform.rotation, out Vector3 direction, out float distance);

				_tf.position += direction * distance;
			}
		}
	}

	private bool IsGrounded()
	{
		Vector3 pos = _tf.position;
		return Physics.Raycast(pos, Vector3.down, _capsule.height * 0.5f + 0.0001f);
	}

	private void Update()
	{
		Vector3 mouseDelta = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		_accumulatedMouseDelta += mouseDelta * (Time.deltaTime * sensitivityScale);
		
		if (_accumulatedMouseDelta.x < 0f) {
			_accumulatedMouseDelta.x += 360f;
		}
		else if (_accumulatedMouseDelta.x >= 360f) {
			_accumulatedMouseDelta.x -= 360f;
		}

		_accumulatedMouseDelta.y = Mathf.Clamp(_accumulatedMouseDelta.y, minTilt, maxTilt);
		_cameraTransform.localRotation = Quaternion.AngleAxis(-_accumulatedMouseDelta.y, Vector3.right);
		_tf.rotation = Quaternion.AngleAxis(mouseDelta.x * Time.deltaTime * sensitivityScale, Vector3.up) * _tf.rotation;
	}
}
