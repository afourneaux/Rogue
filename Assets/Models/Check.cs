using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Check {
    public string key;
    public string target;
    public string comparison; // Enum?
    public string type; // Enum?
    
    public bool Test(Dictionary<string,string> variables) {
        if (variables.TryGetValue(key, out string value)) {
            switch(type) {
                case "string":
                switch(comparison) {
                    case "=":
                        return value == target;
                    case "!=":
                        return value != target;
                    default:
                        Debug.LogError("comparison \"" + comparison + "\" not recognised!");
                        return false;
                }
                case "number":
                    if (int.TryParse(value, out int numValue) && int.TryParse(target, out int numTarget)) {
                        switch(comparison) {
                            case "=":
                                return numValue == numTarget;
                            case "!=":
                                return numValue != numTarget;
                            case ">":
                                return numValue > numTarget;
                            case "<":
                                return numValue < numTarget;
                            case ">=":
                                return numValue >= numTarget;
                            case "<=":
                                return numValue <= numTarget;
                            default:
                                Debug.LogError("comparison \"" + comparison + "\" not recognised!");
                                return false;
                        }
                    } else {
                        Debug.LogError("value \"" + value + "\" expected numeric!");
                        return false;
                    }
                default:
                Debug.LogError("type \"" + type + "\" not recognised!");
                return false;
            }
        } else {
            Debug.LogError("Variable key \"" + key + "\" not found!");
            return false;
        }
    }
}