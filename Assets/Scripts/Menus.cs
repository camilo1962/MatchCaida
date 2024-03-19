using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menus : MonoBehaviour
{
    public void Salir()
    {
        Application.Quit();
    }

    public void CambiarEscena(string nombreDeEscena)
    {
        SceneManager.LoadScene(nombreDeEscena);
    }

    public void BorraRecords()
    {
        PlayerPrefs.DeleteAll();
    }
}
