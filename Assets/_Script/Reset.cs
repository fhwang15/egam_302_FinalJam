using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void OnButtonPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnButtonStartGame()
    {
        SceneManager.LoadScene("MainScene");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
