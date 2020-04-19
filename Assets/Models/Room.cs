using System.Collections.Generic;

[System.Serializable]
public class Room 
{
    public int roomID;
    public string text;
    public Option[] options;

    public string DisplayText(Dictionary<string, string> variables) {
        string displayString = text;
        foreach (KeyValuePair<string, string> variable in variables) {
            displayString = displayString.Replace("{" + variable.Key + "}", variable.Value);
        }
        return displayString;
    }
}