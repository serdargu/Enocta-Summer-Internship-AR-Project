using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ButtonClass : MonoBehaviour {

    public bool true_answer;
    public bool clicked;
    //Reference to button to access its components
    public Button theButton;

    //this get the Transitions of the Button as its pressed
    private ColorBlock theColor;
    IEnumerator Start()
    {
        Button btn = theButton.GetComponent<Button>();
        btn.onClick.AddListener(ChangeColor);
        yield return new WaitForSeconds(1.0f);
        btn.GetComponentInParent<GameObject>().SetActive(false);


    }
    // Use this for initialization
    void Awake()
    {
        theButton = GetComponent<Button>();
        theColor = GetComponent<Button>().colors;

    }
    public void Clicked()
    {
        clicked = true;
    }
   
    void ChangeColor()
    {
        clicked = true;
        if (true_answer) TrueColors();
        else WrongColors();
    }
    private void WrongColors()
    {

        theColor.highlightedColor = Color.red;
        theColor.normalColor = Color.red;
        theColor.pressedColor = Color.red;

        theButton.colors = theColor;
        print("Clicked wrong option");
    }
    private void TrueColors()
    {

        theColor.highlightedColor = Color.green;
        theColor.normalColor = Color.green;
        theColor.pressedColor = Color.green;

        theButton.colors = theColor;
        print("Clicked true option");
    }
    /*if (question.answer_type == "multiple_choice")
                {
                    question_object.SetActive(true);
                    Debug.Log(question.description);
                    question_object.GetComponentInChildren<Text>().text = question.description;
                    int i = 0;
                    int answers_count = question.answers.Count;
                    foreach(Answer answer in question.answers)
                    {
                        answer_buttons.Add(Instantiate(buttonPrefab, BUTTON_POSITONS[answers_count - 2, i], Quaternion.identity, question_object.transform));
                        Debug.Log("Button coordinates: " + BUTTON_POSITONS[answers_count - 2, i].x + " " + BUTTON_POSITONS[answers_count - 2, i].y + " " + BUTTON_POSITONS[answers_count - 2, i].z );
                        //answer_buttons[i].transform.SetParent(); // Make the button's parent as question canvas
                        answer_buttons[i].GetComponentInChildren<Text>().text = question.answers[i].answer;
                        answer_buttons[i].GetComponentInChildren<ButtonClass>().true_answer = question.answers[i].correct;
                        i++;
                    }
                    //question_object.transform.position = new Vector3(0, 0, 0);
                    int count;
                    for (count = question_display_time; count > 0; count--)
                    {
                        timer_text.text = "Kalan Süre: " + count;
                        bool flag = false;
                        foreach (Button answer_button in answer_buttons)
                        {
                            if (answer_button.GetComponent<ButtonClass>().clicked)
                            {
                                flag = true;
                                if (answer_button.GetComponent<ButtonClass>().true_answer) correct_answers++;
                                else false_answers++;
                            }
                        }
                        yield return new WaitForSeconds(1.0f); // Wait 1 sec
                        if (flag) break;
                    }
                    foreach (Button button in answer_buttons)
                    {
                        Destroy(button.gameObject);
                    }
                    answer_buttons.Clear();
                    question_object.SetActive(false);

                    int time = question_display_time - count;
                    total_time_spent += time;
                }*/
}
