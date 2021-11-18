using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Portal : MonoBehaviour
{
	public Portal otherPortal;
	public Vector3 portalScale = new Vector3(3f, 3f, 0.01f);
	public bool usePortalEdgeColliders = true;
	public float portalEdgeColliderWidth = 0.01f;

	public List<BoxCollider> portalEdgeColliders = new List<BoxCollider>();
	
	public MeshRenderer PortalSurface { get; private set; }
	
	public Camera Cam { get; private set; }

	private RenderTexture _renderTexture;
	private Portal _previousPortal;
	private Transform _otherPortalTransform;
	private Transform _portalCamTransform;
	private Camera _mainCam;
	private Transform _mainCameraTf;
	private Transform _tf;
	private List<PortalTraveller> _trackedTravellers = new List<PortalTraveller>();

	private void OnValidate()
	{
		GetComponent<BoxCollider>().size = portalScale;
		GetComponentInChildren<MeshRenderer>().transform.localScale = portalScale;
		
		if (portalEdgeColliders.Count != 4)
		{
			List<BoxCollider> colliders = GetComponents<BoxCollider>().ToList();
			for (int i = 0; i < colliders.Count; i++)
			{
				if (colliders[i].isTrigger)
				{
					colliders.RemoveAt(i);
					--i;
				}
			}

			if (colliders.Count == 4)
				portalEdgeColliders = colliders;
			else
				return;
		}

		if (usePortalEdgeColliders)
		{
			Vector2Int[] directions = new[] {new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)};
			for(int i = 0; i < portalEdgeColliders.Count; i++)
			{
				portalEdgeColliders[i].enabled = true;
				portalEdgeColliders[i].center = new Vector3(directions[i].x * portalScale.x * 0.5f, directions[i].y * portalScale.y * 0.5f, 0f);
				portalEdgeColliders[i].size =
					new Vector3(
						directions[i].x == 0 ? portalScale.x : portalEdgeColliderWidth,
						directions[i].y == 0 ? portalScale.y : portalEdgeColliderWidth, 
						portalEdgeColliderWidth);
			}
		}
		else
		{
			for (int i = 0; i < portalEdgeColliders.Count; i++)
			{
				portalEdgeColliders[i].enabled = false;
			}
		}
	}

	private void Awake()
	{
		_mainCam = Camera.main;
		_mainCameraTf = _mainCam.transform;
		_tf = transform;

		PortalSurface = GetComponentInChildren<MeshRenderer>();
		Cam = GetComponentInChildren<Camera>();
		if (Cam == null)
		{
			Debug.Log("Please add a childed camera to this portal object before you can use it");
			enabled = false;
			return;
		}

		Cam.enabled = false;

		Resolution res = Screen.currentResolution;
		_renderTexture = new RenderTexture(res.width, res.height, 16);
	}

	private void OnDestroy()
	{
		if (!Application.isPlaying)
			return;
		
		_renderTexture.Release();
	}

	private void OnTriggerEnter(Collider other)
	{
		PortalTraveller traveller = other.GetComponent<PortalTraveller>();

		if (!traveller)
			return;

		_trackedTravellers.Add(traveller);
		traveller.SetTravelling(_tf.worldToLocalMatrix, _otherPortalTransform.localToWorldMatrix,
			Vector3.Dot(_tf.forward, (traveller.transform.position - _tf.position).normalized));
	}

	private void OnTriggerExit(Collider other)
	{
		PortalTraveller traveller = other.GetComponent<PortalTraveller>();

		if (!traveller)
			return;

		PortalSurface.transform.localScale = portalScale;
		PortalSurface.transform.localPosition = Vector3.zero;

		if (!_trackedTravellers.Contains(traveller))
			return;
		
		_trackedTravellers.Remove(traveller);
		traveller.ResetTravelling();
	}

	private void Update()
	{
		if (!otherPortal)
			return;

		if (otherPortal != _previousPortal)
		{
			_previousPortal = otherPortal;
			_otherPortalTransform = otherPortal.transform;
			Cam.targetTexture = _renderTexture;
			otherPortal.PortalSurface.material.SetTexture(Shader.PropertyToID("_MainTex"), _renderTexture);
			_portalCamTransform = Cam.transform;
		}

		for (int i = 0; i < _trackedTravellers.Count; i++)
		{
			PortalTraveller traveller = _trackedTravellers[i];

			if (traveller.scalePortalToProtectCameraFromClipping)
			{
				Camera cam = traveller.cameraToProtect ? traveller.cameraToProtect : _mainCam;
				ProtectScreenFromClipping(cam);
			}
			
			Transform travellerTransform = traveller.transform;
			float dot = Vector3.Dot(_tf.forward, (travellerTransform.position - _tf.position).normalized);
			if (Math.Sign(dot) != Math.Sign(traveller.previousDot))
			{
				Matrix4x4 travellerNewWorldMatrix = _otherPortalTransform.localToWorldMatrix * _tf.worldToLocalMatrix *
				               travellerTransform.localToWorldMatrix;
				traveller.Teleport(this, otherPortal, travellerNewWorldMatrix.GetColumn(3), travellerNewWorldMatrix.rotation);
				_trackedTravellers.Remove(traveller);
				traveller.ResetTravelling();
				--i;
			}
			else
			{
				traveller.previousDot = dot;
			}
		}

		if (!IsVisibleFrom(otherPortal.PortalSurface, _mainCam))
			return;

		PortalSurface.enabled = false;
		
		Matrix4x4 m = _tf.localToWorldMatrix * _otherPortalTransform.worldToLocalMatrix * _mainCameraTf.localToWorldMatrix;
		_portalCamTransform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
		
		Cam.Render();
		PortalSurface.enabled = true;
	}

	public void TravelToPortal(PortalTraveller traveller)
	{
		if(traveller.scalePortalToProtectCameraFromClipping)
			ProtectScreenFromClipping(_mainCam);
	}
	
	private bool IsVisibleFrom(Renderer rend, Camera cam)
	{
		Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
		return GeometryUtility.TestPlanesAABB(frustumPlanes, rend.bounds);
	}

	private void ProtectScreenFromClipping(Camera camToProtect)
	{
		Transform portalSurfaceTransform = PortalSurface.transform;
		
		Vector3 portalPos = _tf.position;
		Vector3 portalForward = _tf.forward;
		float nearClipPlane = camToProtect.nearClipPlane;
		
		float halfHeight = Mathf.Tan(camToProtect.fieldOfView * 0.5f * Mathf.Deg2Rad) * nearClipPlane;
		float halfWidth = halfHeight * camToProtect.aspect;
		float distanceToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, nearClipPlane).magnitude;

		bool facingSameDirection = Vector3.Dot(portalForward, portalPos - camToProtect.transform.position) > 0;
		Vector3 currentScale = portalSurfaceTransform.localScale;
		portalSurfaceTransform.localScale = new Vector3(currentScale.x, currentScale.y, distanceToNearClipPlaneCorner);
		portalSurfaceTransform.localPosition = portalForward * (distanceToNearClipPlaneCorner * (facingSameDirection ? 0.5f : -0.5f));
	}
}