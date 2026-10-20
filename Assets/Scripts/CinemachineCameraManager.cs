using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class CinemachineCameraManager : MonoBehaviour
{
    public static CinemachineCameraManager Instance { get; private set; }

    private CinemachineCamera cinemachineCamera;
    private CinemachineConfiner2D confiner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        cinemachineCamera = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        SetupCamera();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupCamera();
    }

    private void SetupCamera()
    {
        if (Player.Instance != null)
        {
            cinemachineCamera.Target.TrackingTarget =
                Player.Instance.transform;
        }

        GameObject cameraLimit =
            GameObject.FindGameObjectWithTag("CameraLimit");

        if (cameraLimit != null)
        {
            Collider2D collider =
                cameraLimit.GetComponent<Collider2D>();

            if (collider != null)
            {
                confiner.BoundingShape2D = collider;
                confiner.InvalidateBoundingShapeCache();
            }
            else
            {
                Debug.LogWarning(
                    "CameraLimit không có Collider2D!"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "Không tìm thấy CameraLimit!"
            );
        }
    }
}