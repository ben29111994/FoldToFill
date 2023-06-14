using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using MoreMountains.NiceVibrations;
using UnityEngine.EventSystems;
using GPUInstancer;

public class GameController : MonoBehaviour
{
    [Header("Variable")]
    public static GameController instance;
    float fh;
    float fv;
    float h;
    float v;
    public static bool isDrag = false;
    public float speed;
    public Vector3 forwardRotationPoint;
    public Vector3 backRotationPoint;
    public Vector3 leftRotationPoint;
    public Vector3 rightRotationPoint;
    public Transform debugForward;
    public Transform debugBackward;
    public Transform debugLeft;
    public Transform debugRight;
    public bool rolling;
    int maxLevel = 100;
    public List<Texture2D> listGround = new List<Texture2D>();
    public List<Sprite> listBackground = new List<Sprite>();
    bool isStartGame = true;
    Vector3 point;
    Vector3 axis;
    GameObject playerSpawn;
    public bool isCalc = false;
    public bool isControl = true;
    public int progressCount;
    public int countFall;
    public int numOfStack;
    public int lastNumOfStack;
    float timeDelay = 1;
    Coroutine checkLoseRoutine;
    int tryTimes = 0;
    public List<Color> mainColor = new List<Color>();
    public Color theme;

    [Header("UI")]
    public GameObject winPanel;
    public GameObject losePanel;
    public Slider levelProgress;
    public Text currentLevelText;
    public Text nextLevelText;
    public Text moneyText;
    int currentLevel;
    public static int score;
    public static int money;
    public int progress;
    public Canvas canvas;
    public GameObject shopMenu;
    public GameObject gameMenu;
    public Text bestScoreText;
    public GameObject gemAnim;
    public GameObject shopButton;
    public GameObject bonusPopup;
    public InputField levelInput;
    public Text title;

    [Header("Objects")]
    public Transform blockManager;
    public GameObject plusVarPrefab;
    public GameObject conffeti1;
    public GameObject conffeti2;
    public GameObject playerPrefab;

    public GPUInstancerPrefab prefab;
    public GPUInstancerPrefabManager prefabManager;
    public List<GPUInstancerPrefab> instancesList = new List<GPUInstancerPrefab>();

    void Start()
    {
        Application.targetFrameRate = 60;
        theme = mainColor[Random.Range(0, mainColor.Count)];
        playerPrefab.GetComponent<Renderer>().material.color = theme;
        foreach(ParticleSystem item in playerPrefab.GetComponentsInChildren<ParticleSystem>())
        {
            var getColor = item.main;
            getColor.startColor = theme;
        }
        title.color = theme;
        tryTimes = 0;
        lastNumOfStack = -1;
        currentLevel = PlayerPrefs.GetInt("currentLevel");
        currentLevelText.text = currentLevel.ToString();
        nextLevelText.text = (currentLevel + 1).ToString();
        isControl = true;
        isDrag = false;
        BoundCalculator();
        levelProgress.maxValue = progressCount;
        levelProgress.value = 0;

        if (prefabManager != null && prefabManager.isActiveAndEnabled)
        {
            try
            {
                GPUInstancerAPI.RegisterPrefabInstanceList(prefabManager, instancesList);
                GPUInstancerAPI.InitializeGPUInstancer(prefabManager);
            }
            catch { }
        }
    }

