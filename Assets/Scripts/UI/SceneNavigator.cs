using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    [Header("Panel Petunjuk (Opsional)")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private float autoHideDelay = 5f;

    private void Start()
    {
        // Jika panel petunjuk dipasang, sembunyikan otomatis setelah beberapa detik
        if (instructionPanel != null && instructionPanel.activeSelf)
        {
            Invoke(nameof(HideInstructionPanel), autoHideDelay);
        }
    }

    // Method pindah scene berdasarkan nama (untuk OnClick Button)
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneNavigator] Nama scene kosong!");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    // Method untuk tombol tutup panel petunjuk
    public void HideInstructionPanel()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }
}