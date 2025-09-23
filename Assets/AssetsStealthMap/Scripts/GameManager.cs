using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Optional")]
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    bool _locked;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayerCaught()
    {
        if (_locked) return; _locked = true;
        if (losePanel) losePanel.SetActive(true);
        Invoke(nameof(ReloadScene), 1.0f);
    }


    public void PlayerWon()
    {
        if (_locked) return; _locked = true;
        if (winPanel) winPanel.SetActive(true);
        Invoke(nameof(ReloadScene), 1.5f);
    }


    void ReloadScene()
    {
        _locked = false;
        var idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
    }


}
