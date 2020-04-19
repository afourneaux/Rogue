using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class RoomController : MonoBehaviour
{
    // TODO: Quit game, return to menu
    // TODO: back button, tracking variables via stack
    // TODO: Additive variables
    // TODO: Achievements
    public GameObject RoomTextPrefab;
    public GameObject RoomOptionsButtonPrefab;
    public GameObject RoomOptionsInputPrefab;
    public Button LoadGamePrefab;
    public GameObject RoomOptionsPanel;
    public GameObject ButtonList;
    Room activeRoom;
    Dictionary<int, Room> story;
    GameObject roomTextObject;
    Stack<int> roomHistory;
    Dictionary<string, string> defaultVariables;
    Dictionary<string, string> checkpointVariables;
    int checkpointRoom;

    public RoomController()
    {
        variables = new Dictionary<string, string>();
        defaultVariables = new Dictionary<string, string>();
        checkpointVariables = new Dictionary<string, string>();
    }

    // Start is called before the first frame update
    void Start()
    {
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
        GenerateStory(rawStory);
        Room startingRoom = story[1];

        foreach (Transform child in RoomOptionsPanel.transform) {
            GameObject.Destroy(child.gameObject);
        }
        GenerateRoom(startingRoom);
    }

    public void GenerateRoom(Room room)
    {
        // If this is a checkpoint, save progress
        if (room.checkpoint)
        {
            // TODO: Toast "Checkpoint saved!"
            checkpointRoom = room.roomID;
            checkpointVariables = new Dictionary<string, string>(variables);
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
                        if (option.key != null && option.key != "" && 
                            option.value != null && option.value != "") {
                            variables[option.key] = option.value;
                        }
                        // Progression switches: Handle save, load, and restart
                        switch (option.type)
                        {
                            case "restart":
                                variables = new Dictionary<string, string>(defaultVariables);
                                checkpointVariables = new Dictionary<string, string>(defaultVariables);
                                checkpointRoom = 1;
                                SelectRoom(option.roomID);
                                break;
                            case "reload":
                                Debug.Log("reloading room " + checkpointRoom);
                                variables = new Dictionary<string, string>(checkpointVariables);
                                SelectRoom(checkpointRoom);
                                break;
                            case "checkpoint":
                                checkpointVariables = new Dictionary<string, string>(variables);
                                checkpointRoom = option.roomID;
                                SelectRoom(option.roomID);
                                break;
                            default:
                                SelectRoom(option.roomID);
                                break;
                        }
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
            Debug.Log("Room " + roomID + " does not exist!");
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
                checkpointVariables[var.key] = var.value;
            }
        }
        checkpointRoom = 1;
        foreach (Room room in rawStory.story)
        {
            story.Add(room.roomID, room);
        }
    }
}
