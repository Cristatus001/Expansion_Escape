using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour
{
    public Button playVideoButton;
    public GameObject videoDisplayPanel; // RawImage holder
    public VideoPlayer videoPlayer;

    void Start()
    {
        playVideoButton.onClick.AddListener(PlayVideo);
        videoPlayer.loopPointReached += OnVideoFinished;
        videoDisplayPanel.SetActive(false);
    }

    void PlayVideo()
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "111.mp4");
        videoPlayer.url = videoPath;

        videoDisplayPanel.SetActive(true);
        videoPlayer.Play();
    }


    void OnVideoFinished(VideoPlayer vp)
    {
        videoDisplayPanel.SetActive(false);
    }
}
