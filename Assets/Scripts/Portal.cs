using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Portal : MonoBehaviour
{
	public Portal otherPortal;
	
	public Camera Cam { get; private set; }

	private RenderTexture _renderTexture;
	private Portal _previousPortal;
	private Transform _otherPortalTransform;
	private Transform _otherPortalCamTransform;
	private Transform _mainCameraTf;
	private Transform _tf;
	private MeshRenderer _portalSurface;

	private void Awake()
	{
		_mainCameraTf = Camera.main.transform;
		_tf = transform;
		
		_portalSurface = GetComponent<MeshRenderer>();
		Cam = GetComponentInChildren<Camera>();
		if (Cam == null)
		{
			Debug.Log("Please add a childed camera to this portal object before you can use it");
			enabled = false;
			return;
		}

		Cam.enabled = false;

		Resolution res = Screen.currentResolution;
		_renderTexture = new RenderTexture(res.width, res.height, 0);
		_portalSurface.material.mainTexture = _renderTexture;
	}

	private void OnDestroy()
	{
		if (!Application.isPlaying)
			return;
		
		_renderTexture.Release();
	}

	private void Update()
	{
		if (!_portalSurface.isVisible || !otherPortal)
			return;

		if (otherPortal != _previousPortal)
		{
			_previousPortal = otherPortal;
			_otherPortalTransform = otherPortal.transform;
			otherPortal.Cam.targetTexture = _renderTexture;
			_otherPortalCamTransform = otherPortal.Cam.transform;
		}

		Transform mainTf = _mainCameraTf;
		Vector3 mainPos = mainTf.position;
		Vector3 forward = mainTf.forward;
		Vector3 up = mainTf.up;

		Matrix4x4 toLocalMatrix = _tf.worldToLocalMatrix;
		Vector3 mainLocalPos = toLocalMatrix.MultiplyPoint3x4(mainPos);
		Vector3 localForward = toLocalMatrix.MultiplyVector(forward);
		Vector3 localUp = toLocalMatrix.MultiplyVector(up);

		Matrix4x4 toWorldMatrix = _otherPortalTransform.localToWorldMatrix;
		_otherPortalCamTransform.position = toWorldMatrix.MultiplyPoint3x4(mainLocalPos);
		_otherPortalCamTransform.rotation = Quaternion.LookRotation(toWorldMatrix.MultiplyVector(localForward),
			toWorldMatrix.MultiplyVector(localUp));

		otherPortal.Cam.Render();
	}
}