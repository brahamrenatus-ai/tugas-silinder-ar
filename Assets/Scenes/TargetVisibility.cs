
using UnityEngine;

public class TargetVisibility : MonoBehaviour
{
    public GameObject arContent;

    public void ShowContent()
    {
        arContent.SetActive(true);
    }

    public void HideContent()
    {
        arContent.SetActive(false);
    }
}
