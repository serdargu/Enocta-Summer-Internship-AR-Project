using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Settings
{
    public string api_key;
    public string lang;
    public int model_display_time;
    public int question_countdown_time;
    public int total_models;
    public List<Model> models;
}

[System.Serializable]
public class Model
{
    public string image_target;
    public string name;
    public int total_questions;
    public List<Question> questions;
}


[System.Serializable]
public class Question
{
    public string description;
    public List<Answer> answers;
    public string answer_type;
    public string correct_answer;
}

[System.Serializable]
public class Answer
{
    public string answer;
    public bool correct;

}
