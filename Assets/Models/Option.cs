using System.Collections.Generic;

[System.Serializable]
public class Option {
    public int roomID;
    public string text;
    public string type; // TODO: Enum. Button, small, text, number, challenge
    public string key; // For text, numbers, and challenges, this is where the response is saved
    public string value; // For buttons and smalls, set this value to the variable at key
    public Answer[] answers; // For challenges, this is the list of answers tied to their target room. If none are found, go to the default roomID

    public string DisplayText(Dictionary<string, string> variables) {
        string displayString = text;
        foreach (KeyValuePair<string, string> variable in variables) {
            displayString = displayString.Replace("{" + variable.Key + "}", variable.Value);
        }
        return displayString;
    }

    public int getChallengeResponse(Dictionary<string, string> variables, string input) {
        foreach (Answer answer in answers) {
            // If the input matches the hard coded answer
            if (answer.answer != null && input == answer.answer) {
                return answer.roomID;
            }
            // If the input matches a variable keyed by the answer
            if (answer.variableAnswer != null && variables.TryGetValue(answer.variableAnswer, out string variableAnswer) && variableAnswer == input) {
                return answer.roomID;
            }
        }
        // If no answers are matched, return the default
        return roomID;
    }
}