// [Your using directives remain unchanged]
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Option
{
    public string text;
    public bool isCorrect;
}

[System.Serializable]
public class QuizData
{
    public string question;
    public List<Option> options;
    public string correctAnswerText;
    public string randomText;
    public string explanationText;
    public string failureText;
}

[System.Serializable]
public class QuizWrapper
{
    public List<QuizData> quizData;
}

public class QuizManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text scoreText;
    public Button[] optionButtons;
    public GameObject questionPanel;
    public GameObject optionsParent;
    public GameObject Explainationpanel;
    public TMP_Text panelText;
    public Button nextButton;
    public GameObject failurePanel;
    public TMP_Text failurePanelText;

    [Header("Animated Objects")]
    public GameObject cage;
    public GameObject additionalImage;
    public BirdMovement[] birdMovement;
    public GameObject birdObject;

    [Header("Question Objects")]
    public GameObject[] questionObjects;

    [Header("Animator")]
    public Animator[] birdAnimators;

    [Header("Bird Images")]
    public Sprite[] birdSprites;
    public Image[] birdImage;

    private List<QuizData> allQuizData;
    private int currentQuestionIndex = 0;
    private int attemptsLeft = 2;
    private bool quizCompleted = false;
    private int score = 0;
 
    private Vector3 cageOriginalPosition;
    private Vector3 additionalImageOriginalPosition;
    private Vector3 birdOriginalPosition;
    private Vector3 swingParentOriginalPosition;

    [Header("Review Panel")]
    public GameObject reviewPanel;
    public Button reviewPanelButton;


    [Header("Results Panel")]
    public GameObject resultsPanel;
    public Image[] savedBirdImages;
    private List<Sprite> savedBirdSprites = new List<Sprite>();

    [Header("Swing Control")]
    public Transform swingParent;
    public float swingAngle = 15f;
    public float swingSpeed = 2f;

    private Coroutine swingCoroutine;
    private bool isCurrentFailure = false;

    void Start()
    {
        for (int i = 0; i < birdAnimators.Length; i++)
        {
            if (birdAnimators[i] == null)
            {
                Debug.LogError("birdAnimators[" + i + "] is not assigned!");
            }
        }

        questionPanel.SetActive(false);
        failurePanel.SetActive(false);
        Explainationpanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        LoadQuizData();

        for (int i = 0; i < questionObjects.Length; i++)
            questionObjects[i].SetActive(i == 0);

        cageOriginalPosition = cage.transform.position;
        additionalImageOriginalPosition = additionalImage.transform.position;
        birdOriginalPosition = birdImage[currentQuestionIndex].transform.position;
        swingParentOriginalPosition = swingParent.position;

        scoreText.text = $"{score}/{allQuizData.Count}";
        
        for (int i = 0; i < birdImage.Length; i++)
            birdImage[i].gameObject.SetActive(false);
    }

    public void ShowQuestionPanel()
    {
        questionPanel.SetActive(true);
        quizCompleted = false;
        attemptsLeft = 2;
        DisplayQuestion();

        // 🔥 Set bird to Fly animation before cage drop
        foreach (Animator anim in birdAnimators)
        {
            anim.ResetTrigger("Fail");
            anim.SetTrigger("Fly");
        }

        StartCoroutine(LockBirdInCage());
    }


    void LoadQuizData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("quizData");
        if (jsonFile != null)
        {
            QuizWrapper data = JsonUtility.FromJson<QuizWrapper>(jsonFile.text);
            allQuizData = data.quizData;
        }
        else
        {
            Debug.LogError("Quiz data file not found in Resources folder.");
        }
    }

    private void DisplayQuestion()
    {
        if (currentQuestionIndex >= allQuizData.Count)
        {
            Debug.Log("Quiz completed!");
            return;
        }

        foreach (Animator anim in birdAnimators)
        {
            anim.ResetTrigger("Fly");
            anim.ResetTrigger("Fail");
            anim.Play("Idle");
        }

        QuizData currentQuiz = allQuizData[currentQuestionIndex];

        TMP_Text questionObjText = questionObjects[currentQuestionIndex].GetComponentInChildren<TMP_Text>();
        questionObjText.text = currentQuiz.question;

        optionsParent.transform.position = questionObjects[currentQuestionIndex].transform.position + new Vector3(0, -50f, 0);
        for (int i = 0; i < birdImage.Length; i++)
        {
            birdImage[i].gameObject.SetActive(false);
        }

        birdImage[currentQuestionIndex].gameObject.SetActive(true);

        if (birdSprites != null && birdSprites.Length > currentQuestionIndex)
            birdImage[currentQuestionIndex].sprite = birdSprites[currentQuestionIndex];

        birdImage[currentQuestionIndex].gameObject.transform.position = birdOriginalPosition;

        var randomizedOptions = currentQuiz.options.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].GetComponentInChildren<TMP_Text>().text = randomizedOptions[i].text;
            optionButtons[i].interactable = true;
            optionButtons[i].image.color = Color.white;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => CheckAnswer(randomizedOptions[index].isCorrect, optionButtons[index]));
        }
    }

    IEnumerator LockBirdInCage()
    {
        foreach (var button in optionButtons)
            button.interactable = false;

        cage.transform.position = cageOriginalPosition;
        additionalImage.transform.position = additionalImageOriginalPosition;

        Vector3 cageStart = cageOriginalPosition + new Vector3(0, 700f, 0);
        Vector3 cageEnd = cageOriginalPosition;

        Vector3 addImgStart = additionalImageOriginalPosition - new Vector3(0, 300f, 0);
        Vector3 addImgEnd = additionalImageOriginalPosition;

        cage.transform.position = cageStart;
        additionalImage.transform.position = addImgStart;

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cage.transform.position = Vector3.Lerp(cageStart, cageEnd, elapsed / duration);
            additionalImage.transform.position = Vector3.Lerp(addImgStart, addImgEnd, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cage.transform.position = cageEnd;
        additionalImage.transform.position = addImgEnd;

        if (swingCoroutine != null)
            StopCoroutine(swingCoroutine);

        swingCoroutine = StartCoroutine(SwingObject(swingParent));

        foreach (Animator anim in birdAnimators)
        {
            anim.ResetTrigger("Fly");
            anim.ResetTrigger("Fail");
            anim.SetTrigger("idle");
           
        }

        foreach (var button in optionButtons)
            button.interactable = true;

    }
    
    IEnumerator SwingObject(Transform obj, float maxAngle = 15f, float speed = 1.5f)
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * speed;
            float angle = maxAngle * Mathf.Sin(t);
            obj.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
    }

    void CheckAnswer(bool isCorrect, Button clickedButton)
    {
        if (quizCompleted) return;

        if (isCorrect)
        {
            clickedButton.image.color = Color.green;
            quizCompleted = true;
            score++;

            if (birdSprites != null && birdSprites.Length > currentQuestionIndex)
                savedBirdSprites.Add(birdSprites[currentQuestionIndex]);

            scoreText.text = $"{score}/{allQuizData.Count}";

            // Make all other buttons uninteractable  
            foreach (var button in optionButtons)
            {
                button.interactable = false;
            }

            ReplaceQuestionMarkImmediately();
            StartCoroutine(StartBirdMovementAndThenShowPanel());

        }
        else
        {
            StartCoroutine(BlinkRed(clickedButton));
            clickedButton.image.color = Color.gray;
            clickedButton.interactable = false;
            attemptsLeft--;

            if (attemptsLeft <= 0)
            {
                quizCompleted = true;
                birdAnimators[currentQuestionIndex].SetTrigger("Fail");
                ShowCorrectAnswer();
                ShowFailureText();
            }
        }
    }
    void ReplaceQuestionMarkImmediately()
    {
        QuizData currentQuiz = allQuizData[currentQuestionIndex];
        TMP_Text questionObjText = questionObjects[currentQuestionIndex].GetComponentInChildren<TMP_Text>();

        if (questionObjText.text.Contains("?"))
            questionObjText.text = questionObjText.text.Replace("?", currentQuiz.correctAnswerText);
        else
            questionObjText.text = currentQuiz.correctAnswerText;
    }

    IEnumerator StartBirdMovementAndThenShowPanel()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingParent.localRotation = Quaternion.identity;
        }

        Vector3 cageStart = cage.transform.position;
        Vector3 cageEnd = cageStart + new Vector3(0, 780f, 0);

        Vector3 addImgStart = additionalImage.transform.position;
        Vector3 addImgEnd = addImgStart - new Vector3(0, 1500f, 0);

        float cageDuration = 1.5f;
        float addImgDuration = 0.7f;
        float elapsed = 0f;

        birdAnimators[currentQuestionIndex].SetTrigger("Fly");
        birdMovement[currentQuestionIndex].StartBirdMovement();

        while (elapsed < cageDuration || elapsed < addImgDuration)
        {
            if (elapsed < cageDuration)
                cage.transform.position = Vector3.Lerp(cageStart, cageEnd, elapsed / cageDuration);

            if (elapsed < addImgDuration)
                additionalImage.transform.position = Vector3.Lerp(addImgStart, addImgEnd, elapsed / addImgDuration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cage.transform.position = cageEnd;
        additionalImage.transform.position = addImgEnd;

        yield return new WaitForSeconds(0f);
        ShowAnswer();
    }

    void ShowCorrectAnswer()
    {
        QuizData currentQuiz = allQuizData[currentQuestionIndex];

        // First make all buttons grey
        for (int i = 0; i < optionButtons.Length; i++)
        {
            //optionButtons[i].image.color = Color.gray;
            optionButtons[i].interactable = false;
        }

        // Then delay and highlight the correct one
        StartCoroutine(HighlightCorrectAfterDelay(currentQuiz));
    }

    IEnumerator HighlightCorrectAfterDelay(QuizData currentQuiz)
    {
        yield return new WaitForSeconds(1f); // Wait before highlighting

        Option correctOption = currentQuiz.options.Find(x => x.isCorrect);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            TMP_Text btnText = optionButtons[i].GetComponentInChildren<TMP_Text>();
            if (btnText.text == correctOption.text)
            {
                optionButtons[i].image.color = Color.green;
            }
        }

        // ✅ Replace the "?" with correct answer *after* showing green highlight
        ReplaceQuestionMarkImmediately();
    }



    void ShowAnswer()
    {
        QuizData currentQuiz = allQuizData[currentQuestionIndex];
        TMP_Text questionObjText = questionObjects[currentQuestionIndex].GetComponentInChildren<TMP_Text>();

       
        panelText.text = currentQuiz.explanationText;
        //Invoke("EnablePanel", 1f);
        nextButton.gameObject.SetActive(true);
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextQuestion);
    }
    private void EnablePanel()
    {
        Explainationpanel.SetActive(true);
    }
    void ShowFailureText()
    {
        isCurrentFailure = true;
        StartCoroutine(ShowFailurePanelWithDelay());
    }


    IEnumerator ShowFailurePanelWithDelay()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingParent.localRotation = Quaternion.identity;
        }

        QuizData currentQuiz = allQuizData[currentQuestionIndex];
        TMP_Text questionObjText = questionObjects[currentQuestionIndex].GetComponentInChildren<TMP_Text>();

        /*if (questionObjText.text.Contains("?"))
            questionObjText.text = questionObjText.text.Replace("?", currentQuiz.correctAnswerText);
        else
            questionObjText.text = currentQuiz.correctAnswerText;*/

        yield return new WaitForSeconds(2f);

        failurePanel.SetActive(true);
        failurePanelText.text = currentQuiz.failureText;

        Button failureNextButton = failurePanel.GetComponentInChildren<Button>();
        if (failureNextButton != null)
        {
            failureNextButton.onClick.RemoveAllListeners();
            failureNextButton.onClick.AddListener(() =>
            {
                StartCoroutine(AnimateParentUpAndProceed());
            });
        }
    }

    IEnumerator AnimateParentUpAndProceed()
{
    Vector3 start = swingParent.position;
    Vector3 end;

    if (isCurrentFailure)
    {
        end = start - new Vector3(0, 1000f, 0); // DROP DOWN
    }
    else
    {
        end = start + new Vector3(0, 1000f, 0); // MOVE UP (default)
    }

    float duration = 0.5f;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        swingParent.position = Vector3.Lerp(start, end, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    swingParent.position = end;
    isCurrentFailure = false; // Reset for next question
    NextQuestion();
}


    void ShowResults()
    {
        reviewPanel.SetActive(true); // Show intermediate panel first

        reviewPanelButton.onClick.RemoveAllListeners();
        reviewPanelButton.onClick.AddListener(() =>
        {
            reviewPanel.SetActive(false);
            resultsPanel.SetActive(true);

            for (int i = 0; i < savedBirdImages.Length; i++)
            {
                if (i < savedBirdSprites.Count)
                {
                    savedBirdImages[i].sprite = savedBirdSprites[i];
                    savedBirdImages[i].SetNativeSize();
                    savedBirdImages[i].gameObject.SetActive(true);
                }
                else
                {
                    savedBirdImages[i].gameObject.SetActive(false);
                }
            }
        });
    }


    IEnumerator BlinkRed(Button button)
    {
        for (int i = 0; i < 3; i++)
        {
            button.image.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            button.image.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
        button.image.color = Color.gray;
    }

    void NextQuestion()
    {
        Explainationpanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        failurePanel.SetActive(false);

        currentQuestionIndex++;

        if (currentQuestionIndex < questionObjects.Length && currentQuestionIndex < allQuizData.Count)
        {
            cage.transform.position = cageOriginalPosition;
            additionalImage.transform.position = additionalImageOriginalPosition;
            birdObject.transform.position = birdOriginalPosition;
            birdObject.transform.rotation = Quaternion.Euler(0, 0, 0);
            swingParent.position = swingParentOriginalPosition;

            questionObjects[currentQuestionIndex].SetActive(true);
            optionsParent.transform.position = questionObjects[currentQuestionIndex].transform.position + new Vector3(0, -50f, 0);

            quizCompleted = false;
            attemptsLeft = 2;

            scoreText.text = $"{score}/{allQuizData.Count}";
            DisplayQuestion();

            StartCoroutine(LockBirdInCage());
        }
        else
        {
            ShowResults();
        }
    }
}
