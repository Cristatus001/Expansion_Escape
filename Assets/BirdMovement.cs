using UnityEngine;
using System.Collections;

public class BirdMovement : MonoBehaviour
{
    public Transform[] pathPoints;
    public float speed = 2f;
    public GameObject panel; // Added reference to panel

    public void StartBirdMovement()
    {
        StartCoroutine(MoveBirdAlongPath());
    }

    public IEnumerator MoveBirdAlongPath() // Changed to public
    {
        foreach (var point in pathPoints)
        {
            while (Vector3.Distance(transform.position, point.position) > 0.9f)
            {
                transform.position = Vector3.MoveTowards(transform.position, point.position, speed * Time.deltaTime);
                yield return null;
            }
        }
        Debug.Log("Bird reached the end of the path!");
        transform.Rotate(0, 180, 0); // Rotate bird 180 degrees on Y-axis
        panel.SetActive(true); // Enable panel
    }
}
