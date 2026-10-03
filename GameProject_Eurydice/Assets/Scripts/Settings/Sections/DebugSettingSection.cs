using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DebugSettingSection : SettingSection
{
    [SerializeField] private Button deleteSaveDataButton;
    [SerializeField] private Button deleteSettingsButton;
    [SerializeField] private Button deleteAllDataButton;
    [SerializeField] private SettingSelectorControl sceneSelector;
    [SerializeField] private Button loadSceneButton;
    [SerializeField] private Button quitGameButton;

    public override void Init(SettingManager manager)
    {
        if (deleteSaveDataButton != null) deleteSaveDataButton.onClick.AddListener(SaveManager.DeleteSaveData);
        if (deleteSettingsButton != null) deleteSettingsButton.onClick.AddListener(SaveManager.DeleteSettings);
        if (deleteAllDataButton != null) deleteAllDataButton.onClick.AddListener(SaveManager.DeleteAllData);
        if (loadSceneButton != null) loadSceneButton.onClick.AddListener(LoadSelectedScene);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(QuitGame);

        if (sceneSelector != null)
        {
            var options = new List<string>();
            int sceneCount = SceneManager.sceneCountInBuildSettings;

            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = Path.GetFileNameWithoutExtension(path);
                options.Add("[" + i + "] " + sceneName);
            }

            if (options.Count == 0)
            {
                options.Add("[0] " + SceneManager.GetActiveScene().name);
            }

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int defaultIndex = Mathf.Clamp(currentSceneIndex, 0, options.Count - 1);
            sceneSelector.Init(options, defaultIndex);
        }
    }

    private void LoadSelectedScene()
    {
        if (sceneSelector == null) return;

        int index = sceneSelector.CurrentIndex;
        if (index >= 0 && index < SceneManager.sceneCountInBuildSettings)
        {
            Time.timeScale = 1f;
            UIWindowManager.Instance?.CloseAllTabs(animatePanel: false);
            GameStateManager.ResetState();
            SceneManager.LoadScene(index);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
