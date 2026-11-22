using UnityEngine;

public class RotateLogo : MonoBehaviour
{
    public float kecepatanPutar = 10f; // Kecepatan putaran logo
    public Transform pusatPutaran; // Titik tengah (kaleng) yang akan dikelilingi

    void Update()
    {
        if (pusatPutaran != null)
        {
            transform.RotateAround(pusatPutaran.position, Vector3.up, kecepatanPutar * Time.deltaTime);
        }
    }
}