using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomController : MonoBehaviour
{
    // TODO: Additive variables
    // TODO: Enums
    // TODO: Make the main title "ROGUE" look more grand
    // TODO: Experiment with fonts
    // TODO: Active story's title along the top of the screen, inline with "Back" and "Main Menu" buttons
    // TODO: Dev mode: Log output to screen, track variables, view full current room details
    // TODO: More buttons on main menu. Quit to Desktop for example
    // TODO: Document how to use JSON interface
    // TODO: Achievements
    // TODO: Minigames
    public GameObject RoomTextPrefab;
    public GameObject RoomOptionsButtonPrefab;
    public GameObject RoomOptionsInputPrefab;
    public Button LoadGamePrefab;
    public GameObject RoomOptionsPanel;
    public GameObject ButtonList;
    public Button BackButton;
    public Button MenuButton;
    Room activeRoom;
    Dictionary<int, Room> story;
    GameObject roomTextObject;
    Stack<int> roomHistory;
    Stack<Dictionary<string,string>> variableHistory;
    Dictionary<string, string> variables;
    Dictionary<string, string> defaultVariables;

    public RoomController()
    {
        variables = new Dictionary<string, string>();
        defaultVariables = new Dictionary<string, string>();
        roomHistory = new Stack<int>();
        variableHistory = new Stack<Dictionary<string, string>>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Hook up the Back and Menu buttons
        BackButton.onClick.AddListener(Back);
        MenuButton.onClick.AddListener(Menu);

        // Find all json files in Stories
        string[] files = Directory.GetFiles("Assets/Resources", "*.json");
        foreach(string file in files) {
            // Determine if they parse correctly
            string fileName = Path.GetFileName(file).Replace(".json", "");
            TextAsset textAsset = Resources.Load<TextAsset>(fileName);
            Story storyJSON = JsonUtility.FromJson<Story>(textAsset.text);
            if (storyJSON != null && storyJSON.title != null && storyJSON.title != "") {
                // Display each story based on its title
                Button newLoadable = Instantiate(LoadGamePrefab, Vector3.zero, Quaternion.identity);
                newLoadable.transform.SetParent(ButtonList.transform);
                Text loadText = newLoadable.GetComponentInChildren<Text>();
                loadText.text = storyJSON.title;
                // Put each story into an onClick listener for each button for GenerateStory
                newLoadable.onClick.AddListener(() => {
                    StartGame(storyJSON);
                });
            }
            // TODO: If the story parse failed, display filename with an X
        }
    }

    public void StartGame(Story rawStory) 
    {
        MenuButton.gameObject.SetActive(true);
        GenerateStory(rawStory);
        Room startingRoom = story[1];

        foreach (Transform child in RoomOptionsPanel.transform) {
            GameObject.Destroy(child.gameObject);
        }
        GenerateRoom(startingRoom);
    }

    public void GenerateRoom(Room room)
    {
        // Display the back button (or not)
        if (roomHistory.Count > 0) {
            BackButton.gameObject.SetActive(true);
        } else {
            BackButton.gameObject.SetActive(false);
        }

        // Generate the room block text
        roomTextObject = Instantiate(RoomTextPrefab, Vector3.zero, Quaternion.identity);
        roomTextObject.transform.SetParent(RoomOptionsPanel.transform, false);
        Text roomText = roomTextObject.GetComponentInChildren<Text>();
        roomText.text = room.DisplayText(variables);

        GameObject optionObject;
        Button submitButton;
        Text optionText;
        InputField input;
        Button button;

        // Generate each button
        foreach (Option option in room.options)
        {
            // Test whether to display the condition based on a check
            if (option.hasCheck) {
                if (!option.check.Test(variables)) {
                    continue;
                }
            }
            // Input switches: Handle the display of different input types
            switch (option.type)
            {
                case "text":
                    // Create and place input and button
                    optionObject = Instantiate(RoomOptionsInputPrefab, Vector3.zero, Quaternion.identity);
                    optionObject.transform.SetParent(RoomOptionsPanel.transform, false);
                    // Set text
                    submitButton = optionObject.GetComponentInChildren<Button>();
                    input = optionObject.GetComponentInChildren<InputField>();
                    optionText = submitButton.GetComponentInChildren<Text>();
                    optionText.text = option.DisplayText(variables);
                    // Set button listener
                    submitButton.onClick.AddListener(() =>
                    {
                        SaveToHistory(activeRoom, option);
                        input.text = input.text.Trim();
                        if (input.text == "")
                        {
                            // TODO: Display a toast stating "Input is empty" or something more graceful
                            return;
                        }
                        variables[option.key] = input.text;
                        SelectRoom(option.roomID);
                    });
                    break;
                case "challenge":
                    // Create and place input and button
                    optionObject = Instantiate(RoomOptionsInputPrefab, Vector3.zero, Quaternion.identity);
                    optionObject.transform.SetParent(RoomOptionsPanel.transform, false);
                    optionObject.transform.localScale = new Vector3(1f, 1f, 1f);
                    // Set text
                    submitButton = optionObject.GetComponentInChildren<Button>();
                    input = optionObject.GetComponentInChildren<InputField>();
                    optionText = submitButton.GetComponentInChildren<Text>();
                    optionText.text = option.DisplayText(variables);
                    // Set button listener
                    submitButton.onClick.AddListener(() =>
                    {
                        SaveToHistory(activeRoom, option);
                        input.text = input.text.Trim();
                        if (input.text == "")
                        {
                            // TODO: Display a toast stating "Input is empty" or something more graceful
                            return;
                        }
                        if (option.key != null)
                        {
                            variables[option.key] = input.text;
                        }
                        SelectRoom(option.getChallengeResponse(variables, input.text));
                    });
                    break;
                default:
                    // Create and place button
                    optionObject = Instantiate(RoomOptionsButtonPrefab, Vector3.zero, Quaternion.identity);
                    optionObject.transform.SetParent(RoomOptionsPanel.transform, false);
                    optionObject.transform.localScale = new Vector3(1f, 1f, 1f);
                    // Set text
                    optionText = optionObject.GetComponentInChildren<Text>();
                    optionText.text = option.DisplayText(variables);
                    // Set button listener
                    button = optionObject.GetComponent<Button>();
                    button.onClick.AddListener(() =>
                    {
                        SaveToHistory(activeRoom, option);
                        if (option.key != null && option.key != "" && 
                            option.value != null && option.value != "") {
                            variables[option.key] = option.value;
                        }
                        if (option.type == "restart") {
                            variables = new Dictionary<string, string>(defaultVariables);
                            roomHistory = new Stack<int>();
                            variableHistory = new Stack<Dictionary<string, string>>();
                        }
                        SelectRoom(option.roomID);
                    });
                    break;
            }
        }
        activeRoom = room;
    }

    public void ClearRoom()
    {
        Destroy(roomTextObject);
        for (int i = 0; i < RoomOptionsPanel.transform.childCount; i++)
        {
            Destroy(RoomOptionsPanel.transform.GetChild(i).gameObject);
        }
        activeRoom = null;
    }

    public void SelectRoom(int roomID)
    {
        if (story.TryGetValue(roomID, out Room room))
        {
            ClearRoom();
            GenerateRoom(room);
        }
        else
        {
            Debug.LogError("Room " + roomID + " does not exist!");
        }
    }

    public void GenerateStory(Story rawStory)
    {
        story = new Dictionary<int, Room>();
        if (rawStory.defaultVariables != null) {
            foreach (Variable var in rawStory.defaultVariables)
            {
                variables[var.key] = var.value;
                defaultVariables[var.key] = var.value;
            }
        }
        foreach (Room room in rawStory.story)
        {
            story.Add(room.roomID, room);
        }
    }

    public void SaveToHistory(Room room, Option option) {
        roomHistory.Push(room.roomID);
        Dictionary<string, string> variableChanges = new Dictionary<string, string>();
        // TODO: Eventually, we may implement changing multiple variables in an option
        if (option.key != null) {
            if (variables.TryGetValue(option.key, out string value)) {
                variableChanges[option.key] = value;
            } else {
                variableChanges[option.key] = null;
            }
        }
        variableHistory.Push(variableChanges);
    }

    public void Back() {
        if (variableHistory.Count <= 0) {
            return;
        }
        Dictionary<string,string> updateVariables = variableHistory.Pop();
        int updateRoomID = roomHistory.Pop();
        foreach (KeyValuePair<string,string> variable in updateVariables) {
            if (variable.Value == null) {
                variables.Remove(variable.Key);
            } else {
                variables[variable.Key] = variable.Value;
            }
        }
        SelectRoom(updateRoomID);
    }

    public void Menu() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
