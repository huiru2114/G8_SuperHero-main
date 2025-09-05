using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public string sceneToLoad = "SampleScene";

    public GameObject instructionsPanel;  // Assign InstructionsPanel
    public GameObject menuPanel;          // Assign MenuPanel (contains Start a+ Instructions buttons)

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("M key pressed - Start Game");
            StartGame();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("I key pressed - Show Instructions");
            ShowInstructions();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("B key pressed - Back to Menu");
            HideInstructions();
        }
    }

    public void StartGame()
    {
        Debug.Log("Loading scene: " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
    }

    public void ShowInstructions()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(true);

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    public void HideInstructions()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);
    }
}
