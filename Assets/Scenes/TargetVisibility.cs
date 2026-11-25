
using UnityEngine;

public class TargetVisibility : MonoBehaviour
{
    public GameObject arContent;

    // Keeps the content for a moment when tracking drops out, for example while the can is shaken.
    [SerializeField] private float hideDelay = 0.3f;

    public void ShowContent()
    {
        CancelInvoke(nameof(Hide));
        arContent.SetActive(true);
    }

    public void HideContent()
    {
        Invoke(nameof(Hide), hideDelay);
    }

    private void Hide()
    {
        arContent.SetActive(false);
    }
}
