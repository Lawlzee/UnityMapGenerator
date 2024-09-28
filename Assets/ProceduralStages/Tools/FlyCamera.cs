using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    public class FlyCamera : MonoBehaviour
    {
        public float acceleration = 300; // how fast you accelerate
        public float accSprintMultiplier = 8; // how much faster you go when "sprinting"
        public float lookSensitivity = 2; // mouse look sensitivity
        public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input
        public bool focusOnEnable = true; // whether or not to focus and lock cursor immediately on enable
        [Range(0, 180)]
        public float fov = 90;

        Vector3 velocity; // current velocity

        private GameObject cameraObject;

        static bool Focused
        {
            get => Cursor.lockState == CursorLockMode.Locked;
            set
            {
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = value == false;
            }
        }

        void Awake()
        {
            if (Application.isPlaying)
            {
                var cameraPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/Main Camera.prefab").WaitForCompletion();

                var mainCamera = Instantiate(cameraPrefab, transform);

                var cameraRigController = mainCamera.GetComponent<CameraRigController>();
                //Fix fov override
                mainCamera.GetComponent<CameraRigController>().isCutscene = true;
                cameraRigController.sceneCam.fieldOfView = fov;

                cameraObject = mainCamera.transform.GetChild(0).gameObject;
            }
        }

        void OnEnable()
        {
            if (focusOnEnable) Focused = true;
        }

        void OnDisable() => Focused = false;

        void Update()
        {
            if (Application.isPlaying)
            {
                GameObject.Find("HUDSimple(Clone)")?.SetActive(false);
            }

            // Input
            if (Focused)
                UpdateInput();
            else if (Input.GetMouseButtonDown(0))
                Focused = true;

            // Physics
            velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
            cameraObject.transform.position += velocity * Time.deltaTime;
        }

        void UpdateInput()
        {
            // Position
            velocity += GetAccelerationVector() * Time.deltaTime;

            // Rotation
            Vector2 mouseDelta = lookSensitivity * new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            Quaternion rotation = cameraObject.transform.rotation;
            Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
            Quaternion vert = Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
            cameraObject.transform.rotation = horiz * rotation * vert;

            // Leave cursor lock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Focused = false;
            }
        }

        Vector3 GetAccelerationVector()
        {
            Vector3 moveInput = default;

            void AddMovement(KeyCode key, Vector3 dir)
            {
                if (Input.GetKey(key))
                    moveInput += dir;
            }

            AddMovement(KeyCode.W, Vector3.forward);
            AddMovement(KeyCode.S, Vector3.back);
            AddMovement(KeyCode.D, Vector3.right);
            AddMovement(KeyCode.A, Vector3.left);
            AddMovement(KeyCode.LeftShift, Vector3.down);
            AddMovement(KeyCode.Space, Vector3.up);
            Vector3 direction = cameraObject.transform.TransformVector(moveInput.normalized);

            if (Input.GetKey(KeyCode.LeftControl))
            {
                return direction * (acceleration * accSprintMultiplier); // "sprinting"
            }

            return direction * acceleration; // "walking"
        }
    }
}