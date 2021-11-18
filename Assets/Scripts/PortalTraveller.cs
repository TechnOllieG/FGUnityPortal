using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

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

	[Header("Debug")]
	
	public float previousDot = 0;

	private void Awake()
	{
		_tf = transform;
	}

	public void Teleport(Portal destinationPortal, Vector3 pos, Quaternion rotation)
	{
		if (teleportOnFixedUpdate)
		{
			StartCoroutine(TeleportRoutine(destinationPortal, pos, rotation));
			return;
		}
		TravelToPortal(destinationPortal, pos, rotation);
	}

	private IEnumerator TeleportRoutine(Portal destinationPortal, Vector3 pos, Quaternion rotation)
	{
		yield return new WaitForFixedUpdate();
		TravelToPortal(destinationPortal, pos, rotation);
	}

	private void TravelToPortal(Portal destinationPortal, Vector3 pos, Quaternion rotation)
	{
		_tf.position = pos;
		_tf.rotation = rotation;
		destinationPortal.TravelToPortal(this);
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