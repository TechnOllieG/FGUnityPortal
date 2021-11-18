using System.Collections;
using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
	public bool scalePortalToProtectCameraFromClipping = false;
	[Tooltip("The camera that will be protected from clipping when passing through the portal, if null it will choose main camera")]
	public Camera cameraToProtect = null;
	public bool teleportOnFixedUpdate = false;
	
	public bool CurrentlyTravelling { get; private set; } = false;
	
	private Matrix4x4 _currentPortalWorldToLocal;
	private Matrix4x4 _otherPortalLocalToWorld;
	private Transform _tf;
	private Rigidbody _rb;

	[Header("Debug")]
	
	public float previousDot = 0;

	private void Awake()
	{
		_tf = transform;
		_rb = GetComponent<Rigidbody>();
	}

	public void Teleport(Portal currentPortal, Portal destinationPortal, Vector3 pos, Quaternion rotation)
	{
		if (teleportOnFixedUpdate)
		{
			StartCoroutine(TeleportRoutine(currentPortal, destinationPortal, pos, rotation));
			return;
		}
		TravelToPortal(currentPortal, destinationPortal, pos, rotation);
	}

	private IEnumerator TeleportRoutine(Portal currentPortal, Portal destinationPortal, Vector3 pos, Quaternion rotation)
	{
		yield return new WaitForFixedUpdate();
		TravelToPortal(currentPortal, destinationPortal, pos, rotation);
	}

	private void TravelToPortal(Portal currentPortal, Portal destinationPortal, Vector3 pos, Quaternion rotation)
	{
		_tf.position = pos;
		_tf.rotation = rotation;
		destinationPortal.TravelToPortal(this);
		if (_rb)
		{
			Matrix4x4 transformMatrix = destinationPortal.transform.localToWorldMatrix * currentPortal.transform.worldToLocalMatrix;
			_rb.velocity = transformMatrix.MultiplyVector(_rb.velocity);
		}
	}

	public void SetTravelling(Matrix4x4 currentPortalWorldToLocal, Matrix4x4 otherPortalLocalToWorld, float dot)
	{
		CurrentlyTravelling = true;
		_currentPortalWorldToLocal = currentPortalWorldToLocal;
		_otherPortalLocalToWorld = otherPortalLocalToWorld;
		previousDot = dot;
	}

	public void ResetTravelling()
	{
		CurrentlyTravelling = false;
	}

	public bool GetRelativeTransformAtOtherPortal(out Vector3 pos, out Quaternion rotation)
	{
		if (!CurrentlyTravelling)
		{
			pos = Vector3.zero;
			rotation = Quaternion.identity;
			return false;
		}

		Matrix4x4 m = _otherPortalLocalToWorld * _currentPortalWorldToLocal * _tf.localToWorldMatrix;
		pos = m.GetColumn(3);
		rotation = m.rotation;

		return true;
	}
}