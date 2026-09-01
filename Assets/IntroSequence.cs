using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    public GameObject EntryDoor;
    public GameObject Wall;
    public GameObject Player;

    public Camera MainCamera;

    public Transform StartPoint;
    public Transform EndPoint;

    public float Speed = 2f;
    public float ReplaySpeedMultiplier = 2.5f;

    public Canvas StartScreen;
    public float FadeDuration = 1f;

    private GameObject Gun;
    private Vector3 EndGunPos;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private Vector3 originalCameraLocalScale;

    private bool wallFinished;
    private bool introFinished;
    private bool introStarted;
    private bool fading;

    private bool firstTimeThisSession;

    private Graphic[] startScreenGraphics;
    private Color[] originalGraphicColors;

    private static bool introHasPlayedThisSession;

    public bool IntroFinished
    {
        get { return introFinished; }
    }

    private float CurrentSpeed
    {
        get
        {
            if (firstTimeThisSession)
                return Speed;

            return Speed * ReplaySpeedMultiplier;
        }
    }

    void Start()
    {
        firstTimeThisSession = !introHasPlayedThisSession;

        SetupIntro();

        if (firstTimeThisSession)
        {
            ShowStartScreen();
            introStarted = false;
        }
        else
        {
            HideStartScreen();
            introStarted = true;
        }
    }

    void ShowStartScreen()
    {
        if (StartScreen == null)
        {
            introStarted = true;
            return;
        }

        StartScreen.gameObject.SetActive(true);
        StartScreen.enabled = true;
        StartScreen.overrideSorting = true;
        StartScreen.sortingOrder = 9999;

        startScreenGraphics =
            StartScreen.GetComponentsInChildren<Graphic>(true);

        originalGraphicColors =
            new Color[startScreenGraphics.Length];

        for (int i = 0; i < startScreenGraphics.Length; i++)
        {
            originalGraphicColors[i] =
                startScreenGraphics[i].color;
        }
    }

    void HideStartScreen()
    {
        if (StartScreen != null)
        {
            StartScreen.gameObject.SetActive(false);
        }
    }

    void SetupIntro()
    {
        MainCamera = Camera.main;

        if (MainCamera != null)
        {
            originalCameraParent =
                MainCamera.transform.parent;

            originalCameraLocalPosition =
                MainCamera.transform.localPosition;

            originalCameraLocalRotation =
                MainCamera.transform.localRotation;

            originalCameraLocalScale =
                MainCamera.transform.localScale;

            CameraLookScript cameraLook =
                MainCamera.GetComponent<CameraLookScript>();

            if (cameraLook != null)
            {
                cameraLook.enabled = false;
            }

            if (MainCamera.transform.childCount > 0)
            {
                Gun =
                    MainCamera.transform.GetChild(0).gameObject;

                EndGunPos =
                    Gun.transform.localPosition;

                Gun.SetActive(false);

                Gun.transform.localPosition =
                    EndGunPos -
                    Vector3.up -
                    Vector3.forward;
            }

            if (StartPoint != null)
            {
                MainCamera.transform.position =
                    StartPoint.position;

                MainCamera.transform.rotation =
                    StartPoint.rotation;
            }

            if (Wall != null)
            {
                MainCamera.transform.SetParent(
                    Wall.transform,
                    true
                );
            }
        }

        if (Player != null)
        {
            Player.SetActive(false);
        }
    }

    void Update()
    {
        if (introFinished)
            return;

        if (!introStarted)
        {
            if (!fading &&
                (Input.GetKeyDown(KeyCode.Return) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                StartCoroutine(FadeOutStartScreen());
            }

            return;
        }

        if (!wallFinished)
        {
            MoveIntro();
        }
        else
        {
            FinishIntro();
        }
    }

    IEnumerator FadeOutStartScreen()
    {
        fading = true;

        if (StartScreen == null)
        {
            introStarted = true;
            fading = false;
            yield break;
        }

        float timer = 0f;

        while (timer < FadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(timer / FadeDuration);

            for (int i = 0; i < startScreenGraphics.Length; i++)
            {
                Color color =
                    originalGraphicColors[i];

                color.a =
                    originalGraphicColors[i].a *
                    (1f - t);

                startScreenGraphics[i].color =
                    color;
            }

            yield return null;
        }

        StartScreen.gameObject.SetActive(false);

        introStarted = true;
        fading = false;
    }

    void MoveIntro()
    {
        float introSpeed = CurrentSpeed;

        if (Wall != null && EndPoint != null)
        {
            Wall.transform.position =
                Vector3.MoveTowards(
                    Wall.transform.position,
                    EndPoint.position,
                    introSpeed * Time.deltaTime
                );
        }

        if (EntryDoor != null)
        {
            EntryDoor.transform.position -=
                Vector3.up *
                (introSpeed * 0.5f) *
                Time.deltaTime;
        }

        if (Wall == null || EndPoint == null)
            return;

        if (Vector3.Distance(
                Wall.transform.position,
                EndPoint.position
            ) <= 0.025f)
        {
            wallFinished = true;

            if (Player != null)
            {
                Player.SetActive(true);
            }

            RestoreCamera();

            if (Gun != null)
            {
                Gun.SetActive(true);
            }
        }
    }

    void FinishIntro()
    {
        float introSpeed = CurrentSpeed;

        if (Gun == null)
        {
            EnablePlayerCamera();
            CompleteIntro();
            return;
        }

        Gun.transform.localPosition =
            Vector3.MoveTowards(
                Gun.transform.localPosition,
                EndGunPos,
                introSpeed * 0.5f * Time.deltaTime
            );

        if (Vector3.Distance(
                Gun.transform.localPosition,
                EndGunPos
            ) <= 0.01f)
        {
            Gun.transform.localPosition =
                EndGunPos;

            EnablePlayerCamera();

            CompleteIntro();
        }
    }

    void CompleteIntro()
    {
        introFinished = true;
        introHasPlayedThisSession = true;
    }

    void RestoreCamera()
    {
        if (MainCamera == null)
            return;

        MainCamera.transform.SetParent(
            originalCameraParent,
            false
        );

        MainCamera.transform.localPosition =
            originalCameraLocalPosition;

        MainCamera.transform.localRotation =
            originalCameraLocalRotation;

        MainCamera.transform.localScale =
            originalCameraLocalScale;
    }

    void EnablePlayerCamera()
    {
        if (MainCamera == null)
            return;

        CameraLookScript cameraLook =
            MainCamera.GetComponent<CameraLookScript>();

        if (cameraLook != null)
        {
            cameraLook.enabled = true;
        }
    }
}