    public void BoundCalculator()
    {
        if (!isCalc)
        {
            isCalc = true;
            var xList = new List<float>();
            var yList = new List<float>();
            var zList = new List<float>();
            numOfStack = 0;
            foreach (var item in transform.GetChild(0).GetComponentsInChildren<Transform>())
            {
                if (item != null && item.tag != "Root")
                {
                    if(Mathf.Abs(item.position.x) > 10 || Mathf.Abs(item.position.z) > 10)
                    {
                        Destroy(item.gameObject);
                        countFall++;
                    }
                    else
                    {
                        xList.Add(item.position.x);
                        yList.Add(item.position.y);
                        zList.Add(item.position.z);
                        numOfStack++;
                    }
                }
                else if(item.tag == "Root" && item.childCount == 0)
                {
                    Destroy(item.gameObject);
                }
            }
            xList.Sort();
            yList.Sort();
            zList.Sort();
            if(checkLoseRoutine != null)
            StopCoroutine(checkLoseRoutine);
            checkLoseRoutine = StartCoroutine(delayCheckLose());

            forwardRotationPoint = new Vector3((xList[0] + xList[zList.Count - 1]) / 2, yList[0], zList[zList.Count - 1] + 0.5f);
            backRotationPoint = new Vector3((xList[0] + xList[zList.Count - 1]) / 2, yList[0], zList[0] - 0.5f);
            leftRotationPoint = new Vector3(xList[0] - 0.5f, yList[0], (zList[0] + zList[zList.Count - 1]) / 2);
            rightRotationPoint = new Vector3(xList[xList.Count - 1] + 0.5f, yList[0], (zList[0] + zList[zList.Count - 1]) / 2);

            debugForward.position = forwardRotationPoint;
            debugBackward.position = backRotationPoint;
            debugLeft.position = leftRotationPoint;
            debugRight.position = rightRotationPoint;
            isCalc = false;
        }
    }
    private void FixedUpdate()
    {
        if (isStartGame && isControl)
        {
            if (Input.GetMouseButtonDown(0))
            {
                fh = Input.GetAxis("Mouse X") * speed;
                fv = Input.GetAxis("Mouse Y") * speed;
            }

            if (Input.GetMouseButton(0))
            {
                OnMouseDrag();
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDrag = false;
            }

            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                BoundCalculator();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                axis = Vector3.right;
                StartCoroutine(Roll(forwardRotationPoint));
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                axis = Vector3.left;
                StartCoroutine(Roll(backRotationPoint));
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                axis = Vector3.forward;
                StartCoroutine(Roll(leftRotationPoint));
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                axis = Vector3.back;
                StartCoroutine(Roll(rightRotationPoint));
            }
        }
    }

    void OnMouseDrag()
    {
#if UNITY_EDITOR
        h = Input.GetAxis("Mouse X") * speed;
        v = Input.GetAxis("Mouse Y") * speed;
        if (Mathf.Abs(h - fh) > 0.5f)
        {
            isDrag = true;
        }
        if (Mathf.Abs(v - fv) > 0.5f)
        {
            isDrag = true;
        }
#endif
#if UNITY_IOS
        if (Input.touchCount > 0)
        {
            h = Input.touches[0].deltaPosition.x / 8;
            v = Input.touches[0].deltaPosition.y / 8;
            isDrag = true;
        }
#endif
        if (isDrag)
        {
            Debug.Log(isControl);
            if (rolling && !isCalc)
                return;
            var rot = new Vector3(v, 0, -h);
            if (v > 0 && Mathf.Abs(v) > Mathf.Abs(-h))
            {
                axis = Vector3.right;
                StartCoroutine(Roll(forwardRotationPoint));
            }
            else if (v < 0 && Mathf.Abs(v) > Mathf.Abs(-h))
            {
                axis = Vector3.left;
                StartCoroutine(Roll(backRotationPoint));
            }
            else if (h > 0 && Mathf.Abs(-h) > Mathf.Abs(v))
            {
                axis = Vector3.back;
                StartCoroutine(Roll(rightRotationPoint));
            }
            else if (h < 0 && Mathf.Abs(-h) > Mathf.Abs(v))
            {
                axis = Vector3.forward;
                StartCoroutine(Roll(leftRotationPoint));
            }
        }
    }

    private IEnumerator Roll(Vector3 rotationPoint)
    {
        point = rotationPoint;
        float angle = 180;
        float a = 0;
        rolling = true;
        isControl = false;

        playerSpawn = Instantiate(transform.GetChild(0).gameObject, transform.position, transform.rotation);

        while (angle > 0)
        {
            a = Time.deltaTime * speed;
            transform.RotateAround(point, axis, a);
            angle -= a;
            yield return null;
        }
        transform.RotateAround(point, axis, angle);

        if (playerSpawn != null)
        {
            playerSpawn.transform.parent = transform.GetChild(0).transform;
        }
        BoundCalculator();
        rolling = false;
    }

    float ClampAngle(float angle, float from, float to)
    {
        if (angle < 0f) angle = 360 + angle;
        if (angle > 180f) return Mathf.Max(angle, 360 + from);
        return Mathf.Min(angle, to);
    }

    public void Scoring()
    {
        progressCount--;
        levelProgress.value = levelProgress.maxValue - progressCount;
        if (progressCount == 0)
        {
            Win();
        }
    }

    public void Lose()
    {
        if (isStartGame)
        {
            isStartGame = false;
            losePanel.SetActive(true);
        }
    }

    public void Win()
    {
        if (isStartGame)
        {
            losePanel.SetActive(false);
            isStartGame = false;
            currentLevel++;
            if (currentLevel > LevelGenerator.instance.list2DMaps.Count - 1)
            {
                currentLevel = 0;
            }
            PlayerPrefs.SetInt("currentLevel", currentLevel);
            winPanel.SetActive(true);
        }
    }

    public void ButtonStartGame()
    {
        gameMenu.SetActive(false);
    }

    IEnumerator delayCheckLose()
    {
        timeDelay = 1;
        while(timeDelay > 0)
        {
            timeDelay -= 0.025f;
            yield return null;
        }
        if (lastNumOfStack == levelProgress.value)
        {
            tryTimes++;
            if (tryTimes >= 2)
            {
                Lose();
            }
        }
        else
        {
            lastNumOfStack = (int)levelProgress.value;
            tryTimes = 0;
        }
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(0);
    }

    public void OnChangeMap()
    {
        if (levelInput != null)
        {
            int level = int.Parse(levelInput.text.ToString());
            PlayerPrefs.SetInt("currentLevel", level);
            SceneManager.LoadScene(0);
        }
    }

    public void ButtonNextLevel()
    {
        currentLevel++;
        PlayerPrefs.SetInt("currentLevel", currentLevel);
        SceneManager.LoadScene(0);
    }

    public void ButtonPreviousLevel()
    {
        currentLevel--;
        PlayerPrefs.SetInt("currentLevel", currentLevel);
        SceneManager.LoadScene(0);
    }
}
