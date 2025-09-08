using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
     public void GoToScene1()
    {
        SceneManager.LoadScene(0);
    }
    public void GoToScene2()
    {
        SceneManager.LoadScene(1);
    }
}
