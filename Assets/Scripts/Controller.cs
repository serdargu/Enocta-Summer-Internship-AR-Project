using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyAR;
public class Controller : MonoBehaviour
{
    Settings settings;

    public string API_KEY = "AIzaSyAjbTIs3XCTGkhCBGyihJME24l3duua8PI";
    public string LANG = "tr-TR";

    /*References to display the time left*/
    public GameObject timer;
    public Text timer_text;

    [SerializeField] private int model_display_time = 15 ;
    [SerializeField] private int question_display_time =5;

    public ImageTrackerBaseBehaviour image_tracker; // reference to the image tracker component of the AR camera
    public GameObject barcode_scanner; // reference to the barcode scanner component of the AR camera

    public List<GameObject> target_images; // list of the target images that can be used in the app

    public bool model_alive; 

    public GameObject result_canvas; // canvas to display results
    public GameObject qr_code_canvas; // canvas to inform user when QR code is scanned
    

    /* Speech to text game objects */
    public GameObject speech_to_text_canvas;
    public GameObject question_text;
    public Text answer_text;
    public Text correct_answer_text;
    public GameObject stt_processing;
    public GameObject stt_recording;

    /* Speech recognition */
    const int HEADER_SIZE = 44;
    private int min_freq;
    private int max_freq;
    private bool mic_connected = false;
    private AudioSource go_audio_source; // a handle to the attached AudioSource
    private string file_path;

    /* Gameobjects to display result */
    private int correct_answers = 0; // # of correct answers
    private int false_answers = 0; // # of false answers
    public Text correct_answers_text; // to display the # of correct answers
    public Text false_answers_text; // to display the # of false answers
    private string result;
    private bool mutex;

    public GameObject controller;
    private IEnumerator Start()
    {

        /*WWW www = new WWW("http://www.myenocta.com/download/content/AR_demo/modelsAR.txt");
        //WWW www = new WWW("file:///C:/Users/eda.mutlu/Desktop/modelsAR.json");
        yield return www;
        settings = JsonUtility.FromJson<Settings>(www.text);*/
        
        // when the QR code scanned is not a json file
        while (settings == null)
        {
            Debug.Log("not a json file");
            yield return new WaitForSeconds(1.0f); // wait for a second
        }
        // when the QR code scanned is a json file but not in the form we wish to have
        while (settings.total_models==0)
        {
            Debug.Log("wrong json file");
            yield return new WaitForSeconds(1.0f); // wait for a second
        }
        // just after this point we know that we've scanned a json file in the form we wish to have

        Destroy(barcode_scanner); // disable QR scanning

        // some settings
        API_KEY = settings.api_key;
        LANG = settings.lang;
        model_display_time = settings.model_display_time;
        question_display_time = settings.question_countdown_time;
        
        // inform user about QR scanned was the correct one
        qr_code_canvas.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        qr_code_canvas.SetActive(false);

        Debug.Log("start");
        Debug.Log(settings.total_models);

        // create an deactive game object (ImageTarget-Image prefab) for each model given
        int i = 0;
        foreach (Model model in settings.models)
        {
            GameObject target_image = Resources.Load("ImageTarget-Image") as GameObject;
            target_image = Instantiate(target_image, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            Debug.Log(target_image);

            // set the references of the gameobject created
            EasyImageTargetBehaviour EITScript = target_image.GetComponent<EasyImageTargetBehaviour>();
            EITScript.Path = model.image_target + ".jpg";
            EITScript.Name = model.image_target;
            EITScript.Bind(image_tracker);
            EITScript.controller = controller;

            // keep a reference of the created object to access later
            target_images.Add(target_image);
            Debug.Log(target_images.Count);

            // make the model a child object of the ImageTarget-Image created 
            GameObject newModel = Resources.Load(model.name) as GameObject;
            newModel = Instantiate(newModel, target_images[i].transform);
            newModel.transform.SetParent(target_images[i].transform);

            // save the questions of the given model 
            foreach (Question question in model.questions)
            {
                ImageTarget image_target_script = target_images[i].GetComponent<ImageTarget>();
                image_target_script.questions.Add(question);

            }
            i++;
        }
        // find if there any activated model in the scene
        GameObject current_target = null;
        for (i = 0; i < target_images.Count; i++)
        {
            // ...if not wait
            while (!model_alive)
                yield return new WaitForSeconds(1.0f);
            // after finding it, display it for a while (given as model_display_time)
            Debug.Log("Model found");
            int count ;
            timer.gameObject.SetActive(true);
            for (count = model_display_time; count > 0; count--)
            {
                timer_text.text = "Kalan Süre: " + count;
                yield return new WaitForSeconds(1.0f); // Wait 1 sec
            }
            timer.gameObject.SetActive(false);
            foreach (var target_image in target_images)
            {
                if (target_image)
                {
                    if (target_image.activeInHierarchy)
                    {
                        current_target = target_image;
                        Destroy(target_image);
                        break;

                    }
                }
            }
            // display the questions of the current model
            yield return StartCoroutine(UpdateQuestion(current_target));
            model_alive = false;
        }
    }
    // change the content of the question canvas according to the question of the given gameobject
    private IEnumerator UpdateQuestion(GameObject target)
    {
        
        foreach (Question question in target.GetComponent<ImageTarget>().questions)
        {
            if(target) Destroy(target);
            timer.gameObject.SetActive(true);
            
           
            if (question.answer_type == "stt")
            {
             if (Microphone.devices.Length <= 0)
                {
                    /*SetVisibile(roomTimerText, false);
                    Debug.Log("Microphone not connected!"); // Throw a warning message at the console if there isn't
                    SetVisibile(micWarningText.gameObject, true);
                    yield return new WaitForSeconds(3.0f);
                    SetVisibile(micWarningText.gameObject, false);*/
                }
                else // At least one microphone is present
                {

                    speech_to_text_canvas.SetActive(true);
                    question_text.gameObject.SetActive(true);
                    question_text.GetComponent<Text>().text = question.description;

                    //Set 'micConnected' to true
                    mic_connected = true;

                    //Get the default microphone recording capabilities
                    Microphone.GetDeviceCaps(null, out min_freq, out max_freq);

                    // According to the documentation, if min_freq and max_freq are zero, the microphone supports any frequency...
                    if (min_freq == 0 && max_freq == 0)
                    {
                        //...Meaning 44100 Hz can be used as the recording sampling rate
                        max_freq = 44100;
                    }

                    //Get the attached AudioSource component
                    go_audio_source = this.GetComponent<AudioSource>();

                    // Display mic image & recording text
                    stt_recording.SetActive(true);

                    // Ready to record 
                    Debug.Log("start record");                  
                    yield return StartCoroutine(StartRecording(5));
                   
                    this.mutex = false;
                    SpeechToText();
                    stt_recording.SetActive(false);                    
                    stt_processing.SetActive(true);

                    while (!this.mutex)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }

                    stt_processing.SetActive(false);              
                    question_text.gameObject.SetActive(false);
                    
                    // Display the user's answer
                    answer_text.text = result;
                    answer_text.gameObject.SetActive(true);

                    // Check if the answer is correct or not
                    // ...Change the color of the text accordingly
                    Debug.Log(result.ToLower());
                    if (result.ToLower() == question.correct_answer.ToLower())
                    {
                        correct_answers += 1;
                        answer_text.color = Color.green;
                    }
                    else
                    {
                        false_answers += 1;
                        answer_text.color = Color.red;
                        // Display the correct answer
                        correct_answer_text.gameObject.SetActive(true);
                        correct_answer_text.text = "Doğru cevap: " + question.correct_answer.ToLower();
                    }

                    yield return new WaitForSeconds(5.0f);
                    correct_answer_text.gameObject.SetActive(false);
                    answer_text.gameObject.SetActive(false);
                    speech_to_text_canvas.SetActive(false);

                    // Delete the .wav file used
                    File.Delete(file_path);
                }
            }
            
            timer.gameObject.SetActive(false);           
        }
        yield return StartCoroutine(DisplayResults());

    }
    // The function to display the time spent, # of correct and wrong answers for 5 seconds
    private IEnumerator DisplayResults()
    {

        correct_answers_text.text = "Doğru cevap sayısı: " + correct_answers;
        false_answers_text.text = "Yanlış cevap sayısı: " + false_answers;
        result_canvas.SetActive(true);
        yield return new WaitForSeconds(5.0f);  // Wait 5 sec
        result_canvas.SetActive(false);
    }
    private bool startShowMessage;
    private bool isShowing;
    private string textMessage;
    private void Awake()
    {
        var EasyARBehaviour = FindObjectOfType<EasyARBehaviour>();
        EasyARBehaviour.Initialize();
        foreach (var behaviour in ARBuilder.Instance.ARCameraBehaviours)
        {
            behaviour.TextMessage += OnTextMessage;
        }
    }
    private void OnTextMessage(ARCameraBaseBehaviour arcameraBehaviour, string text)
    {
        StartCoroutine(ShowMessage(arcameraBehaviour));
        textMessage = text;
        startShowMessage = true;
        Debug.Log("got text: " + text);
    }
    IEnumerator ShowMessage(ARCameraBaseBehaviour arcameraBehaviour)
    {
        Debug.Log("show message");
        WWW www = new WWW(textMessage);
        yield return www;
        settings = JsonUtility.FromJson<Settings>(www.text);
        
}
    private void OnGUI()
    {
        if (isShowing)
            GUI.Box(new Rect(10, Screen.height / 2, Screen.width - 20, 30), textMessage);
    }
    private IEnumerator StartRecording(int questionCountDownTime)
    {
        // If there is a microphone
        if (mic_connected)
        {
            // If the audio from any microphone isn't being recorded
            if (!Microphone.IsRecording(null))
            {
                // Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource
                go_audio_source.clip = Microphone.Start(null, true, questionCountDownTime, max_freq);

                // Countdown to answer the given question
                int count;
                timer.SetActive(true);
                for (count = question_display_time; count > 0; count--)
                {
                    timer_text.text = "Kalan Süre: " + count;
                    yield return new WaitForSeconds(1.0f); // Wait 1 sec
                }
                timer.SetActive(false);

                var filename = "recording_" + UnityEngine.Random.Range(0.0f, 10.0f) + ".wav";

                Microphone.End(null); // Stop the audio recording

                file_path = Path.Combine("temp_records/", filename);
                file_path = Path.Combine(Application.persistentDataPath, file_path);

                Directory.CreateDirectory(Path.GetDirectoryName(file_path)); // Make sure directory exists if user is saving to sub dir.
                SavWav.Save(file_path, go_audio_source.clip); // Save a temporary .wav File
            }
        }
    }
    /*
     * https://cloud.google.com/speech/docs/getting-started
     * Google Speech To Text API using HTTP POST
     * First create a Cloud Project from https://console.cloud.google.com/cloud-resource-manager
     * To get api key, visit https://console.cloud.google.com/flows/enableapi?apiid=speech.googleapis.com
     * To see the language list that Google supports currently, visit https://cloud.google.com/speech/docs/languages
     * Currently Google supports .wav, .flac, .raw files well. (Tested with .wav, .flac and .raw)
     * Base64 encoding audio https://cloud.google.com/speech/docs/base64-encoding
     * 
     * The function that takes the audio file and converts it to the byte array
     * then sends it to the Google Speech To Text API
     * then receives the text and returns it
     */
    private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
    {
        HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

        var bytes = File.ReadAllBytes(file_path); // Read the bytes from the path
        string base64 = Convert.ToBase64String(bytes); // Convert the bytes to Base64 string

        // End the operation
        Stream postStream = request.EndGetRequestStream(asynchronousResult);

        string json = "{\n  \"config\": {\n    \"languageCode\":\"" + LANG + "\"\n  },\n  \"audio\":{\n    \"content\":\"" + base64 + "\"\n  }\n}"; // Create JSON that includes encoded bytes and language
        byte[] byteArray = Encoding.UTF8.GetBytes(json);
        // Write to the request stream.

        postStream.Write(byteArray, 0, byteArray.Length);
        postStream.Close();

        // Start the asynchronous operation to get the response
        request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
    }
    private void GetResponseCallback(IAsyncResult asynchronousResult)
    {
        HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
        string finalResult = ""; // To store the final result

        using (var streamReader = new StreamReader(response.GetResponseStream()))
        {
            SpeechToTextResult speechToTextResult = JsonUtility.FromJson<SpeechToTextResult>(streamReader.ReadToEnd()); // Convert the JSON to the object

            float maxConfidence = 0.0f; // To check whether the result has max confidence

            if (speechToTextResult.results.Count != 0) // If the result is not null
            {
                foreach (Alternative result in speechToTextResult.results.ElementAt(0).alternatives) // Iterate each results and to get the result that has max confidence
                {
                    if (maxConfidence < result.confidence)
                    {
                        finalResult = result.transcript; // Set the new transcript that currently has max confidence level
                        maxConfidence = result.confidence; // Update the confidence with the max
                    }
                }
            }
        }

        this.result = finalResult;
        this.mutex = true;

        // Release the HttpWebResponse
        response.Close();
    }
    private void SpeechToText()
    {
        System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true; // To handle the SSL, we don't know how; but it works, derived from https://ubuntuforums.org/showthread.php?t=1841740

        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://speech.googleapis.com/v1/speech:recognize?key=" + API_KEY);
        httpWebRequest.ContentType = "application/json"; // Content type that Google accepts is JSON
        httpWebRequest.Method = "POST"; // Set method as POST
        httpWebRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), httpWebRequest);

    }
}
